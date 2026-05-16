using CCE.Application.Health;
using CCE.Application.Localization;
using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public async Task Mediator_resolves_HealthQuery_handler_through_pipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISystemClock>(new FakeSystemClock());
        services.AddSingleton<ILocalizationService>(_ =>
        {
            var l = NSubstitute.Substitute.For<ILocalizationService>();
            l.GetLocalizedMessage(Arg.Any<string>()).Returns(new LocalizedMessage("ar", "en"));
            return l;
        });
        services.AddApplication();

        await using var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new HealthQuery(Locale: "en"));

        result.Status.Should().Be("ok");
        result.Locale.Should().Be("en");
    }

    [Fact]
    public async Task Mediator_resolves_AuthenticatedHealthQuery_handler_through_pipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISystemClock>(new FakeSystemClock());
        services.AddSingleton<ILocalizationService>(_ =>
        {
            var l = NSubstitute.Substitute.For<ILocalizationService>();
            l.GetLocalizedMessage(Arg.Any<string>()).Returns(new LocalizedMessage("ar", "en"));
            return l;
        });
        services.AddApplication();

        await using var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new AuthenticatedHealthQuery(
            UserId: "u",
            PreferredUsername: "u@local",
            Email: "u@local",
            Upn: "u@local",
            Groups: ["SuperAdmin"],
            Locale: "ar"));

        result.Status.Should().Be("ok");
        result.User.Groups.Should().Contain("SuperAdmin");
    }
}
