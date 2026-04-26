namespace CCE.Domain.Common;

/// <summary>
/// Marks an entity class for automatic auditing by the <c>AuditingInterceptor</c>
/// (in <c>CCE.Infrastructure</c>). When the interceptor runs during
/// <c>SaveChangesAsync</c>, every Added/Modified/Deleted entity carrying this
/// attribute generates an <c>AuditEvent</c> row in the same transaction.
/// </summary>
/// <remarks>
/// Apply only to aggregate roots and entities whose state changes are
/// audit-worthy. High-volume association entities (PostRating, TopicFollow,
/// UserFollow, PostFollow, UserNotification, ServiceRating, SearchQueryLog,
/// CityScenarioResult, CountryKapsarcSnapshot) are intentionally NOT audited
/// to avoid inflating audit volume.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AuditedAttribute : Attribute { }
