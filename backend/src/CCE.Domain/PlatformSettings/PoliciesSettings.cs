using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

[Audited]
public sealed class PoliciesSettings : AggregateRoot<System.Guid>
{
    private PoliciesSettings(System.Guid id) : base(id)
    {
    }

    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static PoliciesSettings Create() =>
        new(System.Guid.NewGuid());
}
