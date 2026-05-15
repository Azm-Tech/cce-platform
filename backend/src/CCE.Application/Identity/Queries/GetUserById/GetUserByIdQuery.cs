using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.GetUserById;

/// <summary>
/// Loads a single user by Id. Returns <see cref="Result{T}"/> so the endpoint
/// can map failure to a localized 404 automatically.
/// </summary>
public sealed record GetUserByIdQuery(System.Guid Id) : IRequest<Result<UserDetailDto>>;
