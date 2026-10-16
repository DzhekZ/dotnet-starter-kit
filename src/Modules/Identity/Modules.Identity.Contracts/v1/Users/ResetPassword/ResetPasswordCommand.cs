using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.ResetPassword;

public sealed record ResetPasswordCommand : ICommand<string>
{
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string Token { get; set; } = default!;

    /// <summary>
    /// A record's generated <c>ToString()</c> prints every property, credentials included; keep it as inert
    /// as the class this replaced so a credential can never end up in a log line or an exception message.
    /// </summary>
    public override string ToString() => nameof(ResetPasswordCommand);
}