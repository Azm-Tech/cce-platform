using CCE.Application.Content.Commands.UpdatePage;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class UpdatePageCommandValidatorTests
{
    private static UpdatePageCommand ValidCmd() => new(
        System.Guid.NewGuid(),
        "عنوان", "Title",
        "محتوى", "Content",
        new byte[8]);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new UpdatePageCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_is_rejected()
    {
        var sut = new UpdatePageCommandValidator();
        var cmd = ValidCmd() with { Id = System.Guid.Empty };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdatePageCommand.Id));
    }

    [Fact]
    public void RowVersion_wrong_length_is_rejected()
    {
        var sut = new UpdatePageCommandValidator();
        var cmd = ValidCmd() with { RowVersion = new byte[4] };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdatePageCommand.RowVersion));
    }
}
