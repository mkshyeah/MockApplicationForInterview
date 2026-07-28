using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Interfaces;
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
    private readonly ISalaryService _salaryService;
    private readonly ISender _sender;

    public SalaryController(
        ISalaryService salaryService,
        ISender sender)
    {
        _salaryService = salaryService;
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
        var result = await _salaryService.GetSalaryHistory(employeeId, ct);
        
        var response = result.Select(s => s.ToResponse()).ToList().AsReadOnly();
        return Ok(response);
    }
    
}