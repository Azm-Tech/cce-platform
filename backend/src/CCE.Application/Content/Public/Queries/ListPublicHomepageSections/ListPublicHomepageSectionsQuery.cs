using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicHomepageSections;

public sealed record ListPublicHomepageSectionsQuery() : IRequest<System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto>>;
