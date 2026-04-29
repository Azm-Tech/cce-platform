using MediatR;

namespace CCE.Application.Assistant.Commands.AskAssistant;

public sealed record AskAssistantCommand(string Question, string Locale)
    : IRequest<SmartAssistantReplyDto>;
