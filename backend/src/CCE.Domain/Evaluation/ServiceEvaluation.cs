using CCE.Domain.Common;

namespace CCE.Domain.Evaluation;

[Audited]
public sealed class ServiceEvaluation : AuditableEntity<System.Guid>
{
    private static readonly System.Guid AnonymousVisitorId = new("00000000-0000-0000-0000-000000000001");

    private ServiceEvaluation(
        System.Guid id,
        EvaluationRating overallSatisfaction,
        EvaluationRating easeOfUse,
        EvaluationRating contentSuitability,
        string feedback,
        System.Guid? userId) : base(id)
    {
        OverallSatisfaction = overallSatisfaction;
        EaseOfUse = easeOfUse;
        ContentSuitability = contentSuitability;
        Feedback = feedback;
        UserId = userId;
    }

    public EvaluationRating OverallSatisfaction { get; private set; }
    public EvaluationRating EaseOfUse { get; private set; }
    public EvaluationRating ContentSuitability { get; private set; }
    public string Feedback { get; private set; }
    public System.Guid? UserId { get; private set; }

    public static ServiceEvaluation Submit(
        EvaluationRating overallSatisfaction,
        EvaluationRating easeOfUse,
        EvaluationRating contentSuitability,
        string feedback,
        System.Guid? userId,
        ISystemClock clock)
    {
        if (overallSatisfaction == EvaluationRating.None)
            throw new DomainException("OverallSatisfaction is required.");
        if (easeOfUse == EvaluationRating.None)
            throw new DomainException("EaseOfUse is required.");
        if (contentSuitability == EvaluationRating.None)
            throw new DomainException("ContentSuitability is required.");
        if (string.IsNullOrWhiteSpace(feedback))
            throw new DomainException("Feedback is required.");

        var entity = new ServiceEvaluation(
            System.Guid.NewGuid(),
            overallSatisfaction,
            easeOfUse,
            contentSuitability,
            feedback.Trim(),
            userId);

        entity.CreatedOn = clock.UtcNow;
        entity.CreatedById = userId ?? AnonymousVisitorId;

        return entity;
    }
}
