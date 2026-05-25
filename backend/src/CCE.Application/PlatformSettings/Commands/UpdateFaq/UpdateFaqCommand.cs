using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateFaq;

public sealed record UpdateFaqCommand(
    System.Guid Id,
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    int Order) : IRequest<Response<System.Guid>>;
