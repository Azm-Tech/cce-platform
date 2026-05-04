using CCE.Api.Common.Auth;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

public sealed class EntraIdIssuerValidatorTests
{
    [Fact]
    public void Validate_AcceptsAnyTenantIssuer_WithCanonicalShape()
    {
        // Multi-tenant: any GUID-style tenant ID is valid as long as the prefix + suffix match.
        var issuer = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000001/v2.0";
        var result = EntraIdIssuerValidator.Validate(issuer);
        result.Should().Be(issuer);
    }

    [Fact]
    public void Validate_AcceptsCommonTenantIssuer()
    {
        // The 'common' endpoint is valid too (used during multi-tenant first-use flows).
        var issuer = "https://login.microsoftonline.com/common/v2.0";
        var result = EntraIdIssuerValidator.Validate(issuer);
        result.Should().Be(issuer);
    }

    [Fact]
    public void Validate_RejectsMalformedIssuer()
    {
        // Wrong host — not Entra ID at all.
        var act = () => EntraIdIssuerValidator.Validate("https://example.com/some-tenant/v2.0");
        act.Should().Throw<SecurityTokenInvalidIssuerException>()
            .WithMessage("*not a recognized Entra ID issuer*");
    }

    [Fact]
    public void Validate_RejectsV1Issuer_BecauseSuffixMismatch()
    {
        // Entra ID v1 uses /sts.windows.net/ + no /v2.0 suffix; we accept v2 only.
        var act = () => EntraIdIssuerValidator.Validate("https://login.microsoftonline.com/00000000-0000-0000-0000-000000000001/");
        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public void Validate_RejectsEmptyIssuer()
    {
        var act = () => EntraIdIssuerValidator.Validate(string.Empty);
        act.Should().Throw<SecurityTokenInvalidIssuerException>()
            .WithMessage("*null or empty*");
    }
}
