using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace Multitenancy.Tests;

/// <summary>
/// <see cref="TenantDto"/> is a record, and a record's generated <c>ToString()</c> prints every property —
/// including the dedicated-database connection string, which carries a password.
/// </summary>
public sealed class TenantDtoToStringTests
{
    [Fact]
    public void ToString_Should_PrintTheId_And_NotTheConnectionString()
    {
        var dto = new TenantDto
        {
            Id = "acme",
            Name = "Acme",
            ConnectionString = "Host=db;Database=acme;Username=acme;Password=Tr0ub4dor&3-secret",
            AdminEmail = "admin@acme.test",
        };

        var text = dto.ToString();

        text.ShouldContain("acme");
        text.ShouldNotContain("Tr0ub4dor&3-secret");
        text.ShouldNotContain("Host=db");
    }
}
