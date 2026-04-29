using FluentValidation;

namespace CCE.Application.Country.Commands.UpdateCountry;

public sealed class UpdateCountryCommandValidator : AbstractValidator<UpdateCountryCommand>
{
    public UpdateCountryCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
        RuleFor(x => x.RegionAr).NotEmpty();
        RuleFor(x => x.RegionEn).NotEmpty();
    }
}
