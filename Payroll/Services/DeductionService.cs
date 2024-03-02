using Payroll.Services.Contracts.Services;

namespace Payroll.Services;

public class DeductionService : IDeductionService
{
    public Task<decimal> GetTaxDeduction(decimal totalEarnings)
    {
        decimal slab1 = 14000m;
        decimal slab1Percent = .105m;
        decimal slab2 = 48000m;
        decimal slab2Percent = .175m;
        decimal slab3 = 70000m;
        decimal slab3Percent = .300m; 
        decimal maxPercent = .33m;
        decimal taxDeduction = 0m;

        if (totalEarnings <= slab1)
        {
            taxDeduction = totalEarnings * slab1Percent;
        }
        else if (totalEarnings > slab1 && totalEarnings <= slab2)
        {
            taxDeduction = totalEarnings * slab2Percent;
        }
        else if (totalEarnings > slab2 && totalEarnings <= slab3)
        {
            taxDeduction = totalEarnings * slab3Percent;
        }
        else if (totalEarnings > slab3)
        {
            taxDeduction = totalEarnings * maxPercent;
        }

        return Task.FromResult(taxDeduction);
    }
    
    public Task<decimal> GetUnionFree(bool unionMemberStatus)
    {
        return Task.FromResult((unionMemberStatus) ? 100m : 0);
    }
    
    public Task<decimal> GetTotalDeduction(decimal unionFree, decimal tax)
    {
        return Task.FromResult(unionFree + tax);
    }
}
