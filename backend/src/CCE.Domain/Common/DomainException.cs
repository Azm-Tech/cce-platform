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
/// Phase 08 middleware translates these to RFC 7807 ProblemDetails.
/// </remarks>
public class DomainException : Exception
{
    public DomainException() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
