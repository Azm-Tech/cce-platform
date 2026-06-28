namespace CCE.Application.Search;

public sealed record SearchHitDto(
    System.Guid Id,
    SearchableType Type,
    string TitleAr,
    string TitleEn,
    string ExcerptAr,
    string ExcerptEn,
    double Score);
