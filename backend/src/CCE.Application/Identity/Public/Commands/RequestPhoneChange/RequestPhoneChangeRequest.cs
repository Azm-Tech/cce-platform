namespace CCE.Application.Identity.Public.Commands.RequestPhoneChange;

public sealed record RequestPhoneChangeRequest(string NewPhone, System.Guid? CountryId);
