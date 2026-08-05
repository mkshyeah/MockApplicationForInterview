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
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<EmployeeResponse>>> GetEmployees(
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(Guid id, CancellationToken ct=default)
    {
        var employee = await _sender.Send(new GetEmployeeQuery(id), ct);

        var response =  employee.ToResponse();
        
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct=default)
    {
        var employee = await _sender.Send(request.ToCommand(), ct);

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee.ToResponse());
    }

    [HttpPatch("{id:guid}/fire")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeResponse>> FireEmployee(
        Guid id,
        CancellationToken ct=default)
    {
        var employee = await _sender.Send(new FireEmployeeCommand(id), ct);
        return Ok(employee.ToResponse());
    }

    [HttpPatch("{id:guid}/on-vacation")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeResponse>> SendOnVacation(
        Guid id, 
        CancellationToken ct = default)
    {
        var employee = await _sender.Send(new SendEmployeeOnVacationCommand(id), ct);
        return Ok(employee.ToResponse());
    }
    
    [HttpPatch("{id:guid}/off-vacation")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeResponse>> SendOffVacation(
        Guid id,
        CancellationToken ct = default)
    {
        var employee = await _sender.Send(new SendEmployeeOffVacationCommand(id), ct);
        return Ok(employee.ToResponse());
    }
}