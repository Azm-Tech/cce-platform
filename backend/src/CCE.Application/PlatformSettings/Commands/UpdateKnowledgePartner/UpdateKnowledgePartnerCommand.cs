using CCE.Application.Common;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateKnowledgePartner;

public sealed record UpdateKnowledgePartnerCommand(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string? LogoUrl,
    string? WebsiteUrl,
    string? DescriptionAr,
    string? DescriptionEn) : IRequest<Response<KnowledgePartnerDto>>;
