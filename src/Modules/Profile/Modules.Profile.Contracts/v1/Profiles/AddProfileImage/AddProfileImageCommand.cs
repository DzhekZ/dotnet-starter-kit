using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles.AddProfileImage;

/// <summary>
/// Attach an image to a profile. <paramref name="Url"/> is the durable public URL (typically the
/// <c>publicUrl</c> returned by the Files module after a presigned upload). <paramref name="FileAssetId"/>
/// is the corresponding FileAsset id for bookkeeping; null when attaching an external URL.
/// </summary>
public sealed record AddProfileImageCommand(
    Guid ProfileId,
    Guid? FileAssetId,
    string Url) : ICommand<ProfileImageDto>;
