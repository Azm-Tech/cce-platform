using CCE.Application.Content.Commands.UpdateEvent;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateEventCommandValidatorTests
{
    private static UpdateEventCommand ValidCmd() => new(
        System.Guid.NewGuid(),
        "حدث", "Event",
        "وصف", "Description",
        null, null, null, null,
        new byte[8]);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new UpdateEventCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_is_rejected()
    {
        var sut = new UpdateEventCommandValidator();
        var cmd = ValidCmd() with { Id = System.Guid.Empty };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEventCommand.Id));
    }

    [Fact]
    public void RowVersion_wrong_length_is_rejected()
    {
        var sut = new UpdateEventCommandValidator();
        var cmd = ValidCmd() with { RowVersion = new byte[4] };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEventCommand.RowVersion));
    }
}
