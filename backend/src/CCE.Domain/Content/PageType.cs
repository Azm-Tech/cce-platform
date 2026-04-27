namespace CCE.Domain.Content;

/// <summary>Type of static <c>Page</c>. Mostly drives URL routing + navigation.</summary>
public enum PageType
{
    AboutPlatform = 0,
    TermsOfService = 1,
    PrivacyPolicy = 2,
    /// <summary>Free-form page added by content managers.</summary>
    Custom = 99,
}
