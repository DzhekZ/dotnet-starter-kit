using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.ToggleUserStatus;

public sealed record ToggleUserStatusCommand : ICommand<Unit>
{
    public bool ActivateUser { get; set; }
    public string? UserId { get; set; }
}