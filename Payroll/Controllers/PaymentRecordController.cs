using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NToastNotify;
using Payroll.Controllers.Options;
using Payroll.Controllers.Providers;
using Payroll.Controllers.ViewModels;
using Payroll.Services;
using Payroll.Services.Contracts.Services;
using Payroll.Services.Objects;
using Payroll.Utils.Reports;

namespace Payroll.Controllers;

[Authorize(Roles = "Admin, Manager")]
public class PaymentRecordController : BaseController
{
    readonly IEmployeeService _employeeService;
    readonly IMapper _mapper;
    readonly IPaymentRecordService _paymentRecordService;
    readonly IPaymentSlipService _paymentSlipService;
    readonly ITaxYearService _taxYearService;
    readonly IToastNotification _toast;
    readonly InvoiceService _invoiceService;
    readonly IHttpStatusCodeDescriptionProvider _httpStatusProvider;
    readonly IDeductionService _deductionService;


    public PaymentRecordController
    (
        IEmployeeService employeeService,
        IMapper mapper,
        IPaymentRecordService paymentRecordService,
        ITaxYearService taxYearService,
        IToastNotification toast,
        IPaymentSlipService paymentSlipService,
        InvoiceService invoiceService,
        IHttpStatusCodeDescriptionProvider httpStatusProvider,
        IDeductionService deductionService
    )
    {
        _employeeService = employeeService;
        _mapper = mapper;
        _paymentRecordService = paymentRecordService;
        _taxYearService = taxYearService;
        _toast = toast;
        _paymentSlipService = paymentSlipService;
        _invoiceService = invoiceService;
        _httpStatusProvider = httpStatusProvider;
        _deductionService = deductionService;
    }

    [HttpGet]
    [Route("/payment-record")]
    public async Task<IActionResult> Index(SearchOptions searcher)
    {
        string keyword = ViewBag.keyword = searcher.Keyword!;

        try
        {
            var paymentRecordList = _paymentRecordService.GetPaymentRecordListAsync(keyword)
                .Result
                .Select(dto => _mapper.Map<PaymentRecordViewModel>(dto))
                .ToList();

            return await Task.FromResult<IActionResult>(View(paymentRecordList));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            return RedirectToAction(nameof(Error));
        }
    }

    [HttpGet]
    [Route("/payment-record/create")]
    public async Task<IActionResult> CreatePaymentRecord()
    {
        try
        {
            ViewBag.Employees = new SelectList(await _employeeService.GetEmployeeListAsync(null!), "Id", "Name");
            ViewBag.TaxYears = new SelectList(await _taxYearService.GetTaxYearListAsync(), "Id", "YearOfTax");

            var viewModel = new PaymentRecordViewModel();
            return await Task.FromResult<IActionResult>(View(viewModel));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            return RedirectToAction(nameof(Error));
        }
    }

