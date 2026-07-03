using FluentValidation.Results;

namespace AccountingHelper.Application.Exceptions;

public class AccountingHelperException : Exception
{
    public int StatusCode { get; }
    
    protected AccountingHelperException(string message,int statusCode)
        :base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : AccountingHelperException
{
    public NotFoundException(string message) : base(message,404) { }
}

public class ConflictException : AccountingHelperException
{
    public ConflictException(string message) : base(message, 409) { }
}

public class ValidationException : AccountingHelperException
{
    public IEnumerable<ValidationFailure>? Failures { get; }

    public ValidationException(string message) : base(message, 400)
    {
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this(failures.ToList())
    {
    }

    private ValidationException(List<ValidationFailure> cachedFailures)
        : base(string.Join(";\n", cachedFailures.Select(f => $"{f.PropertyName}:{f.ErrorMessage}")), 400)
    {
        Failures = cachedFailures;
    }

}
