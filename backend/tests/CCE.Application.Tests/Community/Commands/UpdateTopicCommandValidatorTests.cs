using CCE.Application.Community.Commands.UpdateTopic;

namespace CCE.Application.Tests.Community.Commands;

public class UpdateTopicCommandValidatorTests
{
    private readonly UpdateTopicCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes_validation()
    {
        var cmd = new UpdateTopicCommand(System.Guid.NewGuid(), "طاقة", "Energy", "وصف", "Description", 3, true);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_NameEn_fails_validation()
    {
        var cmd = new UpdateTopicCommand(System.Guid.NewGuid(), "طاقة", "", "وصف", "Description", 3, true);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.NameEn));
    }
}
