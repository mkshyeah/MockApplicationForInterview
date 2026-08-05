using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.LeaveBalances.Queries.GetLeaveBalances;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/employees")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class LeaveBalanceController : ControllerBase
{
    private readonly ISender _sender;
    public LeaveBalanceController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{employeeId:guid}/leave-balances")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceResponse>>> GetLeaveBalances(
        Guid employeeId,
        CancellationToken ct = default)
    {
        var balances = await _sender.Send(new GetLeaveBalancesQuery(employeeId), ct);
        
        return Ok(balances.Select(b => b.ToResponse()).ToList());
    }
}