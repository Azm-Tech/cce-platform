using CCE.Application.Assistant;
using CCE.Application.Assistant.Commands.AskAssistant;
using FluentValidation.TestHelper;

namespace CCE.Application.Tests.Assistant;

public class AskAssistantCommandValidatorTests
{
    private readonly AskAssistantCommandValidator _sut = new();

    [Fact]
    public void Empty_messages_is_invalid()
    {
        var cmd = new AskAssistantCommand(new List<ChatMessage>(), "en");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Messages);
    }

    [Fact]
    public void Single_user_message_is_valid()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("user", "What is CCE?") }, "en");
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Last_message_must_be_user()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage>
            {
                new("user", "hi"),
                new("assistant", "hello"),
            }, "en");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Messages);
    }

    [Fact]
    public void Invalid_role_is_invalid()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("system", "hi") }, "en");
        var result = _sut.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public void Valid_locales_pass(string locale)
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("user", "hi") }, locale);
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Locale_must_be_ar_or_en()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("user", "hi") }, "fr");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Locale);
    }

    [Fact]
    public void Content_max_length_4000_is_enforced()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("user", new string('x', 4001)) }, "en");
        var result = _sut.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Max_50_messages_is_enforced()
    {
        var msgs = Enumerable.Range(0, 51)
            .Select(i => new ChatMessage(i % 2 == 0 ? "user" : "assistant", $"m{i}"))
            .ToList();
        // Make sure last is user
        msgs[^1] = new ChatMessage("user", "last");
        var cmd = new AskAssistantCommand(msgs, "en");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Messages);
    }
}
