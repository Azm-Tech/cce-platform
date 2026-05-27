using FluentValidation;

namespace CCE.Application.Lookups.Commands.UpsertCountryCode;

public sealed class UpsertCountryCodeCommandValidator : AbstractValidator<UpsertCountryCodeCommand>
{
    public UpsertCountryCodeCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(256);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DialCode).NotEmpty().MaximumLength(16);
    }
}
