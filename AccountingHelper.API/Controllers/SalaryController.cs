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
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SalaryResponse>> ChangeSalary(
        Guid employeeId,
        [FromBody] ChangeSalaryRequest request,
        CancellationToken ct = default)
    {
        var salary = await _sender.Send(request.ToCommand(employeeId), ct);
        
        return Ok(salary.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SalaryResponse>>> GetSalaryHistory(Guid employeeId, CancellationToken ct = default)
    {
        var salaries = await _sender.Send(new GetSalaryHistoryQuery(employeeId), ct);

        var response = salaries.Select(s => s.ToResponse()).ToList();
        return Ok(response);
    }
    
}