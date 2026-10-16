namespace FSH.Modules.Multitenancy.Contracts.Dtos;

public sealed record TenantDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? ConnectionString { get; set; }
    public string AdminEmail { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime ValidUpto { get; set; }
    public string? Issuer { get; set; }

    /// <summary>
    /// A record's generated <c>ToString()</c> prints every property, and <see cref="ConnectionString"/> can carry a
    /// database password; only the id is printed.
    /// </summary>
    public override string ToString() => $"{nameof(TenantDto)} {{ Id = {Id} }}";
}