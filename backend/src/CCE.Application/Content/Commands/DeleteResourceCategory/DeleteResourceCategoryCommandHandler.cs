using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteResourceCategory;

public sealed class DeleteResourceCategoryCommandHandler : IRequestHandler<DeleteResourceCategoryCommand, Response<VoidData>>
{
    private readonly IRepository<ResourceCategory, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public DeleteResourceCategoryCommandHandler(
        IRepository<ResourceCategory, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(DeleteResourceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (category is null)
            return _messages.CategoryNotFound<VoidData>();

        category.Deactivate();
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(MessageKeys.Content.CONTENT_DELETED);
    }
}
