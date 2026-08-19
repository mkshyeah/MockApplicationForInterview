using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Positions.Queries.GetPositions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/positions")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class PositionsController : ControllerBase
{
    private readonly ISender _sender;

    public PositionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PositionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PositionResponse>>> GetPositions(CancellationToken ct = default)
    {
        var positions = await _sender.Send(new GetPositionsQuery(), ct);

        return Ok(positions);
    }
}
