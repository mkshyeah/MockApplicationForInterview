using FluentValidation.Results;

namespace AccountingHelper.Application.Exceptions;

public class AccountingHelperException : Exception
{
    public int StatusCode { get; }
    public string ErrorType { get; }

    protected AccountingHelperException(string message, int statusCode, string errorType)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
    }
}

public class NotFoundException : AccountingHelperException
{
    public NotFoundException(string resource, object id) 
        : base($"{resource} with ID '{id}' was not found", 404, ProblemTypes.NotFound) { }
    
    public NotFoundException(string message)
        : base(message, 404, ProblemTypes.NotFound) { }
}

public class ValidationException : AccountingHelperException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string message)
        : base("One or more validation errors occurred.", 400,
            ProblemTypes.Validation)
    {
        Errors = new Dictionary<string, string[]>
        {
            { JsonPropertyName.FromPropertyPath(field), new[] { message } }
        };
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("One or more validation errors occurred.", 400,
            ProblemTypes.Validation)
    {
        // keys are camelCased here rather than through ValidatorOptions.Global.PropertyNameResolver:
        // that resolver also rewrites {PropertyName} inside default message texts, and it is global
        // mutable state. This keeps the blast radius to one method.
        Errors = failures
            .GroupBy(e => JsonPropertyName.FromPropertyPath(e.PropertyName), e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}

public class ConflictException : AccountingHelperException
{
    public ConflictException(string message) 
        : base(message, 409, ProblemTypes.Conflict) { }
}
public class BusinessRuleException : AccountingHelperException
{
    public BusinessRuleException(string message) 
        : base(message, 422, ProblemTypes.BusinessRule) { }
}


