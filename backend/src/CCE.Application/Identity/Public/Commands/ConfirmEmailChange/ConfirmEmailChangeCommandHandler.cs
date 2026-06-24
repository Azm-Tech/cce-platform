using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.Verification;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Identity.Public.Commands.ConfirmEmailChange;

internal sealed class ConfirmEmailChangeCommandHandler
    : IRequestHandler<ConfirmEmailChangeCommand, Response<VoidData>>
{
    private readonly IOtpVerificationRepository _otpRepo;
    private readonly IUserProfileRepository _userRepo;
    private readonly UserManager<User> _userManager;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly IOtpCodeGenerator _codeGenerator;

    public ConfirmEmailChangeCommandHandler(
        IOtpVerificationRepository otpRepo,
        IUserProfileRepository userRepo,
        UserManager<User> userManager,
        ICceDbContext db,
        MessageFactory msg,
        IOtpCodeGenerator codeGenerator)
    {
        _otpRepo = otpRepo;
        _userRepo = userRepo;
        _userManager = userManager;
        _db = db;
        _msg = msg;
        _codeGenerator = codeGenerator;
    }

    public async Task<Response<VoidData>> Handle(
        ConfirmEmailChangeCommand request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // WRITE — fetch OTP via repository
        var otp = await _otpRepo
            .GetByIdAsync(request.VerificationId, ct)
            .ConfigureAwait(false);

        if (otp is null)
            return _msg.OtpNotFound<VoidData>();

        if (otp.IsInvalidated)
            return _msg.OtpInvalidated<VoidData>();

        if (otp.IsExpired(now))
            return _msg.OtpExpired<VoidData>();

        if (otp.HasExceededMaxAttempts())
            return _msg.OtpMaxAttempts<VoidData>();

        // Ownership validation — OTP must belong to the authenticated user
        if (otp.UserId.HasValue && otp.UserId.Value != request.UserId)
            return _msg.Unauthorized<VoidData>(MessageKeys.Verification.OTP_UNAUTHORIZED);

        otp.IncrementAttempt();

        if (!_codeGenerator.Verify(request.Code, otp.CodeHash))
        {
            _otpRepo.Update(otp);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return _msg.OtpInvalidCode<VoidData>();
        }

        // WRITE — fetch user via repository
        var user = await _userRepo
            .FindAsync(request.UserId, ct)
            .ConfigureAwait(false);

        if (user is null)
            return _msg.UserNotFound<VoidData>();

        // Use UserManager to ensure NormalizedEmail and SecurityStamp are properly updated
        var setEmailResult = await _userManager.SetEmailAsync(user, otp.Contact).ConfigureAwait(false);
        if (!setEmailResult.Succeeded)
            return _msg.BusinessRule<VoidData>(MessageKeys.Identity.EMAIL_CHANGE_FAILED);

        // Update UserName to match the new email
        var setUserNameResult = await _userManager.SetUserNameAsync(user, otp.Contact).ConfigureAwait(false);
        if (!setUserNameResult.Succeeded)
            return _msg.BusinessRule<VoidData>(MessageKeys.Identity.EMAIL_CHANGE_FAILED);

        // domain methods
        otp.MarkVerified();
        otp.Invalidate();

        _otpRepo.Update(otp);

        // ICceDbContext as unit of work
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return _msg.EmailUpdated();
    }
}
