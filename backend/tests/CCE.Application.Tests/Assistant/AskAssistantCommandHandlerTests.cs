using CCE.Application.Assistant;
using CCE.Application.Assistant.Commands.AskAssistant;

namespace CCE.Application.Tests.Assistant;

public class AskAssistantCommandHandlerTests
{
    [Fact]
    public async Task Returns_reply_from_client()
    {
        var client = Substitute.For<ISmartAssistantClient>();
        var expected = new SmartAssistantReplyDto("Test reply");
        client.AskAsync("What is CCE?", "en", default).ReturnsForAnyArgs(expected);

        var sut = new AskAssistantCommandHandler(client);
        var result = await sut.Handle(new AskAssistantCommand("What is CCE?", "en"), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Passes_question_and_locale_to_client()
    {
        var client = Substitute.For<ISmartAssistantClient>();
        client.AskAsync(default!, default!, default).ReturnsForAnyArgs(new SmartAssistantReplyDto("ok"));

        var sut = new AskAssistantCommandHandler(client);
        await sut.Handle(new AskAssistantCommand("ما هو CCE؟", "ar"), CancellationToken.None);

        await client.Received(1).AskAsync("ما هو CCE؟", "ar", CancellationToken.None);
    }
}
