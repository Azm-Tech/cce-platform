using CCE.Application.Common;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.UpdateCommunityLawSection;

public sealed record UpdateCommunityLawSectionCommand(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn) : IRequest<Response<VoidData>>;
