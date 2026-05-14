using System.ComponentModel.DataAnnotations;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Domain.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;


namespace AccountingHelper.API.Controllers;

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
    public async Task<IActionResult> GetEmployeeCount(CancellationToken ct=default)
    {
        var count = await _reportService.CountEmployees(ct);
        return Ok(count);
    }

    [HttpGet("employees/{id:guid}/status")]
    public async Task<IActionResult> GetEmployeeStatus(Guid id, CancellationToken ct=default)
    {
        var employeeStatus = await _reportService.GetEmployeeStatus(id, ct);
        
        return Ok(employeeStatus);
    }

    [HttpGet("salaries")]
    public async Task<IActionResult> GetSalaries(CancellationToken ct=default)
    {
        var salaries = await _reportService.GetTotalSalaries(ct);

        return Ok(salaries);
    }

    [HttpGet("employees/{id:guid}/salary")]
    public async Task<IActionResult> GetSalaryByType(
        Guid id, 
        [FromQuery][Required] SalaryType type,
        CancellationToken ct=default)
    {
        var salaryByType = await _reportService.GetSalaryByType(id, type, ct);

        return Ok(salaryByType);
    }

    [HttpGet("employees/{id:guid}/taxes")]
    public async Task<IActionResult> CalculateTaxes(Guid id, CancellationToken ct=default)
    {
        var taxes = await _reportService.CalculateTaxes(id, ct);
        
        return Ok(taxes);
    }
}