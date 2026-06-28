using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CCE.Application.Community.Public.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
[SuppressMessage("Design", "CA1008", Justification = "Nullable query param, sentinel value not needed")]
public enum TopicsSortBy
{
    Name = 1,
    PostsCount = 2,
    Newest = 3
}
