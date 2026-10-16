using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.ChangePassword;

public sealed record ChangePasswordCommand : ICommand<string>
{
    /// <summary>The user's current password.</summary>
    public string Password { get; init; } = default!;

    /// <summary>The new password the user wants to set.</summary>
    public string NewPassword { get; init; } = default!;

    /// <summary>Confirmation of the new password.</summary>
    public string ConfirmNewPassword { get; init; } = default!;

    /// <summary>
    /// A record's generated <c>ToString()</c> prints every property, credentials included; keep it as inert
    /// as the class this replaced so a credential can never end up in a log line or an exception message.
    /// </summary>
    public override string ToString() => nameof(ChangePasswordCommand);
}