using Mediator;
using System.Text.Json.Serialization;

namespace FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;

public sealed record RegisterUserCommand : ICommand<RegisterUserResponse>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
    public string? PhoneNumber { get; set; }

    [JsonIgnore]
    public string? Origin { get; set; }

    /// <summary>
    /// A record's generated <c>ToString()</c> prints every property, credentials included; keep it as inert
    /// as the class this replaced so a credential can never end up in a log line or an exception message.
    /// </summary>
    public override string ToString() => nameof(RegisterUserCommand);
}