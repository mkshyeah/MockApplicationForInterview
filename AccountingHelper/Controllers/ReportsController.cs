using AccountingHelper.Contexts;
using AccountingHelper.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Controllers;

/// <summary>
///     Provides endpoints for generating various reports related to the accounting system.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/reporting")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }
    
    [HttpGet("employees/count")]
    public async Task<IActionResult> GetEmployeeCount(CancellationToken ct)
    {
        var count = await _reportService.CountEmployees(ct);
        return Ok(new { TotalCount = count });
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetEmployeeStatus(Guid id, CancellationToken ct)
    {
        var status = await _reportService.GetEmployeeStatus(id, ct);
        return Ok(new { status });
    }

    [HttpGet("salaries")]
    public async Task<IActionResult> GetSalaries(CancellationToken ct)
    {
        var salaries = await _reportService.GetTotalSalaries(ct);
        return Ok(new { totalSum = salaries });
    }

    [HttpGet("salary/{id:guid}")]
    public async Task<IActionResult> GetSalaryByType(
        Guid id, 
        [FromQuery] string type,
        CancellationToken ct)
    {
        var salary = await _reportService.GetSalaryByType(id, type, ct);
        return Ok(new { salary });
    }

    [HttpGet("calculate-taxes/{id:guid}")]
    public async Task<IActionResult> CalculateTaxes(Guid id, CancellationToken ct)
    {
        var taxes = await _reportService.CalculateTaxes(id, ct);

        return Ok(new { taxes });
    }
}