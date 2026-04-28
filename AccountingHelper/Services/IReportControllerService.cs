using AccountingHelper.Models;

namespace AccountingHelper.Services;

public interface IReportControllerService
{
    public Task<int> CountEmployees();
    public Task<string> GetEmployeeStatus(string id);

    public Task<int> GetTotalSalaries();

    Task<decimal> GetSalaryByType(Employee id, string type);

    Task<decimal> CalculateTaxes(Employee id);
}