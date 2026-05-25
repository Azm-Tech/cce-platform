using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateFaq;

public sealed class CreateFaqCommandHandler
    : IRequestHandler<CreateFaqCommand, Response<System.Guid>>
{
    private readonly IFaqRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public CreateFaqCommandHandler(
        IFaqRepository repo,
        ICceDbContext db,
        MessageFactory msg,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Response<System.Guid>> Handle(
        CreateFaqCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var question = LocalizedText.Create(request.QuestionAr, request.QuestionEn);
        var answer = LocalizedText.Create(request.AnswerAr, request.AnswerEn);

        var faq = Faq.Create(question, answer, request.Order, userId, _clock);
        _repo.Add(faq);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(faq.Id, "CONTENT_CREATED");
    }
}
