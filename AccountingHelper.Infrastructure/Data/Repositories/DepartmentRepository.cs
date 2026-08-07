using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _dbContext;
    public DepartmentRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        
        return entity?.ToModel();
    }
}