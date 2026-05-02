using CCE.Application.Assistant;
using System.Runtime.CompilerServices;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Stub implementation of <see cref="ISmartAssistantClient"/>.
/// Phase 01 Task 1.2 fills in the fake-streamer.
/// </summary>
public sealed class SmartAssistantClient : ISmartAssistantClient
{
#pragma warning disable CS1998 // async method without await
    public async IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string locale,
        [EnumeratorCancellation] CancellationToken ct)
#pragma warning restore CS1998
    {
        // Phase 01 Task 1.2 fills in the fake-streamer.
        yield break;
    }
}
