using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Messages;
using CCE.Application.Verification.Dtos;
using CCE.Domain.Identity;
using CCE.Domain.Verification;
using MediatR;

namespace CCE.Application.Verification.Commands.VerifyOtp;

internal sealed class VerifyOtpCommandHandler
    : IRequestHandler<VerifyOtpCommand, Response<VerifyOtpResponseDto>>
{
    private readonly IOtpVerificationRepository _otpRepo;
    private readonly IUserVerificationRepository _verificationRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly IOtpCodeGenerator _codeGenerator;

    public VerifyOtpCommandHandler(
        IOtpVerificationRepository otpRepo,
        IUserVerificationRepository verificationRepo,
        IUserRepository userRepo,
        ICceDbContext db,
        MessageFactory msg,
        IOtpCodeGenerator codeGenerator)
    {
        _otpRepo = otpRepo;
        _verificationRepo = verificationRepo;
        _userRepo = userRepo;
        _db = db;
        _msg = msg;
        _codeGenerator = codeGenerator;
    }

    public async Task<Response<VerifyOtpResponseDto>> Handle(
        VerifyOtpCommand request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var entity = await _otpRepo
            .GetByIdAsync(request.VerificationId, ct)
            .ConfigureAwait(false);

        if (entity is null)
            return _msg.OtpNotFound<VerifyOtpResponseDto>();

        if (entity.IsExpired(now))
            return _msg.OtpExpired<VerifyOtpResponseDto>();

        if (entity.IsInvalidated)
            return _msg.OtpInvalidated<VerifyOtpResponseDto>();

        if (entity.HasExceededMaxAttempts())
            return _msg.OtpMaxAttempts<VerifyOtpResponseDto>();

        entity.IncrementAttempt();

        if (!_codeGenerator.Verify(request.Code, entity.CodeHash))
        {
            _otpRepo.Update(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return _msg.OtpInvalidCode<VerifyOtpResponseDto>();
        }

        entity.MarkVerified();
        _otpRepo.Update(entity);

        var userVerification = await _verificationRepo
            .FindAsync(entity.Contact, entity.TypeId, ct)
            .ConfigureAwait(false);

        if (userVerification is null)
        {
            userVerification = UserVerification.Create(null, entity.Contact, entity.TypeId);
            await _verificationRepo.AddAsync(userVerification, ct).ConfigureAwait(false);
        }
        userVerification.MarkVerified(now);
        _verificationRepo.Update(userVerification);

        Guid? resolvedUserId = await StampUserConfirmedAsync(entity, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return _msg.Ok(new VerifyOtpResponseDto(true, resolvedUserId), "OTP_VERIFIED");
    }

    private async Task<Guid?> StampUserConfirmedAsync(OtpVerification entity, CancellationToken ct)
    {
        var userId = await _userRepo
            .FindUserIdByContactAsync(entity.Contact, entity.TypeId, ct)
            .ConfigureAwait(false);

        if (userId is null) return null;

        await _userRepo.StampConfirmedAsync(userId.Value, entity.TypeId, ct).ConfigureAwait(false);
        return userId;
    }
}
