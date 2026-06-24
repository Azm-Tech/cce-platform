using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.PublishResource;

public sealed class PublishResourceCommandHandler : IRequestHandler<PublishResourceCommand, Response<System.Guid>>
{
    private readonly IRepository<Resource, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public PublishResourceCommandHandler(
        IRepository<Resource, System.Guid> repo,
        ICceDbContext db,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<System.Guid>> Handle(PublishResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (resource is null)
            return _messages.ResourceNotFound<System.Guid>();

        var assets = await _db.AssetFiles
            .Where(a => a.Id == resource.AssetFileId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var asset = assets.SingleOrDefault();
        if (asset is null)
            return _messages.AssetNotFound<System.Guid>();
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
            return _messages.AssetNotClean<System.Guid>();

        var expectedRowVersion = resource.RowVersion;
        resource.Publish(_clock);

        _db.SetExpectedRowVersion(resource, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(resource.Id, MessageKeys.General.SUCCESS_OPERATION);
    }
}
