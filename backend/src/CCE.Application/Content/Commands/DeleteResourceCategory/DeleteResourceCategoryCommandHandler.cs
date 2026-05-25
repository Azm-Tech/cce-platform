using MediatR;

namespace CCE.Application.Content.Commands.DeleteResourceCategory;

public sealed class DeleteResourceCategoryCommandHandler : IRequestHandler<DeleteResourceCategoryCommand, Unit>
{
    private readonly IResourceCategoryRepository _service;

    public DeleteResourceCategoryCommandHandler(IResourceCategoryRepository service)
    {
        _service = service;
    }

    public async Task<Unit> Handle(DeleteResourceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"ResourceCategory {request.Id} not found.");
        }

        category.Deactivate();
        await _service.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
