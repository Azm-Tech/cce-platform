using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteHomepageSection;

public sealed record DeleteHomepageSectionCommand(System.Guid Id) : IRequest<Response<VoidData>>;
