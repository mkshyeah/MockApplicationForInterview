namespace AccountingHelper.Application.Exceptions;

/// <summary>
/// RFC links used as the "type" member of every ProblemDetails response.
/// Kept in one place so the exception handler and the model-state factory cannot drift apart.
/// </summary>
public static class ProblemTypes
{
    public const string Validation = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
    public const string NotFound = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4";
    public const string Conflict = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8";
    public const string BusinessRule = "https://datatracker.ietf.org/doc/html/rfc2518#section-10.3";
    public const string Cancelled = "https://tools.ietf.org/html/rfc7231#section-6.5";
    public const string InternalServerError = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
}
