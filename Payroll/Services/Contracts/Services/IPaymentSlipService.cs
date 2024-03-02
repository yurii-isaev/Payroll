namespace Payroll.Services.Contracts.Services;

public interface IPaymentSlipService
{
    Task<decimal> GetOvertimeHours(decimal hoursWorked, decimal contractualHours);

    Task<decimal> GetContractualEarning(decimal hoursWorked, decimal contractualHours, decimal hourlyRate);

    Task<decimal> GetOvertimeEarning(decimal overtimeHours, decimal overtimeRate);

    Task<decimal> GetOvertimeRate(decimal hourlyRate);
    
    Task<decimal> GetTotalEarning(decimal overtimeEarnings, decimal contractualEarnings);

    Task<decimal> GetNetPayment(decimal totalEarnings, decimal totalDeduction);
}
