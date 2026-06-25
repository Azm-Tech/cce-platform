using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Auth.AdLogin;

public sealed class AdLoginCommandValidator : AbstractValidator<AdLoginCommand>
{
    public AdLoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Password).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
    }
}
