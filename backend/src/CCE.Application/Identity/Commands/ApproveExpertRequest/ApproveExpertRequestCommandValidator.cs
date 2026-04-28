using FluentValidation;

namespace CCE.Application.Identity.Commands.ApproveExpertRequest;

public sealed class ApproveExpertRequestCommandValidator : AbstractValidator<ApproveExpertRequestCommand>
{
    public ApproveExpertRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AcademicTitleAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AcademicTitleEn).NotEmpty().MaximumLength(200);
    }
}
