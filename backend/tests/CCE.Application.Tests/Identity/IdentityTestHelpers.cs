using CCE.Application.Localization;
using CCE.Application.Messages;
using NSubstitute;

namespace CCE.Application.Tests.Identity;

public static class IdentityTestHelpers
{
    public static MessageFactory BuildMsg()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(call => call.ArgAt<string>(0));

        return new MessageFactory(localization);
    }
}
