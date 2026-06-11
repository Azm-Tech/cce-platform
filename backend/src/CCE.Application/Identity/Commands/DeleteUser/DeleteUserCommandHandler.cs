using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Public;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Response<UserDetailDto>>
{
    private readonly ICceDbContext _db;
    private readonly IUserProfileRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public DeleteUserCommandHandler(
        ICceDbContext db,
        IUserProfileRepository service,
        ICurrentUserAccessor currentUser,
        MessageFactory msg)
    {
        _db = db;
        _service = service;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<UserDetailDto>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            return _msg.UserNotFound<UserDetailDto>();
        }

        var deletedById = _currentUser.GetUserId()
            ?? throw new Domain.Common.DomainException("Cannot delete user without a user identity.");

        user.SoftDelete(deletedById, System.DateTimeOffset.UtcNow);

        _service.Update(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new UserDetailDto(
            user.Id,
            user.Email,
            user.UserName,
            user.LocalePreference,
            user.KnowledgeLevel,
            user.UserInterestTopics
                .Select(u => new InterestTopicDto(u.InterestTopicId, string.Empty, string.Empty, string.Empty, false))
                .ToList(),
            user.CountryId,
            user.CountryCodeId,
            user.AvatarUrl,
            System.Array.Empty<string>(),
            user.Status == Domain.Identity.UserStatus.Active), "USER_DELETED");
    }
}
