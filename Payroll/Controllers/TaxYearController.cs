using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using Payroll.Controllers.ViewModels;
using Payroll.Services.Contracts.Services;
using Payroll.Services.Exceptions;
using Payroll.Services.Objects;
using Payroll.Utils.Reports;

namespace Payroll.Controllers;

[Authorize(Roles = "Admin, Manager")]
public class TaxYearController : BaseController
{
    readonly IMapper _mapper;
    readonly ITaxYearService _taxService;
    readonly IToastNotification _toast;

    public TaxYearController(IMapper mapper, ITaxYearService taxService, IToastNotification toast)
    {
        _mapper = mapper;
        _taxService = taxService;
        _toast = toast;
    }

    [HttpGet]
    [Route("/tax-year")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var taxYear = _taxService.GetTaxYearListAsync()
                .Result
                .Select(tax => _mapper.Map<TaxYearViewModel>(tax));

            return await Task.FromResult<IActionResult>(View(taxYear));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            return RedirectToAction(nameof(Error));
        }
    }

    [HttpGet]
    [Route("/tax-year/create")]
    public ActionResult CreateTaxYear()
    {
        return View(new TaxYearViewModel());
    }

    [HttpPost]
    [Route("/tax-year/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxYearViewModel viewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var dto = _mapper.Map<TaxYearDto>(viewModel);
                await _taxService.CreateTaxYearAsync(dto);
                _toast.AddSuccessToastMessage("Tax Year successfully created");
            }
            else
            {
                return View("CreateTaxYear");
            }
        }
        catch (TaxYearExistsException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("CreateTaxYear");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            return RedirectToAction(nameof(Error));
        }

        // Clear the ModelState before returning the view
        // ModelState.Clear();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("/tax-year/delete/{id}")]
    public async Task<IActionResult> DeleteTaxYear(Guid id)
    {
        try
        {
            await _taxService.DeleteTaxYearAsync(id);
            _toast.AddSuccessToastMessage("Tax Year successfully deleted");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            return RedirectToAction(nameof(Error));
        }

        return RedirectToAction(nameof(Index));
    }
}
