using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.DeleteHomepageSection;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class DeleteHomepageSectionCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_section_missing()
    {
        var service = Substitute.For<IHomepageSectionService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((HomepageSection?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var sut = new DeleteHomepageSectionCommandHandler(service, currentUser, new FakeSystemClock());

        var act = async () => await sut.Handle(new DeleteHomepageSectionCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var section = HomepageSection.Create(HomepageSectionType.Hero, 0, "ar", "en");

        var service = Substitute.For<IHomepageSectionService>();
        service.FindAsync(section.Id, Arg.Any<CancellationToken>()).Returns(section);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new DeleteHomepageSectionCommandHandler(service, currentUser, new FakeSystemClock());

        var act = async () => await sut.Handle(new DeleteHomepageSectionCommand(section.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Soft_deletes_and_calls_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var actorId = System.Guid.NewGuid();
        var section = HomepageSection.Create(HomepageSectionType.Hero, 0, "ar", "en");

        var service = Substitute.For<IHomepageSectionService>();
        service.FindAsync(section.Id, Arg.Any<CancellationToken>()).Returns(section);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(actorId);

        var sut = new DeleteHomepageSectionCommandHandler(service, currentUser, clock);

        await sut.Handle(new DeleteHomepageSectionCommand(section.Id), CancellationToken.None);

        section.IsDeleted.Should().BeTrue();
        await service.Received(1).UpdateAsync(section, Arg.Any<CancellationToken>());
    }
}
