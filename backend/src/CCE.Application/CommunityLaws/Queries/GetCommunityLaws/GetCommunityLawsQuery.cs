using CCE.Application.Common;
using CCE.Application.CommunityLaws.Dtos;
using MediatR;

namespace CCE.Application.CommunityLaws.Queries.GetCommunityLaws;

public sealed record GetCommunityLawsQuery : IRequest<Response<List<CommunityLawSectionDto>>>;
