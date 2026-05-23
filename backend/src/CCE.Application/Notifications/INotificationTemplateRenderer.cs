using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface INotificationTemplateRenderer
{
    /// <summary>
    /// Renders subject and body by replacing {{Variable}} placeholders with values from <paramref name="variables"/>.
    /// </summary>
    /// <param name="template">The template to render.</param>
    /// <param name="variables">Variable values keyed by name.</param>
    /// <param name="locale">"ar" or "en".</param>
    /// <returns>A tuple of (subjectAr, subjectEn, body).</returns>
    (string SubjectAr, string SubjectEn, string Body) Render(
        NotificationTemplate template,
        IReadOnlyDictionary<string, string> variables,
        string locale);
}
