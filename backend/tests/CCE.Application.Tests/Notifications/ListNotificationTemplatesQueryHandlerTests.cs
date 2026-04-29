using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Queries.ListNotificationTemplates;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class ListNotificationTemplatesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_templates_exist()
    {
        var db = BuildDb(System.Array.Empty<NotificationTemplate>());
        var sut = new ListNotificationTemplatesQueryHandler(db);

        var result = await sut.Handle(new ListNotificationTemplatesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_templates_sorted_by_Code_ascending()
    {
        var alpha = NotificationTemplate.Define("ALPHA_CODE", "أ", "Alpha Subject", "جسم", "Alpha Body", NotificationChannel.Email, "{}");
        var beta = NotificationTemplate.Define("BETA_CODE", "ب", "Beta Subject", "جسم", "Beta Body", NotificationChannel.Email, "{}");

        var db = BuildDb(new[] { beta, alpha });
        var sut = new ListNotificationTemplatesQueryHandler(db);

        var result = await sut.Handle(new ListNotificationTemplatesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items[0].Code.Should().Be("ALPHA_CODE");
        result.Items[1].Code.Should().Be("BETA_CODE");
    }

    [Fact]
    public async Task Filters_by_channel_and_isActive()
    {
        var email = NotificationTemplate.Define("EMAIL_TMPL", "أ", "Email Subject", "جسم", "Email Body", NotificationChannel.Email, "{}");
        var sms = NotificationTemplate.Define("SMS_TMPL", "ب", "Sms Subject", "جسم", "Sms Body", NotificationChannel.Sms, "{}");
        sms.Deactivate();

        var db = BuildDb(new[] { email, sms });
        var sut = new ListNotificationTemplatesQueryHandler(db);

        var result = await sut.Handle(new ListNotificationTemplatesQuery(Channel: NotificationChannel.Email, IsActive: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().Code.Should().Be("EMAIL_TMPL");
    }

    private static ICceDbContext BuildDb(IEnumerable<NotificationTemplate> templates)
    {
        var db = Substitute.For<ICceDbContext>();
        db.NotificationTemplates.Returns(templates.AsQueryable());
        return db;
    }
}
