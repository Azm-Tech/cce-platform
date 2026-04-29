using CCE.Application.Assistant.Commands.AskAssistant;

namespace CCE.Application.Tests.Assistant;

public class AskAssistantCommandValidatorTests
{
    private readonly AskAssistantCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _sut.Validate(new AskAssistantCommand("What is CCE?", "en"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_question_fails()
    {
        var result = _sut.Validate(new AskAssistantCommand("", "en"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_locale_fails()
    {
        var result = _sut.Validate(new AskAssistantCommand("Hello", "fr"));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public void Valid_locales_pass(string locale)
    {
        var result = _sut.Validate(new AskAssistantCommand("Hello", locale));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Question_over_2000_chars_fails()
    {
        var result = _sut.Validate(new AskAssistantCommand(new string('x', 2001), "en"));
        result.IsValid.Should().BeFalse();
    }
}
