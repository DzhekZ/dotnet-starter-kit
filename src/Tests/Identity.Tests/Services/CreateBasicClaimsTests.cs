using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Services;

namespace Identity.Tests.Services;

/// <summary>
/// The JWT carries the OIDC-standard `locale` claim only when the user explicitly chose a language;
/// an unset locale emits no claim so culture resolution can fall through to Accept-Language.
/// </summary>
public sealed class CreateBasicClaimsTests
{
    private static FshUser User(string? locale) =>
        new() { Id = "u1", Email = "u@codefi.com.br", UserName = "u", FirstName = "First", LastName = "Last", Locale = locale };

    [Fact]
    public void CreateBasicClaims_Should_EmitLocaleClaim_When_UserLocaleIsSet()
    {
        var claims = IdentityService.CreateBasicClaims(User("pt-BR"), "codefi");

        claims.Single(c => c.Type == "locale").Value.ShouldBe("pt-BR");
    }

    [Fact]
    public void CreateBasicClaims_Should_OmitLocaleClaim_When_UserLocaleIsNull()
    {
        var claims = IdentityService.CreateBasicClaims(User(null), "codefi");

        claims.Any(c => c.Type == "locale").ShouldBeFalse();
    }

    [Fact]
    public void CreateBasicClaims_Should_OmitLocaleClaim_When_UserLocaleIsWhitespace()
    {
        var claims = IdentityService.CreateBasicClaims(User("   "), "codefi");

        claims.Any(c => c.Type == "locale").ShouldBeFalse();
    }
}
