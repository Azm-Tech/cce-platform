using CCE.Application.Common;
using MediatR;

namespace CCE.Application.CommunityLaws.Commands.CreateCommunityLawSection;

public sealed record CreateCommunityLawSectionCommand(
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn) : IRequest<Response<VoidData>>;
