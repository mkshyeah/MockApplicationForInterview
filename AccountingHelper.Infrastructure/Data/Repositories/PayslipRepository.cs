using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class PayslipRepository : IPayslipRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PayslipRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public void AddRange(IEnumerable<Payslip> payslips) =>
        _dbContext.Payslips.AddRange(payslips.Select(p => p.ToEntity()));
}
