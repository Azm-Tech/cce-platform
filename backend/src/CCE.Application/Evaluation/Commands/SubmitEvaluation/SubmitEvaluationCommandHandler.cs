using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using DomainEvaluation = CCE.Domain.Evaluation.ServiceEvaluation;
using MediatR;

namespace CCE.Application.Evaluation.Commands.SubmitEvaluation;

public sealed class SubmitEvaluationCommandHandler
    : IRequestHandler<SubmitEvaluationCommand, Response<VoidData>>
{
    private readonly IEvaluationRepository _repository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messageFactory;

    public SubmitEvaluationCommandHandler(
        IEvaluationRepository repository,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory messageFactory)
    {
        _repository = repository;
        _currentUser = currentUser;
        _clock = clock;
        _messageFactory = messageFactory;
    }

    public async Task<Response<VoidData>> Handle(
        SubmitEvaluationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var evaluation = DomainEvaluation.Submit(
            request.OverallSatisfaction,
            request.EaseOfUse,
            request.ContentSuitability,
            request.Feedback,
            userId,
            _clock);

        await _repository.AddAsync(evaluation, cancellationToken).ConfigureAwait(false);

        return _messageFactory.Ok(ApplicationErrors.Evaluation.EVALUATION_SUBMITTED);
    }
}
