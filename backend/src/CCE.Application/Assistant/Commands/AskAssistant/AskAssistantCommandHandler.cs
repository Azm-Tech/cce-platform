using MediatR;

namespace CCE.Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandHandler
    : IRequestHandler<AskAssistantCommand, SmartAssistantReplyDto>
{
    private readonly ISmartAssistantClient _client;

    public AskAssistantCommandHandler(ISmartAssistantClient client)
    {
        _client = client;
    }

    public Task<SmartAssistantReplyDto> Handle(AskAssistantCommand request, CancellationToken cancellationToken)
        => _client.AskAsync(request.Question, request.Locale, cancellationToken);
}
