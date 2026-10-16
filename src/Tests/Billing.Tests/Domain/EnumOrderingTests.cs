using FSH.Modules.Billing.Contracts;
using Shouldly;
using Xunit;

namespace Billing.Tests.Domain;

public sealed class EnumOrderingTests
{
    [Fact]
    public void InvoicePurpose_Should_HaveTopupAsTwo()
        => ((int)InvoicePurpose.Topup).ShouldBe(2);

    [Fact]
    public void TopupRequestStatus_Should_HavePendingAsDefaultZero()
        => ((int)TopupRequestStatus.Pending).ShouldBe(0);

    [Fact]
    public void WalletStatus_Should_HaveActiveAsDefaultZero()
        => ((int)WalletStatus.Active).ShouldBe(0);

    [Fact]
    public void WalletTransactionKind_Should_HaveTopupAsDefaultZero()
        => ((int)WalletTransactionKind.Topup).ShouldBe(0);
}
