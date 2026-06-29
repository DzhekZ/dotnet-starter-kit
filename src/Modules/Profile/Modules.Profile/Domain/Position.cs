using FSH.Framework.Core.Domain;

namespace FSH.Modules.Profile.Domain;

public sealed class Position : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // Soft-delete metadata, set by AuditableEntitySaveChangesInterceptor on dbContext.Remove(). A
    // BaseDbContext global query filter hides deleted rows; use IgnoreQueryFilters() for trash views.
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Reverses a soft delete. Idempotent: calling on a non-deleted brand is a no-op.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private Position() { }

    public static Position Create(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Position
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Slug = Slugify(name),
            Description = description?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Slug = Slugify(name);
        Description = description?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Slugify(string value)
    {
        var trimmed = value.Trim();
#pragma warning disable CA1308 // slug is canonical lowercase, not security-sensitive
        var lower = trimmed.ToLowerInvariant();
#pragma warning restore CA1308
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars).Trim('-');
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }
        return collapsed;
    }
}
