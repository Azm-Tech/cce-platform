using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.PlatformSettings.Commands.UpdateFaq;

public sealed class UpdateFaqCommandHandler
    : IRequestHandler<UpdateFaqCommand, Response<System.Guid>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public UpdateFaqCommandHandler(
        ICceDbContext db,
        MessageFactory msg,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _db = db;
        _msg = msg;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Response<System.Guid>> Handle(
        UpdateFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (faq is null)
            return _msg.FaqNotFound<System.Guid>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var question = LocalizedText.Create(request.QuestionAr, request.QuestionEn);
        var answer = LocalizedText.Create(request.AnswerAr, request.AnswerEn);

        faq.UpdateContent(question, answer, request.Order, userId, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(faq.Id, "CONTENT_UPDATED");
    }
}
