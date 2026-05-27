using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Messages;
using CCE.Application.Verification.Dtos;
using CCE.Domain.Identity;
using CCE.Domain.Notifications;
using CCE.Domain.Verification;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.RequestPhoneChange;

internal sealed class RequestPhoneChangeCommandHandler
    : IRequestHandler<RequestPhoneChangeCommand, Response<RequestVerificationResponseDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly ICceDbContext _db;
    private readonly ContactChangeOtpService _otpService;
    private readonly MessageFactory _msg;

    public RequestPhoneChangeCommandHandler(
        IUserRepository userRepo,
        ICceDbContext db,
        ContactChangeOtpService otpService,
        MessageFactory msg)
    {
        _userRepo = userRepo;
        _db = db;
        _otpService = otpService;
        _msg = msg;
    }

    public async Task<Response<RequestVerificationResponseDto>> Handle(
        RequestPhoneChangeCommand request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Normalize to digits-only for consistent storage and comparison
        var normalizedPhone = User.NormalizePhone(request.NewPhone);

        // fetch via repository — check new phone not already taken by another account
        var taken = await _userRepo
            .IsContactTakenAsync(normalizedPhone, OtpVerificationType.Sms, request.UserId, ct)
            .ConfigureAwait(false);

        if (taken)
            return _msg.ContactAlreadyTaken<RequestVerificationResponseDto>();

        // Serialize CountryCodeId into ExtraData so it survives to confirm-time without client round-trip
        var extraData = request.CountryCodeId.HasValue
            ? System.Text.Json.JsonSerializer.Serialize(new PhoneChangeExtra(request.CountryCodeId.Value))
            : null;

        var (entity, fail) = await _otpService.PrepareAsync(
            normalizedPhone,
            OtpVerificationType.Sms,
            templateCode: "PHONE_CHANGE_OTP",
            channel: NotificationChannel.Sms,
            recipientUserId: request.UserId,
            extraData,
            now, ct).ConfigureAwait(false);

        if (fail is not null) return fail;

        // ICceDbContext as unit of work
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return _msg.Ok(new RequestVerificationResponseDto(entity!.Id, entity.ExpiresAt), "OTP_SENT");
    }
}

internal sealed record PhoneChangeExtra(System.Guid CountryCodeId);
