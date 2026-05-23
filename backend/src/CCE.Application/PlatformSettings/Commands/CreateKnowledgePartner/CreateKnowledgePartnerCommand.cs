using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateKnowledgePartner;

public sealed record CreateKnowledgePartnerCommand(
    string NameAr,
    string NameEn,
    string? LogoUrl,
    string? WebsiteUrl,
    string? DescriptionAr,
    string? DescriptionEn) : IRequest<Response<System.Guid>>;
