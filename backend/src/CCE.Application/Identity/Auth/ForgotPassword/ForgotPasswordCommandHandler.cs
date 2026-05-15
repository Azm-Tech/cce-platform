using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using AppErrorCodes = CCE.Application.Errors.ApplicationErrors;

namespace CCE.Application.Identity.Auth.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, Result<AuthMessageDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IPasswordResetEmailSender _emailSender;

    public ForgotPasswordCommandHandler(UserManager<User> userManager, IPasswordResetEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    public async Task<Result<AuthMessageDto>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.EmailAddress).ConfigureAwait(false);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            await _emailSender.SendAsync(user, PasswordResetTokenCodec.Encode(token), ct).ConfigureAwait(false);
        }

        return new AuthMessageDto(AppErrorCodes.Identity.PASSWORD_RESET);
    }
}
