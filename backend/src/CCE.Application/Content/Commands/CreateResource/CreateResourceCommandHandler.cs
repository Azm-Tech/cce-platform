using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateResource;

public sealed class CreateResourceCommandHandler : IRequestHandler<CreateResourceCommand, Response<System.Guid>>
{
    private readonly IRepository<Resource, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public CreateResourceCommandHandler(
        IRepository<Resource, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<System.Guid>> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        var assets = await _db.AssetFiles
            .Where(a => a.Id == request.AssetFileId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var asset = assets.SingleOrDefault();

        if (asset is null)
            return _messages.AssetNotFound<System.Guid>();
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
            return _messages.AssetNotClean<System.Guid>();

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

        var uploadedById = _currentUser.GetUserId();
        if (uploadedById is null)
            return _messages.NotAuthenticated<System.Guid>();

        var resource = Resource.Draft(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.ResourceType,
            request.CategoryId,
            request.CountryId,
            uploadedById.Value,
            request.AssetFileId,
            request.CountryIds,
            _clock,
            request.KnowledgeLevelId,
            request.JobSectorId);

        await _repo.AddAsync(resource, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(resource.Id, "RESOURCE_CREATED");
    }

    private static async Task<bool> ExistsAsync<T>(IQueryable<T> query, CancellationToken ct)
    {
        var list = await query.Take(1).ToListAsyncEither(ct).ConfigureAwait(false);
        return list.Count > 0;
    }
}
