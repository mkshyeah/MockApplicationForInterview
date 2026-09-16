using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class PayslipRepository : IPayslipRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PayslipRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public void AddRange(IEnumerable<Payslip> payslips) =>
        _dbContext.Payslips.AddRange(payslips.Select(p => p.ToEntity()));

    public async Task<IReadOnlyList<Payslip>> GetByRunAsync(Guid payrollRunId, CancellationToken ct)
    {
        var payslips = await _dbContext.Payslips
            .AsNoTracking()
            .Where(p => p.PayrollRunId == payrollRunId)
            .ToListAsync(ct);
        
        return payslips.Select(p => p.ToModel()).ToList();
        
    }
}
