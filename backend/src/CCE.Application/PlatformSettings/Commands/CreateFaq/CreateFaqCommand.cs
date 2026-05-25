using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateFaq;

public sealed record CreateFaqCommand(
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    int Order = 0) : IRequest<Response<System.Guid>>;
