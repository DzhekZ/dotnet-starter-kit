using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.RestoreFile;

public sealed class RestoreFileCommandHandler(FilesDbContext db)
    : ICommandHandler<RestoreFileCommand, Unit>
{
    public async ValueTask<Unit> Handle(RestoreFileCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        // Disable only the SoftDelete filter (it would otherwise hide the row); tenant scoping stays in
        // force, so a file deleted in another tenant is invisible here and cannot be restored.
        var f = await db.FileAssets
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(x => x.Id == cmd.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        if (!f.IsDeleted)
        {
            return Unit.Value; // idempotent — already live
        }

        f.Restore();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
