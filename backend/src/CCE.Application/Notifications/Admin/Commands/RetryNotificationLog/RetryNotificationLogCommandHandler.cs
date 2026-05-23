using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Admin.Commands.RetryNotificationLog;

public sealed class RetryNotificationLogCommandHandler
    : IRequestHandler<RetryNotificationLogCommand, Response<System.Guid>>
{
    private readonly INotificationLogRepository _logRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly IEnumerable<INotificationChannelHandler> _handlers;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public RetryNotificationLogCommandHandler(
        INotificationLogRepository logRepository,
        INotificationTemplateRepository templateRepository,
        IEnumerable<INotificationChannelHandler> handlers,
        INotificationTemplateRenderer renderer,
        ICceDbContext db,
        MessageFactory msg)
    {
        _logRepository = logRepository;
        _templateRepository = templateRepository;
        _handlers = handlers;
        _renderer = renderer;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Guid>> Handle(
        RetryNotificationLogCommand request,
        CancellationToken cancellationToken)
    {
        var log = await _logRepository.GetAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (log is null)
            return _msg.NotificationLogNotFound<System.Guid>();

        if (log.Status != NotificationDeliveryStatus.Failed && log.Status != NotificationDeliveryStatus.Skipped)
            throw new DomainException($"Cannot retry a log with status {log.Status}.");

        log.IncrementAttempt();

        // Resolve template
        var template = await _templateRepository.GetActiveByCodeAndChannelAsync(
            log.TemplateCode,
            log.Channel,
            cancellationToken)
            .ConfigureAwait(false);

        if (template is null)
        {
            log.MarkSkipped("Template no longer available.");
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return _msg.NotificationRetried(log.Id);
        }

        // Resolve recipient data
        string? email = null;
        string? phone = null;
        string locale = "en";

        if (log.RecipientUserId is { } userId)
        {
            var user = (await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.PhoneNumber })
                .ToListAsyncEither(cancellationToken)
                .ConfigureAwait(false))
                .FirstOrDefault();

            if (user is not null)
            {
                email = user.Email;
                phone = user.PhoneNumber;
            }
        }

        // Render
        var variables = log.PayloadJson is not null
            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(log.PayloadJson) ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();

        var (subjectAr, subjectEn, body) = _renderer.Render(template, variables, locale);
        var subject = subjectEn;

        var rendered = new RenderedNotification(
            log.TemplateCode,
            log.RecipientUserId,
            template.Id,
            subject,
            subjectAr,
            subjectEn,
            body,
            log.Channel,
            locale,
            email,
            phone);

        // Dispatch
        var sender = _handlers.FirstOrDefault(s => s.Channel == log.Channel);
        if (sender is null)
        {
            log.MarkSkipped($"No sender registered for channel {log.Channel}.");
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return _msg.NotificationRetried(log.Id);
        }

        var sendResult = await sender.SendAsync(rendered, cancellationToken).ConfigureAwait(false);

        if (sendResult.Success)
        {
            log.MarkSent(sendResult.ProviderMessageId);
        }
        else
        {
            log.MarkFailed(sendResult.Error ?? "Unknown error");
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.NotificationRetried(log.Id);
    }
}
