using CCE.Domain.Surveys;

namespace CCE.Application.Surveys;

public interface IServiceRatingService
{
    Task SaveAsync(ServiceRating rating, CancellationToken ct);
}
