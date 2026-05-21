using FluentValidation;

namespace CCE.Application.Media.Commands.UpdateMediaMetadata;

public sealed class UpdateMediaMetadataCommandValidator
    : AbstractValidator<UpdateMediaMetadataCommand>
{
    public UpdateMediaMetadataCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TitleAr).MaximumLength(200);
        RuleFor(x => x.TitleEn).MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);
        RuleFor(x => x.AltTextAr).MaximumLength(500);
        RuleFor(x => x.AltTextEn).MaximumLength(500);
    }
}
