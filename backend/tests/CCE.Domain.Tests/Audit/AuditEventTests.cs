using CCE.Domain.Audit;

namespace CCE.Domain.Tests.Audit;

public class AuditEventTests
{
    [Fact]
    public void Constructor_assigns_provided_values()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var occurredOn = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        var correlationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var sut = new AuditEvent(
            id,
            occurredOn,
            actor: "admin@cce.local",
            action: "User.Create",
            resource: "User/abc-123",
            correlationId: correlationId,
            diff: """{"field":"email","from":null,"to":"x@y.local"}""");

        sut.Id.Should().Be(id);
        sut.OccurredOn.Should().Be(occurredOn);
        sut.Actor.Should().Be("admin@cce.local");
        sut.Action.Should().Be("User.Create");
        sut.Resource.Should().Be("User/abc-123");
        sut.CorrelationId.Should().Be(correlationId);
        sut.Diff.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_blank_actor(string? actor)
    {
        var act = () => new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actor: actor!,
            action: "Test",
            resource: "Res",
            correlationId: Guid.NewGuid(),
            diff: null);

        act.Should().Throw<ArgumentException>().WithParameterName(nameof(actor));
    }

    [Fact]
    public void Constructor_rejects_blank_action()
    {
        var act = () => new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actor: "x",
            action: "",
            resource: "Res",
            correlationId: Guid.NewGuid(),
            diff: null);

        act.Should().Throw<ArgumentException>().WithParameterName("action");
    }

    [Fact]
    public void Diff_can_be_null_for_actions_without_state_change()
    {
        var sut = new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actor: "x",
            action: "User.Login",
            resource: "User/x",
            correlationId: Guid.NewGuid(),
            diff: null);

        sut.Diff.Should().BeNull();
    }
}
