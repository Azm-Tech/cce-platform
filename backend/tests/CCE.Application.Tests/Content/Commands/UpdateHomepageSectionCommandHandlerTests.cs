using CCE.Application.Content;
using CCE.Application.Content.Commands.UpdateHomepageSection;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateHomepageSectionCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_section_not_found()
    {
        var service = Substitute.For<IHomepageSectionRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((HomepageSection?)null);
        var sut = new UpdateHomepageSectionCommandHandler(service);

        var result = await sut.Handle(
            new UpdateHomepageSectionCommand(System.Guid.NewGuid(), "ar", "en", true),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_content_and_activates_section()
    {
        var section = HomepageSection.Create(HomepageSectionType.Hero, 0, "old-ar", "old-en");
        section.Deactivate();

        var service = Substitute.For<IHomepageSectionRepository>();
        service.FindAsync(section.Id, Arg.Any<CancellationToken>()).Returns(section);
        var sut = new UpdateHomepageSectionCommandHandler(service);

        var result = await sut.Handle(
            new UpdateHomepageSectionCommand(section.Id, "new-ar", "new-en", true),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ContentAr.Should().Be("new-ar");
        result.ContentEn.Should().Be("new-en");
        result.IsActive.Should().BeTrue();
        await service.Received(1).UpdateAsync(section, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivates_section_when_IsActive_false()
    {
        var section = HomepageSection.Create(HomepageSectionType.FeaturedNews, 1, "ar", "en");

        var service = Substitute.For<IHomepageSectionRepository>();
        service.FindAsync(section.Id, Arg.Any<CancellationToken>()).Returns(section);
        var sut = new UpdateHomepageSectionCommandHandler(service);

        var result = await sut.Handle(
            new UpdateHomepageSectionCommand(section.Id, "ar", "en", false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        await service.Received(1).UpdateAsync(section, Arg.Any<CancellationToken>());
    }
}
