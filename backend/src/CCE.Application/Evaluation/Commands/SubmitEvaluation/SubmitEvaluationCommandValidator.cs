using CCE.Domain.Evaluation;
using FluentValidation;

namespace CCE.Application.Evaluation.Commands.SubmitEvaluation;

public sealed class SubmitEvaluationCommandValidator : AbstractValidator<SubmitEvaluationCommand>
{
    public SubmitEvaluationCommandValidator()
    {
        RuleFor(x => x.OverallSatisfaction).IsInEnum().NotEqual(EvaluationRating.None);
        RuleFor(x => x.EaseOfUse).IsInEnum().NotEqual(EvaluationRating.None);
        RuleFor(x => x.ContentSuitability).IsInEnum().NotEqual(EvaluationRating.None);
        RuleFor(x => x.Feedback).NotEmpty().MaximumLength(500);
    }
}
