using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.CreateStateRepAssignment;

public sealed record CreateStateRepAssignmentCommand(
    System.Guid UserId,
    System.Guid CountryId) : IRequest<Response<StateRepAssignmentDto>>;
