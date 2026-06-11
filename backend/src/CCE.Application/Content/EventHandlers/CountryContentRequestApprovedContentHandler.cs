using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Country.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.EventHandlers;

public sealed class CountryContentRequestApprovedContentHandler
    : INotificationHandler<CountryContentRequestApprovedEvent>
{
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;

    public CountryContentRequestApprovedContentHandler(
        ICceDbContext db,
        ISystemClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(
        CountryContentRequestApprovedEvent notification,
        CancellationToken cancellationToken)
    {
        var request = await _db.CountryContentRequests
            .FirstOrDefaultAsync(r => r.Id == notification.RequestId, cancellationToken)
            .ConfigureAwait(false);

        if (request is null)
            return;

        switch (request.Type)
        {
            case ContentType.Resource:
                await CreateResourceAsync(request, cancellationToken).ConfigureAwait(false);
                break;
            case ContentType.News:
                await CreateNewsAsync(request, cancellationToken).ConfigureAwait(false);
                break;
            case ContentType.Event:
                await CreateEventAsync(request, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async Task CreateResourceAsync(CountryContentRequest request, CancellationToken ct)
    {
        var categoryId = request.ProposedCategoryId
            ?? throw new DomainException("CategoryId is required for resource requests.");

        var resource = Resource.Draft(
            request.ProposedTitleAr,
            request.ProposedTitleEn,
            request.ProposedDescriptionAr,
            request.ProposedDescriptionEn,
            request.ProposedResourceType ?? throw new DomainException("ResourceType is required for resource requests."),
            categoryId,
            request.CountryId,
            request.RequestedById,
            request.ProposedAssetFileId ?? throw new DomainException("AssetFileId is required for resource requests."),
            [request.CountryId],
            _clock);

        resource.Publish(_clock);
        _db.Add(resource);
    }

    private async Task CreateNewsAsync(CountryContentRequest request, CancellationToken ct)
    {
        string? featuredImageUrl = null;
        if (request.ProposedAssetFileId.HasValue)
        {
            var asset = await _db.AssetFiles
                .FirstOrDefaultAsync(a => a.Id == request.ProposedAssetFileId.Value, ct)
                .ConfigureAwait(false);
            featuredImageUrl = asset?.Url;
        }

        var news = News.Draft(
            request.ProposedTitleAr,
            request.ProposedTitleEn,
            request.ProposedDescriptionAr,
            request.ProposedDescriptionEn,
            request.ProposedTopicId ?? throw new DomainException("TopicId is required for news requests."),
            request.RequestedById,
            featuredImageUrl,
            _clock);

        news.Publish(_clock);
        _db.Add(news);
    }

    private async Task CreateEventAsync(CountryContentRequest request, CancellationToken ct)
    {
        string? featuredImageUrl = null;
        if (request.ProposedAssetFileId.HasValue)
        {
            var asset = await _db.AssetFiles
                .FirstOrDefaultAsync(a => a.Id == request.ProposedAssetFileId.Value, ct)
                .ConfigureAwait(false);
            featuredImageUrl = asset?.Url;
        }

        var ev = Event.Schedule(
            request.ProposedTitleAr,
            request.ProposedTitleEn,
            request.ProposedDescriptionAr,
            request.ProposedDescriptionEn,
            request.ProposedStartsOn ?? throw new DomainException("StartsOn is required for event requests."),
            request.ProposedEndsOn ?? throw new DomainException("EndsOn is required for event requests."),
            request.ProposedLocationAr,
            request.ProposedLocationEn,
            request.ProposedOnlineMeetingUrl,
            featuredImageUrl,
            request.ProposedTopicId ?? throw new DomainException("TopicId is required for event requests."),
            _clock);

        _db.Add(ev);
    }
}
