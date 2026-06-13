using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.SubscribeNewsletter;

public sealed record SubscribeNewsletterCommand(string Email, string Locale)
    : IRequest<Response<VoidData>>;
