using FSH.Modules.Identity.Contracts.v1.Users.ChangePassword;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;
using FSH.Modules.Identity.Contracts.v1.Users.ResetPassword;

namespace Identity.Tests.Contracts;

/// <summary>
/// These commands are records, and a record's generated <c>ToString()</c> prints every property. They carry
/// passwords and a reset token, so each overrides it; a refactor that drops the override would let a credential
/// reach any log line or exception message that formats the command.
/// </summary>
public sealed class CredentialCommandsToStringTests
{
    private const string Secret = "Tr0ub4dor&3-secret";

    [Fact]
    public void ToString_Should_NotExposeCredentials_ForChangePasswordCommand()
    {
        var command = new ChangePasswordCommand
        {
            Password = Secret + "-current",
            NewPassword = Secret + "-new",
            ConfirmNewPassword = Secret + "-confirm",
        };

        command.ToString().ShouldNotContain(Secret);
    }

    [Fact]
    public void ToString_Should_NotExposeCredentials_ForRegisterUserCommand()
    {
        var command = new RegisterUserCommand
        {
            FirstName = "First",
            LastName = "Last",
            Email = "first.last@example.test",
            UserName = "first.last",
            Password = Secret + "-password",
            ConfirmPassword = Secret + "-confirm",
        };

        command.ToString().ShouldNotContain(Secret);
    }

    [Fact]
    public void ToString_Should_NotExposeCredentials_ForResetPasswordCommand()
    {
        var command = new ResetPasswordCommand
        {
            Email = "first.last@example.test",
            Password = Secret + "-password",
            Token = Secret + "-token",
        };

        command.ToString().ShouldNotContain(Secret);
    }
}
