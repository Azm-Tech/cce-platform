using FluentValidation;

namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.LocalePreference)
            .NotEmpty()
            .Must(l => l == "ar" || l == "en")
            .WithMessage("LocalePreference must be 'ar' or 'en'.");

        RuleFor(x => x.Interests).NotNull();

        RuleFor(x => x.AvatarUrl)
            .Must(url => url is null || url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            .WithMessage("AvatarUrl must be null or start with 'https://'.");
    }
}
