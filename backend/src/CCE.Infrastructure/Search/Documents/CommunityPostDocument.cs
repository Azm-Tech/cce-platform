namespace CCE.Infrastructure.Search;

internal sealed class CommunityPostDocument
{
    public string  Id          { get; set; } = string.Empty;
    public string? TitleAr     { get; set; } // set when Post.Locale == "ar"
    public string? TitleEn     { get; set; } // set when Post.Locale == "en"
    public string? ContentAr   { get; set; } // set when Post.Locale == "ar"
    public string? ContentEn   { get; set; } // set when Post.Locale == "en"
    public string? AuthorName  { get; set; } // FirstName + LastName, fallback UserName
    public string? TagNamesAr  { get; set; } // space-separated Tag.NameAr values
    public string? TagNamesEn  { get; set; } // space-separated Tag.NameEn values
}
