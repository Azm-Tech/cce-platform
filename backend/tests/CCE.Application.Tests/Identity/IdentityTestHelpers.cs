using CCE.Application.Localization;
using NSubstitute;

namespace CCE.Application.Tests.Identity;

/// <summary>
/// Shared helpers for Identity handler tests that need a localized <see cref="Errors"/> factory.
/// </summary>
public static class IdentityTestHelpers
{
    /// <summary>
    /// Builds a <see cref="CCE.Application.Common.Errors"/> instance backed by an
    /// <see cref="ILocalizationService"/> stub that returns the key as both Ar and En text.
    /// </summary>
    public static CCE.Application.Common.Errors BuildErrors()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocalizedMessage(Arg.Any<string>())
            .Returns(call => new LocalizedMessage(
                Ar: call.Arg<string>(),
                En: call.Arg<string>()));

        return new CCE.Application.Common.Errors(localization);
    }
}
