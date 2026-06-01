using System.ComponentModel.DataAnnotations.Schema;

namespace CCE.Domain.Content;

/// <summary>
/// Join entity linking a <see cref="Resource"/> to one of its covered countries.
/// </summary>
public sealed class ResourceCountry
{
    private ResourceCountry(System.Guid resourceId, System.Guid countryId)
    {
        ResourceId = resourceId;
        CountryId = countryId;
    }

    public System.Guid ResourceId { get; private set; }
    public System.Guid CountryId { get; private set; }

    public static ResourceCountry Create(System.Guid resourceId, System.Guid countryId)
        => new(resourceId, countryId);
}
