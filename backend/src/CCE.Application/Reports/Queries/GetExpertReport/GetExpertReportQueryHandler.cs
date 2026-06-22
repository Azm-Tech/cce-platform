using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Reports.Queries.GetExpertReport;

internal sealed class GetExpertReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetExpertReportQuery, Response<List<ExpertReportDto>>>
{
    public async Task<Response<List<ExpertReportDto>>> Handle(
        GetExpertReportQuery q, CancellationToken ct)
    {
        var raw = await (
            from req in _db.ExpertRegistrationRequests
            join u in _db.Users on req.RequestedById equals u.Id
            from att in req.Attachments
                .Where(a => a.AttachmentType == ExpertRequestAttachmentType.Cv)
                .DefaultIfEmpty()
            join af in _db.AssetFiles on att.AssetFileId equals af.Id into afGroup
            from af in afGroup.DefaultIfEmpty()
            orderby req.SubmittedOn descending
            select new
            {
                req.Id,
                UserId = u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.JobTitle,
                u.OrganizationName,
                req.RequestedBioEn,
                req.RequestedBioAr,
                CvUrl = af.Url,
                CvMimeType = af.MimeType,
                req.RequestedTags,
                Status = (int)req.Status,
                req.SubmittedOn
            })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var result = raw.Select(x => new ExpertReportDto(
            x.Id,
            x.UserId,
            x.FirstName,
            x.LastName,
            x.Email,
            x.JobTitle,
            x.OrganizationName,
            x.RequestedBioEn,
            x.RequestedBioAr,
            x.CvUrl,
            DeriveFileFormat(x.CvMimeType),
            x.RequestedTags.ToList(),
            x.Status,
            x.SubmittedOn
        )).ToList();

        return _msg.Ok(result, "ITEMS_LISTED");
    }

    private static string DeriveFileFormat(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return string.Empty;

        return mimeType switch
        {
            "application/pdf" => "PDF",
            "application/msword" => "DOC",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "DOCX",
            "application/vnd.ms-excel" => "XLS",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "XLSX",
            "image/jpeg" => "JPEG",
            "image/png" => "PNG",
            _ => mimeType.ToUpperInvariant()
        };
    }
}
