using FluentValidation;

namespace CCE.Application.Media.Commands.UploadMedia;

public sealed class UploadMediaCommandValidator
    : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.FileStream).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TitleAr).MaximumLength(200);
        RuleFor(x => x.TitleEn).MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);
        RuleFor(x => x.AltTextAr).MaximumLength(500);
        RuleFor(x => x.AltTextEn).MaximumLength(500);
    }
}
