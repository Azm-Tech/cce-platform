using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Commands.UpdateResource;

public sealed class UpdateResourceCommandHandler : IRequestHandler<UpdateResourceCommand, Response<System.Guid>>
{
    private readonly IRepository<Resource, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public UpdateResourceCommandHandler(
        IRepository<Resource, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<System.Guid>> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _repo.GetByIdAsync(
            request.Id,
            q => q.Include(r => r.Countries),
            cancellationToken).ConfigureAwait(false);
        if (resource is null)
            return _messages.ResourceNotFound<System.Guid>();

        var categoryExists = await ExistsAsync(_db.ResourceCategories.Where(c => c.Id == request.CategoryId), cancellationToken).ConfigureAwait(false);
        if (!categoryExists)
            return _messages.CategoryNotFound<System.Guid>();

        var countryIds = request.CountryIds.Distinct().ToList();
        if (countryIds.Count > 0)
        {
            var existingCountryCount = await _db.Countries
                .Where(c => countryIds.Contains(c.Id))
                .CountAsyncEither(cancellationToken)
                .ConfigureAwait(false);
            if (existingCountryCount != countryIds.Count)
                return _messages.NotFound<System.Guid>("COUNTRY_NOT_FOUND");
        }

        var expectedRowVersion = resource.RowVersion;
        resource.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.ResourceType,
            request.CategoryId,
            request.CountryIds,
            request.KnowledgeLevelId,
            request.JobSectorId);

        _db.SetExpectedRowVersion(resource, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(resource.Id, "SUCCESS_OPERATION");
    }

    private static async Task<bool> ExistsAsync<T>(IQueryable<T> query, CancellationToken ct)
    {
        var list = await query.Take(1).ToListAsyncEither(ct).ConfigureAwait(false);
        return list.Count > 0;
    }
}
