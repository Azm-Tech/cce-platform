using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.InterestManagement.Queries.GetInterestQuestions;

public sealed class GetInterestQuestionsQueryHandler
    : IRequestHandler<GetInterestQuestionsQuery, Response<IReadOnlyList<InterestCategoryInfoDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetInterestQuestionsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<InterestCategoryInfoDto>>> Handle(
        GetInterestQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var allTopics = await _db.InterestTopics
            .OrderBy(t => t.NameEn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var questions = new List<InterestCategoryInfoDto>
        {
            new(
                "carbon_area",
                "منطقة الكربون",
                "Carbon Area",
                "multiple",
                allTopics.Where(t => t.Category == "carbon_area")
                    .Select(t => new InterestTopicDto(t.Id, t.NameAr, t.NameEn, t.Category, t.IsActive))
                    .ToList()),
            new(
                "knowledge_assessment",
                "تقييم المعرفة",
                "Knowledge Assessment",
                "single",
                allTopics.Where(t => t.Category == "knowledge_assessment")
                    .Select(t => new InterestTopicDto(t.Id, t.NameAr, t.NameEn, t.Category, t.IsActive))
                    .ToList()),
            new(
                "job_sector",
                "القطاع الوظيفي",
                "Job Sector",
                "single",
                allTopics.Where(t => t.Category == "job_sector")
                    .Select(t => new InterestTopicDto(t.Id, t.NameAr, t.NameEn, t.Category, t.IsActive))
                    .ToList()),
        };

        return _msg.Ok<IReadOnlyList<InterestCategoryInfoDto>>(questions, MessageKeys.General.SUCCESS_OPERATION);
    }
}