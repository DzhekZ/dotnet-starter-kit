using FSH.Framework.Core.Domain;
using FSH.Modules.Profile.Domain.Events;
using System.Collections.Immutable;

namespace FSH.Modules.Profile.Domain;

public sealed class ProfileItem : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string MiddleName { get; private set; } = default!;
    public int PersonnelNumber { get; private set; } = default!;  //tabelnyi number
    public string? CodePerson { get; private set; }
    public string? Email { get; private set; }
    public string? Login { get; private set; }
    public string? AdSid { get; private set; }
    public DateTime? DateBirth { get; private set; }
    public DateTime? DateHire { get; private set; }
    public DateTime? DateDismiss { get; private set; }
    public int Sex { get; private set; } = default!; //0 - not set, 1 - male, 2 - female, 3 - other
    public bool IsBoss { get; private set; } = default!;
    public string? TypeEmployment { get; private set; }
    public string? Staffing { get; private set; }
    public string? City { get; private set; }
    public string? Category { get; private set; }
    public string? PhoneMobile { get; private set; }
    public bool PhoneMobileAllowShow { get; private set; } = default!;
    public string? PhoneWork { get; private set; }
    public string? Division { get; private set; }
    public string? Place { get; private set; }
    public string? WtHcmId { get; private set; }
    public bool IsDecret { get; private set; } = default!;
    public bool IsMobilization { get; private set; } = default!;
    public int Subordinates { get; private set; } = default!;
    public string? Information { get; private set; }
    public string? Description { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid SubdivisionId { get; private set; }
    public Guid HierarchyId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; } = default!;


    // ISoftDeletable implementation
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    // EF populates this via the navigation property; aggregate methods mutate through the
    // private list so invariants (single thumbnail, contiguous SortOrder) hold.
    private readonly List<ProfileImage> _images = [];
    public IReadOnlyList<ProfileImage> Images => _images;

    /// <summary>The thumbnail (cover) image URL, or null when the product has no images.</summary>
    public string? ThumbnailUrl => _images.FirstOrDefault(i => i.IsThumbnail)?.Url;

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private ProfileItem() { } // EF Core

    public static ProfileItem Create(
        string name,
        int? personnelNumber,
        string? codePerson,
        string? email,
        string? login,
        string? adSid,
        DateTime? dateBirth,
        DateTime? dateHire,
        DateTime? dateDismiss,
        int? sex,
        bool? isBoss,
        string? typeEmployment,
        string? staffing,
        string? city,
        string? category,
        string? phoneMobile,
        bool? phoneMobileAllowShow,
        string? phoneWork,
        string? division,
        string? place,
        string? wtHcmId,
        bool? isDecret,
        bool? isMobilization,
        int? subordinates,
        string? information,
        string? description,
        Guid positionId,
        Guid subdivisionId,
        Guid hierarchyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (positionId == Guid.Empty)
        {
            throw new ArgumentException("PositionId is required.", nameof(positionId));
        }
        if (subdivisionId == Guid.Empty)
        {
            throw new ArgumentException("SubdivisionId is required.", nameof(subdivisionId));
        }
        if (hierarchyId == Guid.Empty)
        {
            throw new ArgumentException("HierarchyId is required.", nameof(hierarchyId));
        }
        if ((sex ?? 0) < 0)
        {
            sex = 0;
        }
        //split full name to parts
        var nameParts = SplitFullName(name.Trim());

        var profile = new ProfileItem
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Slug = Slugify(name + (personnelNumber ?? 0)),
            LastName = nameParts[0],
            FirstName = nameParts[1],
            MiddleName = nameParts[2],
            PersonnelNumber = personnelNumber ?? 0,
            CodePerson = codePerson?.Trim(),
            Email = email?.Trim(),
            Login = login?.Trim(),
            AdSid = adSid?.Trim(),
            DateBirth = dateBirth,
            DateHire = dateHire,
            DateDismiss = dateDismiss,
            Sex = sex ?? 0,
            IsBoss = isBoss ?? false,
            TypeEmployment = typeEmployment?.Trim(),
            Staffing = staffing?.Trim(),
            City = city?.Trim(),
            Category = category?.Trim(),
            PhoneMobile = phoneMobile?.Trim(),
            PhoneMobileAllowShow = phoneMobileAllowShow ?? false,
            PhoneWork = phoneWork?.Trim(),
            Division = division?.Trim(),
            Place = place?.Trim(),
            WtHcmId = wtHcmId?.Trim(),
            IsDecret = isDecret ?? false,
            IsMobilization = isMobilization ?? false,
            Subordinates = subordinates ?? 0,
            Information = information?.Trim(),
            Description = description?.Trim(),
            PositionId = positionId,
            SubdivisionId = subdivisionId,
            HierarchyId = hierarchyId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        profile.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ProfileCreatedDomainEvent(profile.Id, profile.Slug, profile.Name, id, ts)));

        return profile;
    }

    public void Update(
        string name,
        int? personnelNumber,
        string? codePerson,
        string? email,
        string? login,
        string? adSid,
        DateTime? dateBirth,
        DateTime? dateHire,
        DateTime? dateDismiss,
        int? sex,
        bool? isBoss,
        string? typeEmployment,
        string? staffing,
        string? city,
        string? category,
        string? phoneMobile,
        bool? phoneMobileAllowShow,
        string? phoneWork,
        string? division,
        string? place,
        string? wtHcmId,
        bool? isDecret,
        bool? isMobilization,
        int? subordinates,
        string? information,
        string? description,
        Guid positionId,
        Guid subdivisionId,
        Guid hierarchyId,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (positionId == Guid.Empty)
        {
            throw new ArgumentException("PositionId is required.", nameof(positionId));
        }
        if (subdivisionId == Guid.Empty)
        {
            throw new ArgumentException("SubdivisionId is required.", nameof(subdivisionId));
        }
        if (hierarchyId == Guid.Empty)
        {
            throw new ArgumentException("HierarchyId is required.", nameof(hierarchyId));
        }
        if ((sex ?? 0) < 0)
        {
            sex = 0;
        }
        //split full name to parts
        var nameParts = SplitFullName(name.Trim());

        Name = name.Trim();
        Slug = Slugify(name + (personnelNumber ?? 0));
        LastName = nameParts[0];
        FirstName = nameParts[1];
        MiddleName = nameParts[2];
        PersonnelNumber = personnelNumber ?? 0;
        CodePerson = codePerson?.Trim();
        Email = email?.Trim();
        Login = login?.Trim();
        AdSid = adSid?.Trim();
        DateBirth = dateBirth;
        DateHire = dateHire;
        DateDismiss = dateDismiss;
        Sex = sex ?? 0;
        IsBoss = isBoss ?? false;
        TypeEmployment = typeEmployment?.Trim();
        Staffing = staffing?.Trim();
        City = city?.Trim();
        Category = category?.Trim();
        PhoneMobile = phoneMobile?.Trim();
        PhoneMobileAllowShow = phoneMobileAllowShow ?? false;
        PhoneWork = phoneWork?.Trim();
        Division = division?.Trim();
        Place = place?.Trim();
        WtHcmId = wtHcmId?.Trim();
        IsDecret = isDecret ?? false;
        IsMobilization = isMobilization ?? false;
        Subordinates = subordinates ?? 0;
        Information = information?.Trim();
        Description = description?.Trim();
        PositionId = positionId;
        SubdivisionId = subdivisionId;
        HierarchyId = hierarchyId;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedOnUtc = TimeProvider.System.GetUtcNow();
        DeletedBy = deletedBy;
    }

    public void ChangeSubordinates(int newSubordinates)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newSubordinates, 0);
        if (newSubordinates == Subordinates)
        {
            return;
        }

        int oldAmount = Subordinates;
        Subordinates = newSubordinates;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ProfileSubordinatesChangedDomainEvent(Id, oldAmount, newSubordinates, id, ts)));
    }

    public void AdjustStock(int newValue)
    {
        ////int newStock = Stock + delta;
        ////if (newStock < 0)
        ////{
        ////    throw new InvalidOperationException(
        ////        $"Stock adjustment of {delta} would result in negative stock (current: {Stock}).");
        ////}

        int oldStock = Sex;
        Sex = newValue;
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ProfileStockAdjustedDomainEvent(Id, oldStock, newValue, newValue, id, ts)));
    }


    // ─── Image management ─────────────────────────────────────────────────

    /// <summary>
    /// Attach a new image. The first image attached is automatically the thumbnail; subsequent
    /// images come in non-thumbnail and the caller can promote one via <see cref="SetThumbnail"/>.
    /// </summary>
    public ProfileImage AddImage(Guid? fileAssetId, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        bool isFirst = _images.Count == 0;
        int order = isFirst ? 0 : _images.Max(i => i.SortOrder) + 1;
        var image = ProfileImage.Create(Id, fileAssetId, url, isThumbnail: isFirst, sortOrder: order);
        _images.Add(image);
        UpdatedAtUtc = DateTime.UtcNow;
        return image;
    }

    /// <summary>
    /// Remove an image. If the removed image was the thumbnail and other images remain, the
    /// lowest-sorted remaining image is promoted to thumbnail so the product always has a cover.
    /// </summary>
    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new InvalidOperationException($"Image {imageId} not found on product {Id}.");
        bool wasThumbnail = image.IsThumbnail;
        _images.Remove(image);

        if (wasThumbnail && _images.Count > 0)
        {
            var promoted = _images.OrderBy(i => i.SortOrder).First();
            promoted.MarkThumbnail(true);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Mark <paramref name="imageId"/> as the thumbnail; clears the flag on every other image.</summary>
    public void SetThumbnail(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new InvalidOperationException($"Image {imageId} not found on product {Id}.");
        if (image.IsThumbnail) return;

        foreach (var i in _images)
        {
            i.MarkThumbnail(i.Id == imageId);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Reorder images by the supplied id sequence. Ids not in <paramref name="orderedImageIds"/> are appended in their existing order after the rest.</summary>
    public void ReorderImages(IReadOnlyList<Guid> orderedImageIds)
    {
        ArgumentNullException.ThrowIfNull(orderedImageIds);
        int order = 0;
        var seen = new HashSet<Guid>();
        foreach (var id in orderedImageIds)
        {
            var image = _images.FirstOrDefault(i => i.Id == id);
            if (image is null) continue;
            image.SetSortOrder(order++);
            seen.Add(id);
        }
        foreach (var trailing in _images.Where(i => !seen.Contains(i.Id)).OrderBy(i => i.SortOrder).ToList())
        {
            trailing.SetSortOrder(order++);
        }
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

    private static ImmutableList<string> SplitFullName(string value)
    {
        List<string> resultParts = [];
        ReadOnlySpan<char> nameParts = value.AsSpan();
        foreach (var chunk in nameParts.Split(' '))
        {
            ReadOnlySpan<char> segment = nameParts[chunk];
            resultParts.Add(segment.ToString().Trim());
        }
        //must 3 parts. add if need
        var maxParts = 3;
        for (var i = 0; i < maxParts - resultParts.Count; i++)
        {
            resultParts.Add(string.Empty);
        }
        //must 3 parts. collapse to 3 segment
        if (resultParts.Count > 3)
        {
            for (var i = 3; i < resultParts.Count; i++)
            {
                resultParts[2] += " " + resultParts[i].Trim();
            }
        }

        return [.. resultParts];
    }
}
