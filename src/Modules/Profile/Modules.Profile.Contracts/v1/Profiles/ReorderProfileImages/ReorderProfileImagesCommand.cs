using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles.ReorderProfileImages;

/// <summary>
/// Reorder the product's images. Ids in <paramref name="OrderedImageIds"/> are set to
/// SortOrder = 0, 1, 2... in the order supplied; any images not listed are appended after.
/// </summary>
public sealed record ReorderProfileImagesCommand(
    Guid Profiled,
    IReadOnlyList<Guid> OrderedImageIds) : ICommand<Unit>;
