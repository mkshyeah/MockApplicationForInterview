using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;
using AccountingHelper.Application.Features.LeaveRequests.Queries.GetLeaveRequest;
using AccountingHelper.Application.Features.LeaveRequests.Queries.GetLeaveRequests;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class LeaveRequestController : ControllerBase
{
    private readonly ISender _sender;

    public LeaveRequestController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("employees/{employeeId:guid}/leave-requests")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestResponse>>> GetLeaveRequests(
        Guid employeeId,
        CancellationToken ct = default)
    {
        var leaveRequests = await _sender.Send(new GetLeaveRequestsQuery(employeeId), ct);
        
        return Ok(leaveRequests.Select(leaveRequest => leaveRequest.ToResponse()).ToList());
    }

    [HttpGet("leave-requests/{id:guid}")]
    [ProducesResponseType(typeof(LeaveRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestResponse>> GetLeaveRequest(
        Guid id,
        CancellationToken ct = default)
    {
        var leaveRequest = await _sender.Send(new GetLeaveRequestQuery(id), ct);
        return Ok(leaveRequest.ToResponse());
    }
    
    [HttpPost("employees/{employeeId:guid}/leave-requests")]
    [ProducesResponseType(typeof(LeaveRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeaveRequestResponse>> SubmitLeaveRequest(
        Guid employeeId,
        [FromBody] SubmitLeaveRequestRequest request,
        CancellationToken ct = default
    )
    {
        var leaveRequest = await _sender.Send(request.ToCommand(employeeId), ct);
        
        return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, leaveRequest.ToResponse());
    }

    [HttpPatch("leave-requests/{id:guid}/approve")]
    [ProducesResponseType(typeof(LeaveRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LeaveRequestResponse>> ApproveLeaveRequest(
        Guid id,
        CancellationToken ct = default)
    {
        var leaveRequest = await _sender.Send(new ApproveLeaveRequestCommand(id), ct);
        return Ok(leaveRequest.ToResponse());
    }
    
    
    
}