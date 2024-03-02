using System.ComponentModel.DataAnnotations;
using Payroll.Domains.Entities;

namespace Payroll.Controllers.ViewModels;

public class PaymentRecordViewModel
{
    // [HiddenInput(DisplayValue = true)]
    public Guid Id { get; set; }

    [
        Display(Name = "Employee"),
        Required
    ]
    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string? Name { get; set; }

    public string? InsuranceNumber { get; set; }

    [Display(Name = "Pay Date")]
    public DateTime PayDate { get; set; } = DateTime.UtcNow;

    [
        Display(Name = "Pay Month"),
        Required
    ]
    public string? PayMonth { get; set; } = DateTime.Today.Month.ToString();

    [
        Display(Name = "Tax Year"),
        Required
    ]
    public Guid TaxYearId { get; set; }

    public TaxYear? TaxYear { get; set; }

    public string? TaxCode { get; set; } = "103";

    [
        Display(Name = "Hourly Rate"),
        Required
    ]
    public decimal HourlyRate { get; set; }

    [
        Display(Name = "Hours Worked"),
        Required
    ]
    public decimal HoursWorked { get; set; }

    [Display(Name = "Contractual Hours")]
    public decimal ContractualHours { get; set; }

    [Display(Name = "Overtime Hours")]
    public decimal OvertimeHours { get; set; }
    
    [Display(Name = "Overtime Rate")]
    public decimal OvertimeRate { get; set; }

    [Display(Name = "Contractual Earnings")]
    public decimal ContractualEarnings { get; set; }

    [Display(Name = "Overtime Earnings")]
    public decimal OvertimeEarnings { get; set; }

    public decimal Tax { get; set; }

    [Display(Name = "Union Free")]
    public decimal UnionFree { get; set; }

    [Display(Name = "Total Earnings")]
    public decimal TotalEarnings { get; set; }

    [Display(Name = "Total Deduction")]
    public decimal TotalDeduction { get; set; }

    [Display(Name = "Net Payment")]
    public decimal NetPayment { get; set; }
}
