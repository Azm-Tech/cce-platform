using CCE.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace CCE.Application.Tests.Common.Behaviors;

public sealed record LoggingBehaviorTestRequest(string Name) : IRequest<string>;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Logs_entry_and_success_for_handled_request()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingBehaviorTestRequest, string>>>();
        var sut = new LoggingBehavior<LoggingBehaviorTestRequest, string>(logger);

        var result = await sut.Handle(new LoggingBehaviorTestRequest("x"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        logger.ReceivedCalls()
            .Where(c => c.GetArguments()[0] is LogLevel l && l == LogLevel.Information)
            .Should().HaveCountGreaterThanOrEqualTo(2);   // entry + success
    }

    [Fact]
    public async Task Lets_exceptions_escape()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingBehaviorTestRequest, string>>>();
        var sut = new LoggingBehavior<LoggingBehaviorTestRequest, string>(logger);
        Task<string> Failing() => Task.FromException<string>(new InvalidOperationException("boom"));

        var act = async () => await sut.Handle(new LoggingBehaviorTestRequest("x"), Failing, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }
}
