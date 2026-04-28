using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.RejectCountryResourceRequest;

public sealed record RejectCountryResourceRequestCommand(
    System.Guid Id,
    string AdminNotesAr,
    string AdminNotesEn) : IRequest<CountryResourceRequestDto>;
