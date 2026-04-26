using CCE.Domain.Common;

namespace CCE.Domain.Tests.Common;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_with_message_assigns_message()
    {
        var ex = new DomainException("something went wrong");
        ex.Message.Should().Be("something went wrong");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_with_message_and_inner_assigns_both()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DomainException("outer", inner);
        ex.Message.Should().Be("outer");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void Is_an_Exception()
    {
        var ex = new DomainException("x");
        ex.Should().BeAssignableTo<Exception>();
    }
}
