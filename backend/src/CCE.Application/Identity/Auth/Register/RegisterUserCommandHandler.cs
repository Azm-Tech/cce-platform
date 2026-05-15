using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using AppErrors = CCE.Application.Common.Errors;

namespace CCE.Application.Identity.Auth.Register;

internal sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Result<AuthUserDto>>
{
    private const string DefaultRole = "cce-user";
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly AppErrors _errors;

    public RegisterUserCommandHandler(UserManager<User> userManager, RoleManager<Role> roleManager, AppErrors errors)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _errors = errors;
    }

    public async Task<Result<AuthUserDto>> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(request.EmailAddress).ConfigureAwait(false);
        if (existing is not null)
        {
            return _errors.EmailExists();
        }

        var user = User.RegisterLocal(
            request.FirstName,
            request.LastName,
            request.EmailAddress,
            request.JobTitle,
            request.OrganizationName,
            request.PhoneNumber);

        var createResult = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            return _errors.RegistrationFailed(ToDetails(createResult));
        }

        if (!await _roleManager.RoleExistsAsync(DefaultRole).ConfigureAwait(false))
        {
            var roleResult = await _roleManager.CreateAsync(new Role(DefaultRole)).ConfigureAwait(false);
            if (!roleResult.Succeeded)
            {
                return _errors.RegistrationFailed(ToDetails(roleResult));
            }
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, DefaultRole).ConfigureAwait(false);
        if (!addRoleResult.Succeeded)
        {
            return _errors.RegistrationFailed(ToDetails(addRoleResult));
        }

        return new AuthUserDto(
            user.Id,
            user.Email ?? request.EmailAddress,
            user.FirstName,
            user.LastName,
            [DefaultRole]);
    }

    private static Dictionary<string, string[]> ToDetails(IdentityResult result)
        => new(StringComparer.Ordinal)
        {
            ["Identity"] = result.Errors.Select(e => e.Code).ToArray(),
        };
}
