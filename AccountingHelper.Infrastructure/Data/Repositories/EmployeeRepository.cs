using System.Linq.Expressions;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Data.Entities;
using AccountingHelper.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Infrastructure.Data.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
   private readonly ApplicationDbContext _dbContext;
   public EmployeeRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;
   
   public async Task<IEnumerable<Employee>> GetAllAsync(
      int offset, int limit, SortDirection direction,
      CancellationToken ct)
   {
      var query = _dbContext.Employees
         .Include(e => e.Department)
         .Include(e => e.Position)
         .Include(e => e.Salaries)
         .AsNoTracking();
      
      query = direction == SortDirection.Ascending 
         ? query.OrderBy(e => e.CreatedAt)
         : query.OrderByDescending(e => e.CreatedAt);
      
      var entities = await query
         .Skip(offset)
         .Take(limit)
         .ToListAsync(ct);
      
      return entities.Select(e => e.ToModel());
   }

   public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
   {
      var entity = await _dbContext.Employees
         .Include(e => e.Department)
         .Include(e => e.Salaries)
         .Include(e => e.Position)
         .AsNoTracking()
         .FirstOrDefaultAsync(e => e.Id == id, ct);

      return entity?.ToModel();
   }

   public void Add(Employee model) => _dbContext.Employees.Add(model.ToEntity());

   public void Update(Employee model)
   {
      var entity = model.ToEntity();
      entity.UpdatedAt = DateTime.UtcNow;
      _dbContext.Entry(entity).State = EntityState.Modified;
   }
   
   public async Task DeleteAsync(Guid id, CancellationToken ct)
   {
      var entity = await _dbContext.Employees.FindAsync([id], ct);

      if (entity != null)
         _dbContext.Employees.Remove(entity);
   }

   public async Task<int> CountAsync(CancellationToken ct)
   {
      return await _dbContext.Employees.CountAsync(ct);
   }

   public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
   {
      return await _dbContext.Employees
         .AnyAsync(e => e.Email == email, ct);
   }

   public async Task<EmployeeStatus?> GetStatusAsync(Guid employeeId, CancellationToken ct)
   {
      return await _dbContext.Employees
         .Where(e => e.Id == employeeId)
         .Select(e => (EmployeeStatus?)e.Status)
         .FirstOrDefaultAsync(ct);
   }

   public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetFilteredAsync(
      int offset, int limit,
      EmployeeOrderBy orderBy, SortDirection direction,
      Guid? departmentId, EmployeeStatus? status,
      CancellationToken ct)
   {
      var filtered = _dbContext.Employees.AsNoTracking().AsQueryable();

      if (departmentId.HasValue)
         filtered = filtered.Where(e => e.DepartmentId == departmentId);

      if (status.HasValue)
         filtered = filtered.Where(e => e.Status == status);
      
      Expression<Func<EmployeeEntity, object>> keySelector = orderBy switch
      {
         EmployeeOrderBy.Name       => e => e.FirstName,
         EmployeeOrderBy.HireDate   => e => e.HireDate,
         EmployeeOrderBy.Status     => e => e.Status,
         EmployeeOrderBy.Department => e => e.Department!.Name,
         _                          => e => e.CreatedAt
      };

      var ordered = direction == SortDirection.Ascending
         ? filtered.OrderBy(keySelector)
         : filtered.OrderByDescending(keySelector);

      var pageQuery = ordered
         .Include(e => e.Department)
         .Include(e => e.Position)
         .Include(e => e.Salaries.Where(s => s.EndDate==null))
         .Skip(offset)
         .Take(limit)
         .Select(e => new { Entity = e, TotalCount = filtered.Count() });

      var rows = await pageQuery.ToListAsync(ct);

      // Пустая страница (offset за концом данных): строк для чтения TotalCount нет,
      // поэтому берём total отдельным COUNT. Осознанный второй round-trip только на
      // вырожденном краевом случае — цель "один запрос" сохраняется на обычных страницах.
      var items = rows.Select(r => r.Entity.ToModel()).ToList();
      var total = rows.Count > 0
         ? rows[0].TotalCount
         : await filtered.CountAsync(ct);

      return (items, total);
   }
}