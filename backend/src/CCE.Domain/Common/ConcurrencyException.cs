namespace CCE.Domain.Common;

/// <summary>Raised when an optimistic-concurrency token mismatch occurs.</summary>
public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException() { }
    public ConcurrencyException(string message) : base(message) { }
    public ConcurrencyException(string message, System.Exception innerException) : base(message, innerException) { }
}
