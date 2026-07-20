using AccountingHelper.Domain.Interfaces;

namespace AccountingHelper.API.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";
    
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        accessor.CorrelationId = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });
        
        context.Items[CorrelationIdHeader] = correlationId;
        
        await _next(context);
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if(context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId)
           && !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId.ToString();
        }
        
        return Guid.NewGuid().ToString("N");
    }
}