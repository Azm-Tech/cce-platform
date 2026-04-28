using CCE.Application.Content.Commands.UpdateResource;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateResourceCommandValidatorTests
{
    private static UpdateResourceCommand ValidCmd() => new(
        System.Guid.NewGuid(),
        "عنوان", "Title",
        "وصف", "Description",
        ResourceType.Pdf,
        System.Guid.NewGuid(),
        new byte[8]);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new UpdateResourceCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_is_rejected()
    {
        var sut = new UpdateResourceCommandValidator();
        var cmd = ValidCmd() with { Id = System.Guid.Empty };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateResourceCommand.Id));
    }

    [Fact]
    public void RowVersion_wrong_length_is_rejected()
    {
        var sut = new UpdateResourceCommandValidator();
        var cmd = ValidCmd() with { RowVersion = new byte[4] };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateResourceCommand.RowVersion));
    }
}
