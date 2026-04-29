namespace CCE.Application.Content.Public;

public interface IResourceViewCountService
{
    Task IncrementAsync(System.Guid resourceId, CancellationToken ct);
}
