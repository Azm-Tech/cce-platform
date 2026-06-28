namespace CCE.Domain.Community;

/// <summary>
/// Reddit-style "hot" rank used to order posts and replies. Combines the net score
/// (upvotes − downvotes) on a log scale with a time component so newer content ranks
/// higher for equal votes. Stored denormalized on <see cref="Post"/>/<see cref="PostReply"/>
/// and indexed for cheap <c>ORDER BY score DESC</c> reads.
/// </summary>
internal static class VoteScore
{
    public static double Hot(int upvotes, int downvotes, System.DateTimeOffset createdOn)
    {
        var net = upvotes - downvotes;
        var order = System.Math.Log10(System.Math.Max(System.Math.Abs(net), 1));
        var sign = net > 0 ? 1 : net < 0 ? -1 : 0;
        var seconds = createdOn.ToUnixTimeSeconds();
        return System.Math.Round((sign * order) + (seconds / 45000.0), 7);
    }
}