    [HttpPost]
    [Route("/payment-record/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentRecordViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var dto = await _employeeService.GetEmployeeByIdAsync(viewModel.EmployeeId);
                await ComputePaymentRecord(viewModel, dto);

                var transfer = _mapper.Map<PaymentRecordDto>(viewModel);
                await _paymentRecordService.CreatePaymentRecord(transfer);

                _toast.AddSuccessToastMessage("Payment Record created successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                _toast.AddErrorToastMessage("Error creating new Payment Record");
            }
        }
        else
        {
            ModelState.Values
                .SelectMany(v => v.Errors)
                .ToList()
                .ForEach(error => Logger.LogError(error.ErrorMessage));

            return View("CreatePaymentRecord");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ComputePaymentRecord(PaymentRecordViewModel vm, EmployeeDto dto)
    {
        vm.InsuranceNumber = dto.InsuranceNumber;
        vm.OvertimeHours = await _paymentSlipService.GetOvertimeHours(vm.HoursWorked, vm.ContractualHours);

        // Earnings
        vm.ContractualEarnings = await _paymentSlipService.GetContractualEarning(vm.HoursWorked, vm.ContractualHours, vm.HourlyRate);
        vm.OvertimeRate = await _paymentSlipService.GetOvertimeRate(vm.HourlyRate);
        vm.OvertimeEarnings = await _paymentSlipService.GetOvertimeEarning(vm.OvertimeHours, vm.OvertimeRate);
        vm.TotalEarnings = await _paymentSlipService.GetTotalEarning(vm.OvertimeEarnings, vm.ContractualEarnings);

        // Deduction
        vm.UnionFree = await _deductionService.GetUnionFree(dto.UnionMemberStatus);
        vm.Tax = await _deductionService.GetTaxDeduction(vm.TotalEarnings);
        vm.TotalDeduction = await _deductionService.GetTotalDeduction(vm.UnionFree, vm.Tax);

        // Total net payment
        vm.NetPayment = await _paymentSlipService.GetNetPayment(vm.TotalEarnings, vm.TotalDeduction);
    }

    public async Task<IActionResult> GenerateInvoicePdf(Guid paymentRecordId)
    {
        try
        {
            if (paymentRecordId != Guid.Empty)
            {
                return await _invoiceService.GenerateInvoicePdf(paymentRecordId);
            }
            else
            {
                throw new HttpRequestException("Invalid params", null, HttpStatusCode.BadRequest);
            }
        }
        catch (HttpRequestException ex)
        {
            int? statusCode = (int?) ex.StatusCode;

            if (statusCode.HasValue)
            {
                string statusDescription = _httpStatusProvider.GetStatusDescription(statusCode.Value)!;

                return RedirectToAction("Error", new {statusCode.Value, statusDescription});
            }
            else
            {
                return RedirectToAction(nameof(Error));
            }
        }
    }

    [HttpGet]
    [Route("/payment-record/delete/{id}")]
    public async Task<IActionResult> DeletePaymentRecord(Guid id)
    {
        try
        {
            await _paymentRecordService.DeletePaymentRecordAsync(id);
            _toast.AddSuccessToastMessage("Payment Record created successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            _toast.AddErrorToastMessage("Error deleted Payment Record");
        }

        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet]
    [Route("/payment-record/detail/{id}")]
    public async Task<IActionResult> DetailPaymentRecord(Guid id)
    {
        try
        {
            var dto = await _paymentRecordService.GetPaymentRecordByIdAsync(id);
            var viewModel = _mapper.Map<PaymentRecordViewModel>(dto);

            // string date = viewModel.Created.ToString("dd.MM.yyyy HH:mm");
            // viewModel.Created = Convert.ToDateTime(date);

            return View(viewModel);
        }
        catch (HttpRequestException ex)
        {
            int? statusCode = (int?) ex.StatusCode;

            if (statusCode.HasValue)
            {
                string statusDescription = _httpStatusProvider.GetStatusDescription(statusCode.Value)!;
                return RedirectToAction("Error", new {statusCode.Value, statusDescription});
            }
            else
            {
                return RedirectToAction(nameof(Error));
            }
        }
        catch
        {
            return RedirectToAction("Error", new {statusCode = 400, message = "Bad Request"});
        }
    }
    //
    // [HttpPost]
    // [Route("/payment-record/details/{id}")]
    // [ValidateAntiForgeryToken]
    // public async Task<IActionResult> Details(UserViewModel viewModel)
    // {
    //     try
    //     {
    //         var dto = _mapper.Map<UserDto>(viewModel);
    //         await _userService.UpdateUserAsync(dto);
    //         _toast.AddSuccessToastMessage("User successfully updated");
    //         return RedirectToAction(nameof(Index));
    //     }
    //     catch (HttpRequestException ex)
    //     {
    //         int? statusCode = (int?) ex.StatusCode;
    //
    //         if (statusCode.HasValue)
    //         {
    //             string statusDescription = _httpStatusProvider.GetStatusDescription(statusCode.Value)!;
    //             return RedirectToAction("Error", new {statusCode.Value, statusDescription});
    //         }
    //         else
    //         {
    //             return RedirectToAction(nameof(Error));
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Logger.LogError(ex);
    //         return RedirectToAction(nameof(Error));
    //     }
    // }
}
