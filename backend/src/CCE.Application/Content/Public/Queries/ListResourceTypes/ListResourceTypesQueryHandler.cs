using CCE.Application.Common;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListResourceTypes;

internal sealed class ListResourceTypesQueryHandler(MessageFactory _messages)
    : IRequestHandler<ListResourceTypesQuery, Response<List<ResourceTypeDto>>>
{
    public Task<Response<List<ResourceTypeDto>>> Handle(ListResourceTypesQuery request, CancellationToken cancellationToken)
    {
        var types = Enum.GetValues<ResourceType>()
            .Select(e => new ResourceTypeDto(
                (int)e,
                NameAr: GetArabicName(e),
                NameEn: e.ToString()))
            .ToList();

        return Task.FromResult(_messages.Ok(types, "ITEMS_LISTED"));
    }

    private static string GetArabicName(ResourceType type) => type switch
    {
        ResourceType.Paper          => "ورقة علمية",
        ResourceType.Article        => "مقال",
        ResourceType.Study          => "دراسة",
        ResourceType.Presentation   => "عرض تقديمي",
        ResourceType.ScientificPaper => "بحث علمي",
        ResourceType.Report         => "تقرير",
        ResourceType.Book           => "كتاب",
        ResourceType.Research       => "بحث",
        ResourceType.CceGuide       => "دليل الاقتصاد الدائري للكربون",
        ResourceType.Media          => "وسائط إعلامية",
        _ => type.ToString()
    };
}
