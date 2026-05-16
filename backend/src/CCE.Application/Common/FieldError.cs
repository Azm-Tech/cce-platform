using CCE.Application.Localization;

namespace CCE.Application.Common;

public sealed record FieldError(
    string Field,
    string Code,
    LocalizedMessage Message);
