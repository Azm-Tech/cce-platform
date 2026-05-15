namespace CCE.Application.Content.Public;

public interface IResourceViewCountRepository
{
    Task IncrementAsync(System.Guid resourceId, CancellationToken ct);
}
