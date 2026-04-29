using CCE.Application.Community.Commands.CreateTopic;

namespace CCE.Application.Tests.Community.Commands;

public class CreateTopicCommandValidatorTests
{
    private readonly CreateTopicCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes_validation()
    {
        var cmd = new CreateTopicCommand("طاقة", "Energy", "وصف", "Description", "energy", null, null, 0);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_NameAr_fails_validation()
    {
        var cmd = new CreateTopicCommand("", "Energy", "وصف", "Description", "energy", null, null, 0);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.NameAr));
    }

    [Fact]
    public void Http_IconUrl_fails_validation()
    {
        var cmd = new CreateTopicCommand("طاقة", "Energy", "وصف", "Description", "energy", null, "http://example.com/icon.png", 0);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.IconUrl));
    }
}
