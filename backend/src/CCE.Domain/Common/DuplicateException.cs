namespace CCE.Domain.Common;

/// <summary>Raised when a unique-index violation indicates a duplicate value.</summary>
public sealed class DuplicateException : DomainException
{
    public DuplicateException() { }
    public DuplicateException(string message) : base(message) { }
    public DuplicateException(string message, System.Exception innerException) : base(message, innerException) { }
}
