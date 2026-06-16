using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Media.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Media;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CCE.Application.Media.Commands.UploadMedia;

internal sealed class UploadMediaCommandHandler
    : IRequestHandler<UploadMediaCommand, Response<MediaFileBriefDto>>
{
    private readonly IFileStorage _fileStorage;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MediaUploadOptions _opts;
    private readonly MessageFactory _msg;
    private readonly ISystemClock _clock;

    public UploadMediaCommandHandler(
        [FromKeyedServices("media")] IFileStorage fileStorage,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        IOptions<MediaUploadOptions> opts,
        MessageFactory msg,
        ISystemClock clock)
    {
        _fileStorage = fileStorage;
        _db = db;
        _currentUser = currentUser;
        _opts = opts.Value;
        _msg = msg;
        _clock = clock;
    }

    public async Task<Response<MediaFileBriefDto>> Handle(
        UploadMediaCommand request, CancellationToken ct)
    {
        if (request.FileSize == 0)
            return _msg.EmptyFile<MediaFileBriefDto>();

        if (request.FileSize > _opts.MaxSizeBytes)
            return _msg.FileTooLarge<MediaFileBriefDto>();

        if (!_opts.AllowedMimeTypes.Contains(request.ContentType))
            return _msg.InvalidFileType<MediaFileBriefDto>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Authenticated user required.");

        // Buffer into MemoryStream so the upload stream is seekable and has a known length —
        // same pattern as UploadAssetCommandHandler. Raw request streams are not seekable and
        // cause silent failures with S3-compatible APIs.
        await using var buffer = new MemoryStream();
        await request.FileStream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        buffer.Position = 0;

        var storageKey = await _fileStorage.SaveAsync(buffer, request.FileName, ct, request.ContentType)
            .ConfigureAwait(false);

        var url = _fileStorage.GetPublicUrl(storageKey).ToString();

        var mediaFile = MediaFile.Create(
            storageKey,
            url,
            request.FileName,
            request.ContentType,
            request.FileSize,
            userId,
            _clock,
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.AltTextAr,
            request.AltTextEn);

        _db.Add(mediaFile);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var dto = new MediaFileBriefDto(mediaFile.Id, mediaFile.StorageKey, mediaFile.Url);
        return _msg.Ok(dto, "MEDIA_UPLOADED");
    }
}
