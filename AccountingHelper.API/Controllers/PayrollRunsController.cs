using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Payroll.Queries.GetPayrollRun;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/payroll-runs")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PayrollRunsController : ControllerBase
{
    private readonly ISender _sender;

    public PayrollRunsController(
        ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PayrollRunResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunResponse>> CreatePayrollRun(
        [FromBody] CreatePayrollRunRequest request,
        CancellationToken ct = default)
    {
        var payrollRun = await _sender.Send(request.ToCommand(), ct);

        return AcceptedAtAction(nameof(GetPayrollRun), new { id = payrollRun.Id }, payrollRun);
    }
    
    [HttpGet]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PayrollRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollRunResponse>> GetPayrollRun(Guid id, CancellationToken ct=default)
    {
        var payroll = await _sender.Send(new GetPayrollRunQuery(id), ct);
        return Ok(payroll);   
    }
}