using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Messages;
using CCE.Application.Verification.Dtos;
using CCE.Domain.Notifications;
using CCE.Domain.Verification;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.RequestEmailChange;

internal sealed class RequestEmailChangeCommandHandler
    : IRequestHandler<RequestEmailChangeCommand, Response<RequestVerificationResponseDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly ICceDbContext _db;
    private readonly ContactChangeOtpService _otpService;
    private readonly MessageFactory _msg;

    public RequestEmailChangeCommandHandler(
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
        RequestEmailChangeCommand request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // fetch via repository — check new email not already taken by another account
        var taken = await _userRepo
            .IsContactTakenAsync(request.NewEmail, OtpVerificationType.Email, request.UserId, ct)
            .ConfigureAwait(false);

        if (taken)
            return _msg.Conflict<RequestVerificationResponseDto>(MessageKeys.Verification.CONTACT_ALREADY_TAKEN);

        var (entity, fail) = await _otpService.PrepareAsync(
            request.NewEmail,
            OtpVerificationType.Email,
            templateCode: "EMAIL_CHANGE_OTP",
            channel: NotificationChannel.Email,
            recipientUserId: request.UserId,
            extraData: null,
            now, ct).ConfigureAwait(false);

        if (fail is not null) return fail;

        // ICceDbContext as unit of work
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return _msg.Ok(new RequestVerificationResponseDto(entity!.Id, entity.ExpiresAt), MessageKeys.Verification.OTP_SENT);
    }
}
