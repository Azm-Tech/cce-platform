using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Commands.RequestPhoneChange;
using CCE.Application.Messages;
using CCE.Application.Verification;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.ConfirmPhoneChange;

internal sealed class ConfirmPhoneChangeCommandHandler
    : IRequestHandler<ConfirmPhoneChangeCommand, Response<VoidData>>
{
    private readonly IOtpVerificationRepository _otpRepo;
    private readonly IUserProfileRepository _userRepo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly IOtpCodeGenerator _codeGenerator;

    public ConfirmPhoneChangeCommandHandler(
        IOtpVerificationRepository otpRepo,
        IUserProfileRepository userRepo,
        ICceDbContext db,
        MessageFactory msg,
        IOtpCodeGenerator codeGenerator)
    {
        _otpRepo = otpRepo;
        _userRepo = userRepo;
        _db = db;
        _msg = msg;
        _codeGenerator = codeGenerator;
    }

    public async Task<Response<VoidData>> Handle(
        ConfirmPhoneChangeCommand request, CancellationToken ct)
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
            return _msg.Unauthorized<VoidData>("OTP_UNAUTHORIZED");

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

        // Read CountryCodeId stored at request-time — client does not need to re-send it
        System.Guid? countryCodeId = null;
        if (otp.ExtraData is not null)
        {
            var extra = System.Text.Json.JsonSerializer.Deserialize<PhoneChangeExtra>(otp.ExtraData);
            countryCodeId = extra?.CountryCodeId;
        }

        // domain methods
        otp.MarkVerified();
        otp.Invalidate();
        user.UpdatePhoneNumber(otp.Contact, countryCodeId);

        _otpRepo.Update(otp);
        _userRepo.Update(user);

        // ICceDbContext as unit of work
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return _msg.PhoneUpdated();
    }
}
