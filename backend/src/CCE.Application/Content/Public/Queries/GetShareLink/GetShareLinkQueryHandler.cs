using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetShareLink;

public sealed class GetShareLinkQueryHandler
    : IRequestHandler<GetShareLinkQuery, Response<ShareLinkDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetShareLinkQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<ShareLinkDto>> Handle(
        GetShareLinkQuery request,
        CancellationToken cancellationToken)
    {
        var locale = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        var isAr = locale.Equals("ar", System.StringComparison.OrdinalIgnoreCase);

        ShareLinkDto? dto = request.Type switch
        {
            ShareContentType.News => await GetNewsAsync(request.Id, isAr, cancellationToken).ConfigureAwait(false),
            ShareContentType.Events => await GetEventAsync(request.Id, isAr, cancellationToken).ConfigureAwait(false),
            ShareContentType.Resources => await GetResourceAsync(request.Id, isAr, cancellationToken).ConfigureAwait(false),
            _ => null
        };

        if (dto is null)
            return _messages.NotFound<ShareLinkDto>(ApplicationErrors.General.NOT_FOUND);

        return _messages.Ok(dto, ApplicationErrors.General.SUCCESS_OPERATION);
    }

    private async Task<ShareLinkDto?> GetNewsAsync(
        System.Guid id, bool isAr, CancellationToken ct)
    {
        var list = await _db.News
            .Where(n => n.Id == id && n.PublishedOn != null)
            .Select(n => new { n.TitleAr, n.TitleEn, n.FeaturedImageUrl })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var item = list.SingleOrDefault();
        if (item is null) return null;

        return new ShareLinkDto(
            Link: $"news/{id}",
            Title: isAr ? item.TitleAr : item.TitleEn,
            ImageUrl: item.FeaturedImageUrl);
    }

    private async Task<ShareLinkDto?> GetEventAsync(
        System.Guid id, bool isAr, CancellationToken ct)
    {
        var list = await _db.Events
            .Where(e => e.Id == id)
            .Select(e => new { e.TitleAr, e.TitleEn, e.FeaturedImageUrl })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var item = list.SingleOrDefault();
        if (item is null) return null;

        return new ShareLinkDto(
            Link: $"events/{id}",
            Title: isAr ? item.TitleAr : item.TitleEn,
            ImageUrl: item.FeaturedImageUrl);
    }

    private async Task<ShareLinkDto?> GetResourceAsync(
        System.Guid id, bool isAr, CancellationToken ct)
    {
        var list = await _db.Resources
            .Where(r => r.Id == id && r.PublishedOn != null)
            .Select(r => new { r.TitleAr, r.TitleEn })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var item = list.SingleOrDefault();
        if (item is null) return null;

        return new ShareLinkDto(
            Link: $"resources/{id}",
            Title: isAr ? item.TitleAr : item.TitleEn,
            ImageUrl: null);
    }
}
