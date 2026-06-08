using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.UpdateCommunity;

public sealed record UpdateCommunityCommand(
    Guid CommunityId,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string? PresentationJson) : IRequest<Response<VoidData>>;
