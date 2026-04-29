using CCE.Application.Content;
using CCE.Application.Content.Commands.ReorderHomepageSections;

namespace CCE.Application.Tests.Content.Commands;

public class ReorderHomepageSectionsCommandHandlerTests
{
    [Fact]
    public async Task Forwards_assignments_to_service()
    {
        var service = Substitute.For<IHomepageSectionService>();
        var sut = new ReorderHomepageSectionsCommandHandler(service);
        var assignments = new[]
        {
            new HomepageSectionOrderAssignment(System.Guid.NewGuid(), 0),
            new HomepageSectionOrderAssignment(System.Guid.NewGuid(), 1),
        };

        await sut.Handle(new ReorderHomepageSectionsCommand(assignments), CancellationToken.None);

        await service.Received(1).ReorderAsync(
            Arg.Is<System.Collections.Generic.IReadOnlyList<(System.Guid, int)>>(list =>
                list.Count == 2 && list[0].Item2 == 0 && list[1].Item2 == 1),
            Arg.Any<CancellationToken>());
    }
}
