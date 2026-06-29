using FSH.Framework.Core.Domain;

namespace FSH.Modules.Profile.Domain;

public sealed class Hierarchy : AggregateRoot<Guid>, IAuditableEntity
{
    public int? Version { get; private set; }
    public int? TreeId { get; private set; }
    public int? ParentTreeId { get; private set; }
    public string Parents { get; private set; } = default!;
    public string ParentsInv { get; private set; } = default!;
    public int? Level { get; private set; }
    public int? PersonCode { get; private set; }

    // IAuditableEntity implementation
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }


    private Hierarchy() { }

    public static Hierarchy Create(int? version, int? treeId, int? parentTreeId, string? parents, string? parentsInv, int? level, int? personCode, string? createdBy = null)
    {
        if (IsOwnParent(treeId, parentTreeId))
        {
            throw new InvalidOperationException("A hierarchy cannot be its own parent.");
        }

        return new Hierarchy
        {
            Id = Guid.CreateVersion7(),
            Version = version,
            TreeId = treeId,
            ParentTreeId = parentTreeId,
            Parents = parents?.Trim() ?? string.Empty,
            ParentsInv = parentsInv?.Trim() ?? string.Empty,
            Level = level,
            PersonCode = personCode,
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy
        };
    }

    public void Update(int? version, int? treeId, int? parentTreeId, string? parents, string? parentsInv, int? level, int? personCode, string? modifiedBy = null)
    {
        if (IsOwnParent(treeId, parentTreeId))
        {
            throw new InvalidOperationException("A hierarchy cannot be its own parent.");
        }

        Version = version;
        TreeId = treeId;
        ParentTreeId = parentTreeId;
        Parents = parents?.Trim() ?? string.Empty;
        ParentsInv = parentsInv?.Trim() ?? string.Empty;
        Level = level;
        PersonCode = personCode;
        LastModifiedOnUtc = TimeProvider.System.GetUtcNow();
        LastModifiedBy = modifiedBy;
    }

    private static bool IsOwnParent(int? treeId, int? parentTreeId)
    {
        if (parentTreeId == treeId && (treeId != null || parentTreeId != null))
            return true;

        return false;
    }
}
