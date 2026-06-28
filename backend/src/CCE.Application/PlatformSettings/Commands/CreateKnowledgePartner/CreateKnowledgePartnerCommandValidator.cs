using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.CreateKnowledgePartner;

public sealed class CreateKnowledgePartnerCommandValidator
    : AbstractValidator<CreateKnowledgePartnerCommand>
{
    public CreateKnowledgePartnerCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);
    }
}
