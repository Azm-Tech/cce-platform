using System.Collections.Generic;

namespace CCE.Application.Community.Commands.CreatePost;

/// <summary>Poll definition for a Poll-type post (required iff Type == Poll).</summary>
public sealed record PollInput(
    System.DateTimeOffset Deadline,
    bool AllowMultiple,
    bool IsAnonymous,
    bool ShowResultsBeforeClose,
    IReadOnlyList<string> OptionLabels);
