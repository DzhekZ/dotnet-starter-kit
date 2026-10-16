using FSH.Modules.Billing.Features.v1.Wallets.CreateTopupRequest;
using FSH.Modules.Billing.Contracts.v1.Wallets;
using Shouldly;
using Xunit;

namespace Billing.Tests.Validators;

public sealed class CreateTopupRequestValidatorTests
{
    private readonly CreateTopupRequestCommandValidator _v = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_001)]
    public void Validate_Should_Fail_When_AmountIsOutOfRange(decimal amount)
        => _v.Validate(new CreateTopupRequestCommand(amount, null)).IsValid.ShouldBeFalse();

    [Fact]
    public void Validate_Should_Pass_When_AmountIsValid()
        => _v.Validate(new CreateTopupRequestCommand(50m, "need credit")).IsValid.ShouldBeTrue();
}
