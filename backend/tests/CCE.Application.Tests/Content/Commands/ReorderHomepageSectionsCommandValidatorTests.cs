using CCE.Application.Content.Commands.ReorderHomepageSections;

namespace CCE.Application.Tests.Content.Commands;

public class ReorderHomepageSectionsCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new ReorderHomepageSectionsCommandValidator();
        var cmd = new ReorderHomepageSectionsCommand(new[]
        {
            new HomepageSectionOrderAssignment(System.Guid.NewGuid(), 0),
            new HomepageSectionOrderAssignment(System.Guid.NewGuid(), 1),
        });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_assignments_is_rejected()
    {
        var sut = new ReorderHomepageSectionsCommandValidator();
        var cmd = new ReorderHomepageSectionsCommand(System.Array.Empty<HomepageSectionOrderAssignment>());

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new ReorderHomepageSectionsCommandValidator();
        var cmd = new ReorderHomepageSectionsCommand(new[]
        {
            new HomepageSectionOrderAssignment(System.Guid.Empty, 0),
        });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_order_index_is_rejected()
    {
        var sut = new ReorderHomepageSectionsCommandValidator();
        var cmd = new ReorderHomepageSectionsCommand(new[]
        {
            new HomepageSectionOrderAssignment(System.Guid.NewGuid(), -1),
        });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Duplicate_ids_are_rejected()
    {
        var sut = new ReorderHomepageSectionsCommandValidator();
        var sharedId = System.Guid.NewGuid();
        var cmd = new ReorderHomepageSectionsCommand(new[]
        {
            new HomepageSectionOrderAssignment(sharedId, 0),
            new HomepageSectionOrderAssignment(sharedId, 1),
        });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("more than once", System.StringComparison.OrdinalIgnoreCase));
    }
}
