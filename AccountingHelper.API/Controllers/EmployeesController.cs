using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Employees.Commands.FireEmployee;
using AccountingHelper.Application.Features.Employees.Commands.SendEmployeeOffVacation;
using AccountingHelper.Application.Features.Employees.Commands.SendEmployeeOnVacation;
using AccountingHelper.Application.Features.Employees.Queries.GetEmployee;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/employees")]
public class EmployeesController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeesController(
        ISender sender)
    {
        _sender = sender;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] EmployeeFilteredRequest request,
        CancellationToken ct=default)
    {
        var query = request.ToQuery();
        var (employees, total) = await _sender.Send(query, ct);
        
        var response = PagedResponse<EmployeeResponse>.Create(
            employees.Select(e => e.ToResponse()).ToList(),
            total,
            query.Limit,
            query.Offset);

        return Ok(response);
    }


    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct=default)
    {
        var employee = await _sender.Send(new GetEmployeeQuery(id), ct);

        var response =  employee.ToResponse();
        
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct=default)
    {
        var employee = await _sender.Send(request.ToCommand(), ct);

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee.ToResponse());
    }

    [HttpPatch("{id:guid}/fire")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeResponse>> FireEmployee(Guid id, CancellationToken ct=default)
    {
        var employee = await _sender.Send(new FireEmployeeCommand(id), ct);
        return Ok(employee.ToResponse());
    }

    [HttpPatch("{id:guid}/on-vacation")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendOnVacation(Guid id, CancellationToken ct = default)
    {
        var employee = await _sender.Send(new SendEmployeeOnVacationCommand(id), ct);
        return Ok(employee.ToResponse());
    }
    
    [HttpPatch("{id:guid}/off-vacation")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendOffVacation(Guid id, CancellationToken ct = default)
    {
        var employee = await _sender.Send(new SendEmployeeOffVacationCommand(id), ct);
        return Ok(employee.ToResponse());
    }
}