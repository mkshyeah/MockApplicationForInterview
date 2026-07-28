using System.ComponentModel.DataAnnotations;
using AccountingHelper.Application.Features.Reports.Queries.GetCalculateTaxes;
using AccountingHelper.Application.Features.Reports.Queries.GetEmployeeCount;
using AccountingHelper.Application.Features.Reports.Queries.GetEmployeeStatus;
using AccountingHelper.Application.Features.Reports.Queries.GetSalaryByType;
using AccountingHelper.Application.Features.Reports.Queries.GetTotalSalaries;
using AccountingHelper.Domain.Enums;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/reporting")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(
        ISender sender)
    {
        _sender = sender;
    }
    
    [HttpGet("employees/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeeCount(CancellationToken ct=default)
    {
        var count = await _sender.Send(new GetEmployeeCountQuery(), ct);
        return Ok(count);
    }

    [HttpGet("employees/{id:guid}/status")]
    [ProducesResponseType(typeof(EmployeeStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeStatus(Guid id, CancellationToken ct=default)
    {
        var employeeStatus = await _sender.Send(new GetEmployeeStatusQuery(id), ct);
        
        return Ok(employeeStatus);
    }

    [HttpGet("salaries")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalaries(CancellationToken ct=default)
    {
        var salaries = await _sender.Send(new GetTotalSalariesQuery(), ct);

        return Ok(salaries);
    }

    [HttpGet("employees/{id:guid}/salary")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSalaryByType(
        Guid id, 
        [FromQuery][Required] SalaryType type,
        CancellationToken ct=default)
    {
        var salaryByType = await _sender.Send(new GetSalaryByTypeQuery(id, type), ct);

        return Ok(salaryByType);
    }

    [HttpGet("employees/{id:guid}/taxes")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateTaxes(Guid id, CancellationToken ct=default)
    {
        var taxes = await _sender.Send(new GetCalculateTaxesQuery(id), ct);
        
        return Ok(taxes);
    }
}