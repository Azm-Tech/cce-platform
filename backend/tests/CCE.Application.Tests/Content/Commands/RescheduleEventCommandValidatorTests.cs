using CCE.Application.Content.Commands.RescheduleEvent;

namespace CCE.Application.Tests.Content.Commands;

public class RescheduleEventCommandValidatorTests
{
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    private static RescheduleEventCommand ValidCmd() => new(
        System.Guid.NewGuid(), StartsOn, EndsOn, new byte[8]);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new RescheduleEventCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EndsOn_not_after_StartsOn_is_rejected()
    {
        var sut = new RescheduleEventCommandValidator();
        var cmd = ValidCmd() with { EndsOn = StartsOn };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RescheduleEventCommand.EndsOn));
    }
}
