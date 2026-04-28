using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.GetUserById;

/// <summary>
/// Loads a single user by Id. Returns null when not found (endpoint maps null → 404).
/// </summary>
public sealed record GetUserByIdQuery(System.Guid Id) : IRequest<UserDetailDto?>;
