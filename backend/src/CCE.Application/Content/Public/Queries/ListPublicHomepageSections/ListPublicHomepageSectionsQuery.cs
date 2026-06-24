using CCE.Application.Common;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicHomepageSections;

public sealed record ListPublicHomepageSectionsQuery() : IRequest<Response<System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto>>>;
