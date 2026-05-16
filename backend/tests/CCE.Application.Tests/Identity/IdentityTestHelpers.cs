using CCE.Application.Localization;
using CCE.Application.Messages;
using NSubstitute;

namespace CCE.Application.Tests.Identity;

public static class IdentityTestHelpers
{
    public static MessageFactory BuildMsg()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocalizedMessage(Arg.Any<string>())
            .Returns(call => new LocalizedMessage(
                Ar: call.Arg<string>(),
                En: call.Arg<string>()));

        return new MessageFactory(localization);
    }
}
