namespace Payroll.Services.Contracts.Services;

public interface IDeductionService
{
    Task<decimal> GetTaxDeduction(decimal totalEarnings);

    Task<decimal> GetUnionFree(bool unionMemberStatus);

    Task<decimal> GetTotalDeduction(decimal unionFree, decimal tax);
}
