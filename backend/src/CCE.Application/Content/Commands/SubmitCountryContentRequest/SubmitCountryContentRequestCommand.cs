using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.SubmitCountryContentRequest;

public sealed record SubmitCountryContentRequestCommand(
    System.Guid? CountryId,
    ContentBody Content) : IRequest<Response<System.Guid>>;
