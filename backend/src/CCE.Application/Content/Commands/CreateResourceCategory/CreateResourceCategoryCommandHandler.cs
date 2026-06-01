using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListResourceCategories;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateResourceCategory;

public sealed class CreateResourceCategoryCommandHandler : IRequestHandler<CreateResourceCategoryCommand, Response<ResourceCategoryDto>>
{
    private readonly IRepository<ResourceCategory, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public CreateResourceCategoryCommandHandler(
        IRepository<ResourceCategory, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<ResourceCategoryDto>> Handle(CreateResourceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = ResourceCategory.Create(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.ParentId,
            request.OrderIndex);

        await _repo.AddAsync(category, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(ListResourceCategoriesQueryHandler.MapToDto(category), "CONTENT_CREATED");
    }
}
