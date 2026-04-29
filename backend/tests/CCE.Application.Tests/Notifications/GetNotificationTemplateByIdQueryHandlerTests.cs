using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Queries.GetNotificationTemplateById;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class GetNotificationTemplateByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_template_not_found()
    {
        var db = BuildDb(System.Array.Empty<NotificationTemplate>());
        var sut = new GetNotificationTemplateByIdQueryHandler(db);

        var result = await sut.Handle(new GetNotificationTemplateByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
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
        var sut = new GetNotificationTemplateByIdQueryHandler(db);

        var result = await sut.Handle(new GetNotificationTemplateByIdQuery(template.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Code.Should().Be("WELCOME_EMAIL");
        result.SubjectAr.Should().Be("مرحبا");
        result.SubjectEn.Should().Be("Welcome");
        result.BodyAr.Should().Be("جسم عربي");
        result.BodyEn.Should().Be("English body");
        result.Channel.Should().Be(NotificationChannel.Email);
        result.VariableSchemaJson.Should().Be("{\"name\": \"string\"}");
        result.IsActive.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<NotificationTemplate> templates)
    {
        var db = Substitute.For<ICceDbContext>();
        db.NotificationTemplates.Returns(templates.AsQueryable());
        return db;
    }
}
