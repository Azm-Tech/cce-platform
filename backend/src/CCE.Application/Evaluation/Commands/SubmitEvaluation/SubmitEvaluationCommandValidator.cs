using CCE.Domain.Evaluation;
using FluentValidation;

namespace CCE.Application.Evaluation.Commands.SubmitEvaluation;

public sealed class SubmitEvaluationCommandValidator : AbstractValidator<SubmitEvaluationCommand>
{
    public SubmitEvaluationCommandValidator()
    {
        RuleFor(x => x.OverallSatisfaction)
            .IsInEnum().WithErrorCode("INVALID_ENUM")
            .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");
        RuleFor(x => x.EaseOfUse)
            .IsInEnum().WithErrorCode("INVALID_ENUM")
            .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");
        RuleFor(x => x.ContentSuitability)
            .IsInEnum().WithErrorCode("INVALID_ENUM")
            .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");
        RuleFor(x => x.Feedback)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(500).WithErrorCode("MAX_LENGTH");
    }
}
