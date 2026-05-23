using CCE.Application.Localization;
using CCE.Application.Messages;

namespace CCE.Application.Tests.Notifications;

internal static class NotificationTestMessages
{
    public static MessageFactory Create()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call[0]!.ToString()!);
        return new MessageFactory(localization);
    }
}
