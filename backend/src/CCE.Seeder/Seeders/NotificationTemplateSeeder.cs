using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Idempotent seeder for the <see cref="NotificationTemplate"/> rows that the notification
/// gateway resolves at dispatch time. Every (TemplateCode × Channel) pair that any handler,
/// consumer, or service actually dispatches must have a matching active template here —
/// otherwise the gateway logs "No active template found for channel X" and silently skips it.
///
/// <para>
/// Content is bilingual (ar/en) and uses <c>{{Variable}}</c> placeholders that match the
/// variables the dispatcher supplies. <c>VariableSchemaJson</c> is left as <c>"{}"</c> (no
/// required variables) so a missing variable degrades to the literal placeholder rather than
/// throwing — tighten per-template later if strict validation is wanted. Copy is intentionally
/// plain; edit freely.
/// </para>
/// </summary>
public sealed class NotificationTemplateSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ILogger<NotificationTemplateSeeder> _logger;

    public NotificationTemplateSeeder(CceDbContext ctx, ILogger<NotificationTemplateSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    // After PlatformSettings (40), before DemoData (100). Reference data — runs in every environment.
    public int Order => 45;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var changed = 0;
        foreach (var t in Templates)
        {
            var id = DeterministicGuid.From($"notif_template:{t.Code}:{t.Channel}");

            var existing = await _ctx.NotificationTemplates.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);

            // Fallback: previous versions may have used a different deterministic UUID
            if (existing is null)
            {
                existing = await _ctx.NotificationTemplates.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Code == t.Code && x.Channel == t.Channel, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (existing is null)
            {
                var template = NotificationTemplate.Define(
                    t.Code, t.SubjectAr, t.SubjectEn, t.BodyAr, t.BodyEn, t.Channel, "{}");
                typeof(NotificationTemplate).GetProperty(nameof(template.Id))!.SetValue(template, id);

                _ctx.NotificationTemplates.Add(template);
                _logger.LogInformation("Seeded notification template {Code}/{Channel}.", t.Code, t.Channel);
                changed++;
            }
            else if (existing.BodyAr != t.BodyAr || existing.BodyEn != t.BodyEn ||
                     existing.SubjectAr != t.SubjectAr || existing.SubjectEn != t.SubjectEn)
            {
                typeof(NotificationTemplate).GetProperty(nameof(existing.BodyAr))!.SetValue(existing, t.BodyAr);
                typeof(NotificationTemplate).GetProperty(nameof(existing.BodyEn))!.SetValue(existing, t.BodyEn);
                typeof(NotificationTemplate).GetProperty(nameof(existing.SubjectAr))!.SetValue(existing, t.SubjectAr);
                typeof(NotificationTemplate).GetProperty(nameof(existing.SubjectEn))!.SetValue(existing, t.SubjectEn);
                _logger.LogInformation("Updated notification template {Code}/{Channel}.", t.Code, t.Channel);
                changed++;
            }
        }

        if (changed > 0)
        {
            await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("NotificationTemplateSeeder complete ({Changed} changed/added).", changed);
    }

    private sealed record TemplateSeed(
        string Code,
        NotificationChannel Channel,
        string SubjectAr,
        string SubjectEn,
        string BodyAr,
        string BodyEn);

    // ─── Template definitions (one entry per code × channel that is actually dispatched) ───
    private static readonly TemplateSeed[] Templates =
    {
        // Expert registration — approved (InApp + Email)
        new("EXPERT_REQUEST_APPROVED", NotificationChannel.InApp,
            "تمت الموافقة على طلب الخبير",
            "Expert request approved",
            "تهانينا! تمت الموافقة على طلب تسجيلك كخبير.",
            "Congratulations! Your expert registration request has been approved."),
        new("EXPERT_REQUEST_APPROVED", NotificationChannel.Email,
            "تمت الموافقة على طلب الخبير",
            "Expert request approved",
            "<p>تهانينا! تمت الموافقة على طلب تسجيلك كخبير. يمكنك الآن الوصول إلى ميزات الخبراء على المنصة.</p>",
            "<p>Congratulations! Your expert registration request has been approved. You now have access to expert features on the platform.</p>"),

        // Expert registration — rejected (InApp + Email)
        new("EXPERT_REQUEST_REJECTED", NotificationChannel.InApp,
            "تم رفض طلب الخبير",
            "Expert request rejected",
            "نأسف لإبلاغك بأنه تم رفض طلب تسجيلك كخبير. السبب: {{Reason}}",
            "We're sorry to inform you that your expert registration request was rejected. Reason: {{Reason}}"),
        new("EXPERT_REQUEST_REJECTED", NotificationChannel.Email,
            "تم رفض طلب الخبير",
            "Expert request rejected",
            "<p>نأسف لإبلاغك بأنه تم رفض طلب تسجيلك كخبير.</p><p>السبب: {{Reason}}</p>",
            "<p>We're sorry to inform you that your expert registration request was rejected.</p><p>Reason: {{Reason}}</p>"),

        // Country content request — approved (InApp + Email)
        new("COUNTRY_CONTENT_REQUEST_APPROVED", NotificationChannel.InApp,
            "تمت الموافقة على طلب المحتوى",
            "Content request approved",
            "تمت الموافقة على طلب المحتوى الخاص بك ({{Type}}).",
            "Your content request ({{Type}}) has been approved."),
        new("COUNTRY_CONTENT_REQUEST_APPROVED", NotificationChannel.Email,
            "تمت الموافقة على طلب المحتوى",
            "Content request approved",
            "<p>تمت الموافقة على طلب المحتوى الخاص بك ({{Type}}) وأصبح الآن منشوراً.</p>",
            "<p>Your content request ({{Type}}) has been approved and is now published.</p>"),

        // Country content request — rejected (InApp + Email)
        new("COUNTRY_CONTENT_REQUEST_REJECTED", NotificationChannel.InApp,
            "تم رفض طلب المحتوى",
            "Content request rejected",
            "تم رفض طلب المحتوى الخاص بك ({{Type}}). ملاحظات: {{AdminNotesAr}}",
            "Your content request ({{Type}}) was rejected. Notes: {{AdminNotesEn}}"),
        new("COUNTRY_CONTENT_REQUEST_REJECTED", NotificationChannel.Email,
            "تم رفض طلب المحتوى",
            "Content request rejected",
            "<p>تم رفض طلب المحتوى الخاص بك ({{Type}}).</p><p>ملاحظات المراجع: {{AdminNotesAr}}</p>",
            "<p>Your content request ({{Type}}) was rejected.</p><p>Reviewer notes: {{AdminNotesEn}}</p>"),

        // Country content submitted — admin/reviewer notice (InApp + Email)
        new("COUNTRY_CONTENT_SUBMITTED", NotificationChannel.InApp,
            "طلب محتوى جديد بانتظار المراجعة",
            "New content request awaiting review",
            "تم تقديم طلب محتوى جديد ({{Type}}) وبانتظار المراجعة.",
            "A new content request ({{Type}}) has been submitted and is awaiting review."),
        new("COUNTRY_CONTENT_SUBMITTED", NotificationChannel.Email,
            "طلب محتوى جديد بانتظار المراجعة",
            "New content request awaiting review",
            "<p>تم تقديم طلب محتوى جديد ({{Type}}) وبانتظار المراجعة.</p>",
            "<p>A new content request ({{Type}}) has been submitted and is awaiting review.</p>"),

        // News published — InApp (author + subscribers) + Email (subscribers)
        new("NEWS_PUBLISHED", NotificationChannel.InApp,
            "خبر جديد منشور",
            "News published",
            "تم نشر خبر جديد: {{TitleAr}}",
            "New article published: {{TitleEn}}"),
        new("NEWS_PUBLISHED", NotificationChannel.Email,
            "خبر جديد على المنصة",
            "New article on the platform",
            "<p>مرحباً {{RecipientName}}،</p><p>تم نشر خبر جديد على منصة CCE Knowledge Center:</p><h2>{{TitleAr}}</h2><p>{{ContentBodyAr}}</p><p style='text-align:center;'><a href='{{ArticleUrl}}' class='btn' style='display:inline-block;padding:12px 28px;background-color:#1a73e8;color:#fff;text-decoration:none;border-radius:6px;font-size:15px;font-weight:500;margin:16px 0;'>قراءة الخبر كاملاً</a></p><p>مع تحيات،<br/>فريق CCE Knowledge Center</p>",
            "<p>Dear {{RecipientName}},</p><p>A new article has been published on CCE Knowledge Center:</p><h2>{{TitleEn}}</h2><p>{{ContentBodyEn}}</p><p style='text-align:center;'><a href='{{ArticleUrl}}' class='btn' style='display:inline-block;padding:12px 28px;background-color:#1a73e8;color:#fff;text-decoration:none;border-radius:6px;font-size:15px;font-weight:500;margin:16px 0;'>Read full article</a></p><p>Best regards,<br/>CCE Knowledge Center Team</p>"),

        // Resource published — InApp (uploader + subscribers) + Email (subscribers)
        new("RESOURCE_PUBLISHED", NotificationChannel.InApp,
            "مورد جديد منشور",
            "Resource published",
            "تم نشر مورد جديد: {{TitleAr}}",
            "New resource published: {{TitleEn}}"),
        new("RESOURCE_PUBLISHED", NotificationChannel.Email,
            "مورد جديد على المنصة",
            "New resource on the platform",
            "<p>تم نشر مورد جديد على المنصة: <strong>{{TitleAr}}</strong></p>",
            "<p>A new resource has been published on the platform: <strong>{{TitleEn}}</strong></p>"),

        // Event scheduled — InApp + Email (subscribers)
        new("EVENT_SCHEDULED", NotificationChannel.InApp,
            "فعالية جديدة مجدولة",
            "New event scheduled",
            "تم تحديد موعد فعالية جديدة: {{TitleAr}} في {{StartsOn}}",
            "A new event has been scheduled: {{TitleEn}} on {{StartsOn}}"),
        new("EVENT_SCHEDULED", NotificationChannel.Email,
            "فعالية جديدة على المنصة",
            "New event on the platform",
            "<p>تم تحديد موعد فعالية جديدة: <strong>{{TitleAr}}</strong></p><p>الموعد: {{StartsOn}}</p>",
            "<p>A new event has been scheduled: <strong>{{TitleEn}}</strong></p><p>Date: {{StartsOn}}</p>"),

        // Community — new post for topic/community followers (InApp)
        new("COMMUNITY_POST_CREATED", NotificationChannel.InApp,
            "منشور جديد",
            "New post",
            "تم نشر منشور جديد في مجتمع تتابعه.",
            "A new post was published in a community you follow."),

        // Community — reply on a followed/authored post (InApp)
        new("POST_REPLIED", NotificationChannel.InApp,
            "رد جديد على منشور",
            "New reply on a post",
            "هناك رد جديد على منشور تتابعه.",
            "There's a new reply on a post you follow."),

        // Community — join request for moderators (InApp)
        new("COMMUNITY_JOIN_REQUESTED", NotificationChannel.InApp,
            "طلب انضمام جديد",
            "New join request",
            "هناك طلب انضمام جديد إلى مجتمع تشرف عليه.",
            "There's a new request to join a community you moderate."),

        // Community — user mentioned in a reply (InApp)
        new("COMMUNITY_MENTION", NotificationChannel.InApp,
            "تمت الإشارة إليك",
            "You were mentioned",
            "أشار إليك أحد المستخدمين في رد.",
            "A user mentioned you in a reply."),

        // OTP verification — Email + SMS (channel chosen at runtime)
        new("OTP_VERIFICATION", NotificationChannel.Email,
            "رمز التحقق",
            "Verification code",
            "<p>رمز التحقق الخاص بك هو: <strong>{{Code}}</strong></p>",
            "<p>Your verification code is: <strong>{{Code}}</strong></p>"),
        new("OTP_VERIFICATION", NotificationChannel.Sms,
            "رمز التحقق",
            "Verification code",
            "رمز التحقق الخاص بك هو {{Code}}",
            "Your verification code is {{Code}}"),

        // Password reset — Email
        new("PASSWORD_RESET", NotificationChannel.Email,
            "إعادة تعيين كلمة المرور",
            "Reset your password",
            "<p>مرحباً {{Name}}،</p><p>لإعادة تعيين كلمة المرور الخاصة بك، يرجى الضغط على الرابط التالي:</p><p><a href=\"{{ResetUrl}}\">إعادة تعيين كلمة المرور</a></p>",
            "<p>Hello {{Name}},</p><p>To reset your password, please click the link below:</p><p><a href=\"{{ResetUrl}}\">Reset password</a></p>"),
    };
}
