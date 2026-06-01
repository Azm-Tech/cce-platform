using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteResourceCategory;

public sealed record DeleteResourceCategoryCommand(System.Guid Id) : IRequest<Response<VoidData>>;
