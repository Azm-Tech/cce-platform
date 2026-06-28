using CCE.Application.Common;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetShareLink;

public sealed record GetShareLinkQuery(
    ShareContentType Type,
    System.Guid Id) : IRequest<Response<ShareLinkDto>>;
