using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.ApproveCountryResourceRequest;

public sealed record ApproveCountryResourceRequestCommand(
    System.Guid Id,
    string? AdminNotesAr,
    string? AdminNotesEn) : IRequest<CountryResourceRequestDto>;
