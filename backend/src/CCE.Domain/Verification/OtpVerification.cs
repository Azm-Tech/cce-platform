using CCE.Domain.Common;

namespace CCE.Domain.Verification;

[Audited]
public sealed class OtpVerification : AggregateRoot<Guid>
{
    private OtpVerification() : base(Guid.NewGuid()) { }
    private OtpVerification(Guid id) : base(id) { }

    public string Contact { get; private set; } = string.Empty;
    public OtpVerificationType TypeId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastSentAt { get; private set; }
    public int AttemptCount { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsInvalidated { get; private set; }

    public static OtpVerification Create(
        string contact,
        OtpVerificationType typeId,
        string codeHash,
        DateTimeOffset now)
    {
        return new OtpVerification(Guid.NewGuid())
        {
            Contact = contact,
            TypeId = typeId,
            CodeHash = codeHash,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now,
            LastSentAt = now,
            AttemptCount = 0,
            IsVerified = false,
            IsInvalidated = false,
        };
    }

    public bool CanResend(DateTimeOffset now)
        => LastSentAt is null || (now - LastSentAt.Value).TotalSeconds >= 60;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool HasExceededMaxAttempts() => AttemptCount >= 5;

    public void Refresh(string newCodeHash, DateTimeOffset now)
    {
        CodeHash = newCodeHash;
        ExpiresAt = now.AddMinutes(5);
        LastSentAt = now;
        AttemptCount = 0;
        IsInvalidated = false;
    }

    public void IncrementAttempt() => AttemptCount++;

    public void MarkVerified() => IsVerified = true;

    public void Invalidate() => IsInvalidated = true;
}
