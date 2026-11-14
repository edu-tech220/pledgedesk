namespace PledgeDesk.Api.Domain;

/// <summary>
/// Thrown when a business rule rejects an operation (invalid state transition, stock
/// exhausted, deadline passed, etc). Caught centrally and mapped to a 400/409
/// ProblemDetails response - see ApiExceptionMiddleware.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
