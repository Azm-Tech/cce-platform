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
        RuleFor(x => x.Population)
            .GreaterThan(0).When(x => x.Population.HasValue)
            .WithMessage("Population must be greater than 0.");
        RuleFor(x => x.AreaSqKm)
            .GreaterThan(0).When(x => x.AreaSqKm.HasValue)
            .WithMessage("AreaSqKm must be greater than 0.");
        RuleFor(x => x.GdpPerCapita)
            .GreaterThan(0).When(x => x.GdpPerCapita.HasValue)
            .WithMessage("GdpPerCapita must be greater than 0.");
    }
}
