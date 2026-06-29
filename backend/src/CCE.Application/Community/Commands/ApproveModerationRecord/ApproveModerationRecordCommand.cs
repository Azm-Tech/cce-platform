using CCE.Application.Common;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.ApproveModerationRecord;

public sealed record ApproveModerationRecordCommand(System.Guid RecordId) : IRequest<Response<VoidData>>;
