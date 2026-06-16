namespace CCE.Application.Content;

public interface IUserContentInterestResolver
{
    Task<(System.Guid? KnowledgeLevelId, System.Guid? JobSectorId)> ResolveAsync(
        System.Guid? explicitKnowledgeLevelId,
        System.Guid? explicitJobSectorId,
        CancellationToken ct);
}
