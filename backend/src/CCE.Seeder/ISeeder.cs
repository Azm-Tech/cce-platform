namespace CCE.Seeder;

public interface ISeeder
{
    /// <summary>Lower runs first. Roles → reference → demo.</summary>
    int Order { get; }

    /// <summary>Idempotent. Must be safe to run repeatedly.</summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
