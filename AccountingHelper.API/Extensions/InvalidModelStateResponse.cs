using AccountingHelper.API.Middleware;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Extensions;

/// <summary>
/// Builds the 400 response for model-binding and deserialization failures.
/// The default factory emits a different envelope than <see cref="Middleware.GlobalExceptionHandler"/>
/// (other title, no detail/instance, traceId instead of correlationId), which would leave the API
/// with two shapes of validation error. This one mirrors the handler.
/// </summary>
public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        // order matters: both the noise filter and the message cleanup recognise deserialization
        // errors by their "$." prefix, so the prefix may only be stripped last
        errors = DropBoundParameterNoise(errors);
        errors = StripSerializerDiagnostics(errors);
        errors = StripJsonPathPrefix(errors);

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = ProblemTypes.Validation,
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["correlationId"] = context.HttpContext.RequestServices
            .GetRequiredService<ICorrelationIdAccessor>()
            .CorrelationId;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { GlobalExceptionHandler.ProblemJsonContentType }
        };
    }

    /// <summary>
    /// When the body fails to deserialize, the action parameter stays null and [ApiController]
    /// adds "The request field is required." under the C# parameter name. The client never sent
    /// a field by that name, so it is noise whenever a JSON path already points at the real cause.
    /// </summary>
    private static Dictionary<string, string[]> DropBoundParameterNoise(Dictionary<string, string[]> errors)
    {
        var jsonPathKeys = errors.Keys.Where(key => key.StartsWith("$.")).ToList();

        return jsonPathKeys.Count > 0
            ? jsonPathKeys.ToDictionary(key => key, key => errors[key])
            : errors;
    }

    /// <summary>
    /// System.Text.Json appends " Path: $.leaveType | LineNumber: 0 | BytePositionInLine: 26."
    /// to the message of any JsonException a converter throws. The path is already carried by the
    /// error key, and line/byte positions are debugging detail that has no place in a public
    /// contract, so the tail is cut off.
    /// </summary>
    private static Dictionary<string, string[]> StripSerializerDiagnostics(Dictionary<string, string[]> errors) =>
        errors.ToDictionary(
            entry => entry.Key,
            entry => entry.Key.StartsWith("$.")
                ? entry.Value.Select(CutPathSuffix).ToArray()
                : entry.Value);

    private static string CutPathSuffix(string message)
    {
        var pathIndex = message.IndexOf(" Path: ", StringComparison.Ordinal);
        return pathIndex < 0 ? message : message[..pathIndex].TrimEnd();
    }

    /// <summary>
    /// System.Text.Json already reports path segments under their serialized (camelCase) names,
    /// so only the leading "$." has to go for the key to match the field the client sent.
    /// </summary>
    private static Dictionary<string, string[]> StripJsonPathPrefix(Dictionary<string, string[]> errors) =>
        errors.ToDictionary(
            entry => entry.Key.StartsWith("$.") ? entry.Key[2..] : entry.Key,
            entry => entry.Value);
}
