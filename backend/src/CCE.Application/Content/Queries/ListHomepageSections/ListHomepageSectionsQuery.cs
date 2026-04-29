using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.ListHomepageSections;

public sealed record ListHomepageSectionsQuery() : IRequest<System.Collections.Generic.IReadOnlyList<HomepageSectionDto>>;
