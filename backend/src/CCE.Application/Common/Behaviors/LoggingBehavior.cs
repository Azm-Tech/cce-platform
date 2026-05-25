using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CCE.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs handler entry, success, and elapsed time.
/// Logs at <see cref="LogLevel.Information"/>. Exceptions are not caught — they escape
/// to the next pipeline stage (typically the API middleware that converts to ProblemDetails).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        //_logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        sw.Stop();

        //_logger.LogInformation(
        //    "Handled {RequestName} in {ElapsedMs}ms",
        //    requestName,
        //    sw.ElapsedMilliseconds);

        return response;
    }
}
