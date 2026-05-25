using CCE.Application.Content;
using CCE.Application.Content.Commands.CreateHomepageSection;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class CreateHomepageSectionCommandHandlerTests
{
    [Fact]
    public async Task Persists_section_and_returns_dto()
    {
        var service = Substitute.For<IHomepageSectionRepository>();
        var sut = new CreateHomepageSectionCommandHandler(service);

        var cmd = new CreateHomepageSectionCommand(
            HomepageSectionType.Hero, 0, "ar-content", "en-content");

        var dto = await sut.Handle(cmd, CancellationToken.None);

        await service.Received(1).SaveAsync(Arg.Any<HomepageSection>(), Arg.Any<CancellationToken>());
        dto.SectionType.Should().Be(HomepageSectionType.Hero);
        dto.OrderIndex.Should().Be(0);
        dto.ContentAr.Should().Be("ar-content");
        dto.ContentEn.Should().Be("en-content");
        dto.IsActive.Should().BeTrue();
    }
}
