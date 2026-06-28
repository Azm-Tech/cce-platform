using CCE.Domain.Verification;
using FluentValidation;

namespace CCE.Application.Verification.Commands.RequestVerification;

public sealed class RequestVerificationCommandValidator : AbstractValidator<RequestVerificationCommand>
{
    public RequestVerificationCommandValidator()
    {
        RuleFor(x => x.Contact).NotEmpty();

        RuleFor(x => x.Contact)
            .EmailAddress().When(x => x.TypeId == OtpVerificationType.Email);

        RuleFor(x => x.Contact)
            .Matches(@"^\+?[0-9]{7,15}$").When(x => x.TypeId == OtpVerificationType.Sms);

        RuleFor(x => x.TypeId).IsInEnum();

        RuleFor(x => x.ProviderName)
            .NotEmpty().When(x => x.Token is not null)
            .WithMessage("ProviderName is required when Token is provided.");
    }
}
