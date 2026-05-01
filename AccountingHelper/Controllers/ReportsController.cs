using AccountingHelper.Domain.Enums;
using AccountingHelper.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;


namespace AccountingHelper.Controllers;

/// <summary>
///     Provides endpoints for generating various reports related to the accounting system.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/reporting")]
public class ReportsController : ApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }
    
    [HttpGet("employees/count")]
    public async Task<IActionResult> GetEmployeeCount(CancellationToken ct)
    {
        var result = await _reportService.CountEmployees(ct);
        
        return result.Match(
            count => Ok(new { TotalCount = count }),
            errors => Problem(errors));
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetEmployeeStatus(Guid id, CancellationToken ct)
    {
        var result = await _reportService.GetEmployeeStatus(id, ct);
        
        return result.Match(
            status => Ok(new {Status=status}),
            errors => Problem(errors));
    }

    [HttpGet("salaries")]
    public async Task<IActionResult> GetSalaries(CancellationToken ct)
    {
        var result = await _reportService.GetTotalSalaries(ct);

        return result.Match(
            salaries => Ok(new {TotalSum=salaries}),
            errors => Problem(errors));
    }

    [HttpGet("salary/{id:guid}")]
    public async Task<IActionResult> GetSalaryByType(
        Guid id, 
        [FromQuery] SalaryType type,
        CancellationToken ct)
    {
        var result = await _reportService.GetSalaryByType(id, type, ct);

        return result.Match(
            salaries => Ok(new {Salaries=salaries}),
            errors => Problem(errors));
    }

    [HttpGet("calculate-taxes/{id:guid}")]
    public async Task<IActionResult> CalculateTaxes(Guid id, CancellationToken ct)
    {
        var result = await _reportService.CalculateTaxes(id, ct);

        return result.Match(
            taxes => Ok(new {Taxes=taxes}),
            errors => Problem(errors));
    }
}