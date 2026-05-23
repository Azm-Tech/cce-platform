using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Queries.GetNotificationTemplateById;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class GetNotificationTemplateByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_failure_response_when_template_not_found()
    {
        var db = BuildDb(System.Array.Empty<NotificationTemplate>());
        var sut = new GetNotificationTemplateByIdQueryHandler(db, NotificationTestMessages.Create());

        var result = await sut.Handle(new GetNotificationTemplateByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var template = NotificationTemplate.Define(
            "WELCOME_EMAIL",
            "مرحبا",
            "Welcome",
            "جسم عربي",
            "English body",
            NotificationChannel.Email,
            "{\"name\": \"string\"}");

        var db = BuildDb(new[] { template });
        var sut = new GetNotificationTemplateByIdQueryHandler(db, NotificationTestMessages.Create());

        var result = await sut.Handle(new GetNotificationTemplateByIdQuery(template.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Data!.Id.Should().Be(template.Id);
        result.Data.Code.Should().Be("WELCOME_EMAIL");
        result.Data.SubjectAr.Should().Be("مرحبا");
        result.Data.SubjectEn.Should().Be("Welcome");
        result.Data.BodyAr.Should().Be("جسم عربي");
        result.Data.BodyEn.Should().Be("English body");
        result.Data.Channel.Should().Be(NotificationChannel.Email);
        result.Data.VariableSchemaJson.Should().Be("{\"name\": \"string\"}");
        result.Data.IsActive.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<NotificationTemplate> templates)
    {
        var db = Substitute.For<ICceDbContext>();
        db.NotificationTemplates.Returns(templates.AsQueryable());
        return db;
    }
}
