using CCE.Domain.Common;

namespace CCE.Domain.Content;

public sealed class Tag : Entity<System.Guid>
{
    private Tag(System.Guid id, string nameAr, string nameEn, string? color)
        : base(id)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Color = color;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string? Color { get; private set; }

    public static Tag Create(string nameAr, string nameEn, string? color)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");

        return new Tag(System.Guid.NewGuid(), nameAr, nameEn, color);
    }

    public void Update(string nameAr, string nameEn, string? color)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");

        NameAr = nameAr;
        NameEn = nameEn;
        Color = color;
    }
}
