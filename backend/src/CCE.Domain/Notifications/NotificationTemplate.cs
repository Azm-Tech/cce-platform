using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

[Audited]
public sealed class NotificationTemplate : Entity<System.Guid>
{
    private static readonly Regex CodePattern = new("^[A-Z][A-Z0-9_]+$", RegexOptions.Compiled);

    private NotificationTemplate(System.Guid id, string code,
        string subjectAr, string subjectEn, string bodyAr, string bodyEn,
        NotificationChannel channel, string variableSchemaJson) : base(id)
    {
        Code = code;
        SubjectAr = subjectAr; SubjectEn = subjectEn;
        BodyAr = bodyAr; BodyEn = bodyEn;
        Channel = channel; VariableSchemaJson = variableSchemaJson;
        IsActive = true;
    }

    public string Code { get; private set; }
    public string SubjectAr { get; private set; }
    public string SubjectEn { get; private set; }
    public string BodyAr { get; private set; }
    public string BodyEn { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string VariableSchemaJson { get; private set; }
    public bool IsActive { get; private set; }

    public static NotificationTemplate Define(string code,
        string subjectAr, string subjectEn, string bodyAr, string bodyEn,
        NotificationChannel channel, string variableSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern.IsMatch(code))
            throw new DomainException($"Code '{code}' must be UPPER_SNAKE_CASE.");
        if (string.IsNullOrWhiteSpace(subjectAr)) throw new DomainException("SubjectAr is required.");
        if (string.IsNullOrWhiteSpace(subjectEn)) throw new DomainException("SubjectEn is required.");
        if (string.IsNullOrWhiteSpace(bodyAr)) throw new DomainException("BodyAr is required.");
        if (string.IsNullOrWhiteSpace(bodyEn)) throw new DomainException("BodyEn is required.");
        if (string.IsNullOrWhiteSpace(variableSchemaJson))
            throw new DomainException("VariableSchemaJson is required (use '{}' for none).");
        return new NotificationTemplate(System.Guid.NewGuid(), code,
            subjectAr, subjectEn, bodyAr, bodyEn, channel, variableSchemaJson);
    }

    public void UpdateContent(string subjectAr, string subjectEn, string bodyAr, string bodyEn)
    {
        if (string.IsNullOrWhiteSpace(subjectAr)) throw new DomainException("SubjectAr is required.");
        if (string.IsNullOrWhiteSpace(subjectEn)) throw new DomainException("SubjectEn is required.");
        if (string.IsNullOrWhiteSpace(bodyAr)) throw new DomainException("BodyAr is required.");
        if (string.IsNullOrWhiteSpace(bodyEn)) throw new DomainException("BodyEn is required.");
        SubjectAr = subjectAr; SubjectEn = subjectEn;
        BodyAr = bodyAr; BodyEn = bodyEn;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
