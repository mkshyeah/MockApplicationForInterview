using AccountingHelper.Domain.Interfaces;
using Serilog.Context;

namespace AccountingHelper.API.Middleware;

public class CorrelationIdLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdLoggingMiddleware(RequestDelegate next)
    {
        _next=next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationIdAccessor accessor)
    {
        using (LogContext.PushProperty("CorrelationId", accessor.CorrelationId))
        {
            await _next(context);
        }
    }
}