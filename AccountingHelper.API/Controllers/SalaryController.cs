using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Salaries.Queries.GetSalaryHistory;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Controllers;


[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/employees/{employeeId:guid}/salaries")]
public class SalaryController : ControllerBase
{
    private readonly ISender _sender;

    public SalaryController(
        ISender sender)
    {
        _sender = sender;
    }

    [HttpPut]
    [ProducesResponseType(typeof(SalaryResponse), StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeSalary(
        Guid employeeId,
        [FromBody] ChangeSalaryRequest request,
        CancellationToken ct = default)
    {
        var salary = await _sender.Send(request.ToCommand(employeeId), ct);
        
        return Ok(salary.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryResponse>), StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalaryHistory(Guid employeeId, CancellationToken ct = default)
    {
        var salaries = await _sender.Send(new GetSalaryHistoryQuery(employeeId), ct);

        var response = salaries.Select(s => s.ToResponse()).ToList();
        return Ok(response);
    }
    
}