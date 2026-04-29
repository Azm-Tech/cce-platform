using FluentValidation;

namespace CCE.Application.Country.Commands.UpsertCountryProfile;

public sealed class UpsertCountryProfileCommandValidator : AbstractValidator<UpsertCountryProfileCommand>
{
    public UpsertCountryProfileCommandValidator()
    {
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x.DescriptionAr).NotEmpty();
        RuleFor(x => x.DescriptionEn).NotEmpty();
        RuleFor(x => x.KeyInitiativesAr).NotEmpty();
        RuleFor(x => x.KeyInitiativesEn).NotEmpty();
    }
}
