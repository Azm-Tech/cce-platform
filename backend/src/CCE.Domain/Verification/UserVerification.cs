using CCE.Domain.Common;

namespace CCE.Domain.Verification;

[Audited]
public sealed class UserVerification : AggregateRoot<Guid>
{
    private UserVerification() : base(Guid.NewGuid()) { }
    private UserVerification(Guid id) : base(id) { }

    public Guid? UserId { get; private set; }
    public string Contact { get; private set; } = string.Empty;
    public OtpVerificationType TypeId { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }

    public static UserVerification Create(Guid? userId, string contact, OtpVerificationType typeId)
        => new(Guid.NewGuid())
        {
            UserId = userId,
            Contact = contact,
            TypeId = typeId,
            IsVerified = false,
        };

    public void MarkVerified(DateTimeOffset now)
    {
        IsVerified = true;
        VerifiedAt = now;
    }
}
