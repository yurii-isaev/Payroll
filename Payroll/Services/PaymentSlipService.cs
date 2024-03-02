using Payroll.Services.Contracts.Services;

namespace Payroll.Services;

public class PaymentSlipService : IPaymentSlipService
{
    public Task<decimal> GetContractualEarning(decimal hoursWorked, decimal contractualHours, decimal hourlyRate)
    {
        decimal contractualEarning = 0;

        if (hoursWorked < contractualHours)
        {
            contractualEarning = hourlyRate * hoursWorked;
        }
        else
        {
            contractualEarning = hourlyRate * contractualHours;
        }

        return Task.FromResult(contractualEarning);
    }

    public Task<decimal> GetOvertimeHours(decimal hoursWorked, decimal contractualHours)
    {
        decimal overtimeHours = 0;

        if (hoursWorked <= contractualHours)
        {
            overtimeHours = 0.00m;
        }
        else if (hoursWorked > contractualHours)
        {
            overtimeHours = hoursWorked - contractualHours;
        }

        return Task.FromResult(overtimeHours);
    }

    public Task<decimal> GetOvertimeEarning(decimal overtimeHours, decimal overtimeRate)
    {
        return Task.FromResult((overtimeHours * overtimeRate));
    }

    public Task<decimal> GetOvertimeRate(decimal hourlyRate)
    {
        return Task.FromResult((1.5m * hourlyRate));
    }

    public Task<decimal> GetTotalEarning(decimal overtimeEarnings, decimal contractualEarnings)
    {
        return Task.FromResult((overtimeEarnings + contractualEarnings));
    }

    public Task<decimal> GetNetPayment(decimal totalEarnings, decimal totalDeduction)
    {
        return Task.FromResult(totalEarnings - totalDeduction);
    }
}
