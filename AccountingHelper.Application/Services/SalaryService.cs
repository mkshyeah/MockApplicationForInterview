using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Services;

public class SalaryService : ISalaryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SalaryService> _logger;
    
    public SalaryService(IUnitOfWork unitOfWork, ILogger<SalaryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Salary>> GetSalaryHistory(Guid employeeId, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees.GetStatusAsync(employeeId, ct);
        
        if (status == null)
            throw new NotFoundException("Employee", employeeId);
        
        var salaries = await _unitOfWork.Salaries
            .GetHistoryAsync(employeeId, ct);
        
        return salaries.ToList().AsReadOnly();
    }
}