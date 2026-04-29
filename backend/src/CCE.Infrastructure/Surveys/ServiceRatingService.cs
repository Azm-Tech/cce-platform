using CCE.Application.Surveys;
using CCE.Domain.Surveys;
using CCE.Infrastructure.Persistence;

namespace CCE.Infrastructure.Surveys;

public sealed class ServiceRatingService : IServiceRatingService
{
    private readonly CceDbContext _db;

    public ServiceRatingService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(ServiceRating rating, CancellationToken ct)
    {
        _db.ServiceRatings.Add(rating);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
