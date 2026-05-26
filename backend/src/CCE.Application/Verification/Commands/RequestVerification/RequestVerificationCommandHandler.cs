using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.Notifications;
using CCE.Application.Verification.Dtos;
using CCE.Domain.Notifications;
using CCE.Domain.Verification;
using MediatR;

namespace CCE.Application.Verification.Commands.RequestVerification;

internal sealed class RequestVerificationCommandHandler
    : IRequestHandler<RequestVerificationCommand, Response<RequestVerificationResponseDto>>
{
    private readonly IOtpVerificationRepository _otpRepo;
    private readonly ICceDbContext _db;
    private readonly INotificationGateway _gateway;
    private readonly MessageFactory _msg;
    private readonly IOtpCodeGenerator _codeGenerator;

    public RequestVerificationCommandHandler(
        IOtpVerificationRepository otpRepo,
        ICceDbContext db,
        INotificationGateway gateway,
        MessageFactory msg,
        IOtpCodeGenerator codeGenerator)
    {
        _otpRepo = otpRepo;
        _db = db;
        _gateway = gateway;
        _msg = msg;
        _codeGenerator = codeGenerator;
    }

    public async Task<Response<RequestVerificationResponseDto>> Handle(
        RequestVerificationCommand request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var existing = await _otpRepo.FindActiveAsync(request.Contact, request.TypeId, now, ct)
            .ConfigureAwait(false);

        if (existing is not null && !existing.CanResend(now))
            return _msg.OtpCooldownActive<RequestVerificationResponseDto>();

        var (plainCode, codeHash) = _codeGenerator.Generate();

        OtpVerification entity;
        if (existing is not null)
        {
            existing.Refresh(codeHash, now);
            _otpRepo.Update(existing);
            entity = existing;
        }
        else
        {
            entity = OtpVerification.Create(request.Contact, request.TypeId, codeHash, now, userId: null);
            await _otpRepo.AddAsync(entity, ct).ConfigureAwait(false);
        }

        var channel = request.TypeId == OtpVerificationType.Sms
            ? NotificationChannel.Sms
            : NotificationChannel.Email;

        await _gateway.SendAsync(new NotificationDispatchRequest(
            TemplateCode: "OTP_VERIFICATION",
            RecipientUserId: null,
            Channels: [channel],
            Variables: new Dictionary<string, string> { ["Code"] = plainCode },
            PhoneNumber: request.TypeId == OtpVerificationType.Sms ? request.Contact : null,
            Email: request.TypeId == OtpVerificationType.Email ? request.Contact : null,
            BypassSettings: true), ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return _msg.Ok(
            new RequestVerificationResponseDto(entity.Id, entity.ExpiresAt),
            "OTP_SENT");
    }
}
