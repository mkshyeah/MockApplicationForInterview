using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ValidationException = AccountingHelper.Application.Exceptions.ValidationException;

namespace AccountingHelper.Application.Behaviors;

public class ValidationBehavior<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
        
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next(ct);

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();
        
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, ct);
            if (!result.IsValid)
                failures.AddRange(result.Errors);
        }

        if (failures.Count != 0)
        {
            throw new ValidationException(failures); 
        }

        return await next(ct);
    }
}