using CCE.Application.Common;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.RejectModerationRecord;

public sealed record RejectModerationRecordCommand(System.Guid RecordId, string? Reason) : IRequest<Response<VoidData>>;
