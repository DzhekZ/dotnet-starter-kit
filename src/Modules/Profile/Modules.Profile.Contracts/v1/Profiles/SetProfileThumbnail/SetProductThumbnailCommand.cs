using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles.SetProfileThumbnail;

/// <summary>Promote an existing image to thumbnail (cover). Clears the flag on every other image.</summary>
public sealed record SetProductThumbnailCommand(Guid ProductId, Guid ImageId) : ICommand<Unit>;
