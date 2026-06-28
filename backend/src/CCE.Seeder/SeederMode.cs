namespace CCE.Seeder;

/// <summary>
/// Parsed CCE.Seeder CLI flags. Exactly one of the four mutually-exclusive
/// modes is selected:
///   - <see cref="Kind.RunSeeders"/> (default; existing dev behaviour)
///   - <see cref="Kind.RunSeedersWithDemo"/> (--demo)
///   - <see cref="Kind.MigrateOnly"/> (--migrate)
///   - <see cref="Kind.MigrateAndSeedReference"/> (--migrate --seed-reference)
/// Illegal combinations (e.g. --migrate --demo) produce <see cref="Kind.Error"/>.
/// </summary>
public sealed record SeederMode(SeederMode.Kind Mode, string? ErrorMessage)
{
    public enum Kind
    {
        RunSeeders,
        RunSeedersWithDemo,
        RunSeedersWithBulk,
        MigrateOnly,
        MigrateAndSeedReference,
        Error,
    }

    public static SeederMode Parse(string[] args)
    {
        var hasDemo = args.Contains("--demo", StringComparer.OrdinalIgnoreCase);
        var hasBulk = args.Contains("--bulk", StringComparer.OrdinalIgnoreCase);
        var hasMigrate = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);
        var hasSeedRef = args.Contains("--seed-reference", StringComparer.OrdinalIgnoreCase);

        if (hasMigrate && (hasDemo || hasBulk))
        {
            return new(Kind.Error, "--migrate cannot be combined with --demo or --bulk.");
        }
        if (hasSeedRef && !hasMigrate)
        {
            return new(Kind.Error, "--seed-reference requires --migrate.");
        }
        if (hasMigrate && hasSeedRef) return new(Kind.MigrateAndSeedReference, null);
        if (hasMigrate)              return new(Kind.MigrateOnly, null);
        if (hasBulk)                 return new(Kind.RunSeedersWithBulk, null);
        if (hasDemo)                 return new(Kind.RunSeedersWithDemo, null);
        return new(Kind.RunSeeders, null);
    }
}
