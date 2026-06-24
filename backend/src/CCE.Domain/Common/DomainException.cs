namespace CCE.Domain.Common;

/// <summary>
/// Base class for exceptions thrown from the Domain layer when an invariant
/// or business rule is violated by code-controllable input. Distinct from
/// <c>ArgumentException</c> (caller-bug-style preconditions) and from EF
/// constraint violations (handled via <c>DbExceptionMapper</c>).
/// </summary>
/// <remarks>
/// Sub-projects derive concrete types per bounded context, e.g.,
/// <c>DuplicateException</c>, <c>InvalidStatusTransitionException</c>.
/// The API middleware (<c>ExceptionHandlingMiddleware</c>) translates these
/// to a 422 response with the <c>BUSINESS_RULE_VIOLATION</c> error envelope.
/// </remarks>
public class DomainException : Exception
{
    public DomainException() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
