namespace CCE.Api.Internal.Endpoints;

public sealed record RescheduleEventRequest(
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn);
