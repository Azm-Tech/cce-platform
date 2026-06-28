using CCE.Application.Common;
using CCE.Application.Messages;
using CCE.Application.Notifications;
using CCE.Application.Verification;
using CCE.Application.Verification.Dtos;
using CCE.Domain.Notifications;
using CCE.Domain.Verification;

namespace CCE.Application.Identity.Public.Commands;

/// <summary>
/// Shared OTP preparation logic for email and phone contact-change flows.
/// Handles cooldown check, code generation, OTP create-or-refresh, and notification dispatch.
/// Each handler only needs to perform its contact-specific uniqueness check then delegate here.
/// </summary>
internal sealed class ContactChangeOtpService
{
    private readonly IOtpVerificationRepository _otpRepo;
    private readonly INotificationGateway _gateway;
    private readonly MessageFactory _msg;
    private readonly IOtpCodeGenerator _codeGenerator;

    public ContactChangeOtpService(
        IOtpVerificationRepository otpRepo,
        INotificationGateway gateway,
        MessageFactory msg,
        IOtpCodeGenerator codeGenerator)
    {
        _otpRepo = otpRepo;
        _gateway = gateway;
        _msg = msg;
        _codeGenerator = codeGenerator;
    }

    /// <summary>
    /// Prepares an OTP for a contact-change request.
    /// Returns <c>(entity, null)</c> on success; <c>(null, failResponse)</c> on cooldown.
    /// Caller must call <c>ICceDbContext.SaveChangesAsync</c> after this returns successfully.
    /// </summary>
    public async Task<(OtpVerification? Entity, Response<RequestVerificationResponseDto>? Fail)> PrepareAsync(
        string contact,
        OtpVerificationType type,
        string templateCode,
        NotificationChannel channel,
        System.Guid recipientUserId,
        string? extraData,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var existing = await _otpRepo
            .FindActiveAsync(contact, type, now, recipientUserId, ct)
            .ConfigureAwait(false);

        if (existing is not null && !existing.CanResend(now))
            return (null, _msg.BusinessRule<RequestVerificationResponseDto>(MessageKeys.Verification.OTP_COOLDOWN_ACTIVE));

        var (plainCode, codeHash) = _codeGenerator.Generate();

        OtpVerification entity;
        if (existing is not null)
        {
            existing.Refresh(codeHash, now, extraData, recipientUserId);
            _otpRepo.Update(existing);
            entity = existing;
        }
        else
        {
            entity = OtpVerification.Create(contact, type, codeHash, now, extraData, recipientUserId);
            await _otpRepo.AddAsync(entity, ct).ConfigureAwait(false);
        }

        await _gateway.SendAsync(new NotificationDispatchRequest(
            TemplateCode: templateCode,
            RecipientUserId: recipientUserId,
            Channels: [channel],
            Variables: new Dictionary<string, string> { ["Code"] = plainCode },
            PhoneNumber: type == OtpVerificationType.Sms ? contact : null,
            Email: type == OtpVerificationType.Email ? contact : null,
            BypassSettings: true), ct).ConfigureAwait(false);

        return (entity, null);
    }
}
