using System.Security.Cryptography;
using System.Text;

namespace CCE.Seeder;

/// <summary>
/// Deterministic Guid derived from a string. Used by seeders so re-runs
/// match existing rows by Id rather than creating duplicates.
/// </summary>
public static class DeterministicGuid
{
    /// <summary>SHA-256 → first 16 bytes → Guid. Stable across processes. Not used for security.</summary>
    public static System.Guid From(string seed)
    {
        if (string.IsNullOrEmpty(seed)) throw new System.ArgumentException("seed required", nameof(seed));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var bytes = new byte[16];
        System.Array.Copy(hash, bytes, 16);
        return new System.Guid(bytes);
    }
}
