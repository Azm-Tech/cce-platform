using CCE.Domain.Evaluation;
using FluentValidation;

namespace CCE.Application.Evaluation.Commands.SubmitEvaluation;

public sealed class SubmitEvaluationCommandValidator : AbstractValidator<SubmitEvaluationCommand>
{
    public SubmitEvaluationCommandValidator()
    {
        RuleFor(x => x.OverallSatisfaction)
            .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");
        RuleFor(x => x.EaseOfUse)
            .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");
        RuleFor(x => x.ContentSuitability)
            .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");
        RuleFor(x => x.Feedback)
            .MaximumLength(500).WithErrorCode("MAX_LENGTH");
    }
}
