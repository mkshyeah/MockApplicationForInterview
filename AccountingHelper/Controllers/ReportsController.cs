using AccountingHelper.Contexts;
using AccountingHelper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Controllers;

/// <summary>
///     Provides endpoints for generating various reports related to the accounting system.
/// </summary>
[ApiController]
[Route("reporting")]
public class ReportsController(
    IReportControllerService reportControllerService,
    ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("employee-count")]
    public async Task<IActionResult> GetEmployeeCount()
    {
        return Ok(await reportControllerService.CountEmployees());
    }

    [HttpGet("employee-status/{id}")]
    public async Task<IActionResult> GetEmployeeStatus(string id)
    {
        return Ok(await reportControllerService.GetEmployeeStatus(id));
    }

    [HttpGet("salaries")]
    public async Task<IActionResult> GetSalaries()
    {
        return Ok(await reportControllerService.GetTotalSalaries());
    }

    [HttpGet("get-salary-by-type/{id}/{type}")]
    public async Task<IActionResult> GetSalaryByType(string id, string type)
    {
        var employee = await dbContext.Employees.FirstAsync(e => e.Id == id);

        return Ok(await reportControllerService.GetSalaryByType(employee, type));
    }

    [HttpGet("calculate-taxes/{id}")]
    public async Task<IActionResult> CalculateTaxes(string id)
    {
        var employee = await dbContext.Employees.FirstAsync(e => e.Id == id);

        return Ok(await reportControllerService.CalculateTaxes(employee));
    }
}