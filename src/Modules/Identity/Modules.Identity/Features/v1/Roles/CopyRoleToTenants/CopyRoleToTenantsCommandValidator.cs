using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Roles.CopyRoleToTenants;

namespace FSH.Modules.Identity.Features.v1.Roles.CopyRoleToTenants;

public sealed class CopyRoleToTenantsCommandValidator : AbstractValidator<CopyRoleToTenantsCommand>
{
    public CopyRoleToTenantsCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}
