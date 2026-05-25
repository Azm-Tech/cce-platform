using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.UpdateKnowledgePartner;

public sealed class UpdateKnowledgePartnerCommandValidator
    : AbstractValidator<UpdateKnowledgePartnerCommand>
{
    public UpdateKnowledgePartnerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);
    }
}
