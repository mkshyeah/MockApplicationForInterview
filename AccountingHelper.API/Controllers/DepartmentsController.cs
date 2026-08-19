using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Departments.Queries.GetDepartments;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/departments")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class DepartmentsController : ControllerBase
{
    private readonly ISender _sender;

    public DepartmentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentResponse>>> GetDepartments(CancellationToken ct = default)
    {
        var departments = await _sender.Send(new GetDepartmentsQuery(), ct);

        return Ok(departments.Select(d => d.ToResponse()).ToList());
    }
}
