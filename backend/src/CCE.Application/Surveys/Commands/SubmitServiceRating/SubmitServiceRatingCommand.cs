using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Surveys.Commands.SubmitServiceRating;

public sealed record SubmitServiceRatingCommand(
    int Rating,
    string? CommentAr,
    string? CommentEn,
    string Page,
    string Locale) : IRequest<Response<Guid>>;
