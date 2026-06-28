using FluentValidation;

namespace CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandValidator
    : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.DeviceId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Platform).NotEmpty()
            .Must(p => p is "ios" or "android" or "web")
            .WithMessage("Platform must be 'ios', 'android', or 'web'.");
    }
}
