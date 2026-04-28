using FluentValidation;

namespace CCE.Application.Identity.Commands.RejectExpertRequest;

public sealed class RejectExpertRequestCommandValidator : AbstractValidator<RejectExpertRequestCommand>
{
    public RejectExpertRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RejectionReasonAr).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RejectionReasonEn).NotEmpty().MaximumLength(2000);
    }
}
