using FSH.Framework.Core.Domain;
using FSH.Modules.Profile.Domain;
using FSH.Modules.Profile.Domain.Events;
using System.Globalization;

namespace Profile.Tests.Domain;

public sealed class ProfileTests
{
    private static ProfileItem CreateValidProduct(int tabNumber = 1001, int countSubordinates = 0, int sexValue = 0)
        => ProfileItem.Create(
            name: "LastName FirstName MiddleName",
            personnelNumber: tabNumber,
            codePerson: tabNumber.ToString(CultureInfo.CurrentCulture),
            email: string.Empty,
            login: string.Empty,
            adSid: string.Empty,
            dateBirth: null,
            dateHire: null,
            dateDismiss: null,
            sex: sexValue,
            isBoss: false,
            typeEmployment: string.Empty,
            staffing: string.Empty,
            city: string.Empty,
            category: string.Empty,
            phoneMobile: string.Empty,
            phoneMobileAllowShow: false,
            phoneWork: string.Empty,
            division: string.Empty,
            place: string.Empty,
            wtHcmId: string.Empty,
            isDecret: false,
            isMobilization: false,
            subordinates: countSubordinates,
            information: string.Empty,
            description: " a description ",
            positionId: Guid.NewGuid(),
            subdivisionId: Guid.NewGuid(),
            hierarchyId: Guid.NewGuid());

    #region Create - Happy Path

    [Fact]
    public void Create_Should_NormalizeSkuToUpperAndTrim_When_SkuHasMixedCaseAndWhitespace()
    {
        // Arrange / Act
        ProfileItem product = ProfileItem.Create("  abc-123  ", "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0);

        // Assert
        product.Sku.ShouldBe("ABC-123");
    }

    [Fact]
    public void Create_Should_TrimNameAndGenerateSlug_When_NameHasWhitespaceAndSymbols()
    {
        // Arrange / Act
        ProfileItem product = ProfileItem.Create("sku", "  Hello World!!  ", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0);

        // Assert
        product.Name.ShouldBe("Hello World!!");
        product.Slug.ShouldBe("hello-world");
    }

    [Fact]
    public void Create_Should_TrimDescription_When_DescriptionProvided()
    {
        // Arrange / Act
        ProfileItem product = CreateValidProduct();

        // Assert
        product.Description.ShouldBe("a description");
    }

    [Fact]
    public void Create_Should_LeaveDescriptionNull_When_DescriptionIsNull()
    {
        // Arrange / Act
        ProfileItem product = ProfileItem.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0);

        // Assert
        product.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_BeActiveAndRaiseProductCreatedEvent_When_Valid()
    {
        // Arrange / Act
        ProfileItem product = CreateValidProduct();

        // Assert
        product.IsActive.ShouldBeTrue();
        product.Id.ShouldNotBe(Guid.Empty);
        IDomainEvent evt = product.DomainEvents.ShouldHaveSingleItem();
        ProfileCreatedDomainEvent created = evt.ShouldBeOfType<ProfileCreatedDomainEvent>();
        created.ProfileId.ShouldBe(product.Id);
        created.Sku.ShouldBe(product.Sku);
        created.Name.ShouldBe(product.Name);
    }

    #endregion

    #region Create - Guards

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_SkuIsBlank(string sku)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            ProfileItem.Create(sku, "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_SkuIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            ProfileItem.Create(null!, "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            ProfileItem.Create("sku", name, null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_PriceIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentNullException>(() =>
            ProfileItem.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), null!, 0));
    }

    [Fact]
    public void Create_Should_Throw_When_StockIsNegative()
    {
        // Act / Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ProfileItem.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), Money.Zero(), -1));
    }

    [Fact]
    public void Create_Should_Throw_When_PriceIsNegative()
    {
        // Act / Assert - shared Money allows signed amounts; the non-negative price invariant lives on the aggregate
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ProfileItem.Create("sku", "Name", null, Guid.NewGuid(), Guid.NewGuid(), new Money(-0.01m, "USD"), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_BrandIdIsEmpty()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            ProfileItem.Create("sku", "Name", null, Guid.Empty, Guid.NewGuid(), Money.Zero(), 0));
    }

    [Fact]
    public void Create_Should_Throw_When_CategoryIdIsEmpty()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            ProfileItem.Create("sku", "Name", null, Guid.NewGuid(), Guid.Empty, Money.Zero(), 0));
    }

    #endregion

    #region Update

    [Fact]
    public void Update_Should_MutateFieldsAndStampUpdatedAt_When_Valid()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        Guid newBrand = Guid.NewGuid();
        Guid newCategory = Guid.NewGuid();

        // Act
        product.Update("  New Name  ", "  desc  ", newBrand, newCategory, isActive: false);

        // Assert
        product.Name.ShouldBe("New Name");
        product.Slug.ShouldBe("new-name");
        product.Description.ShouldBe("desc");
        product.PositionId.ShouldBe(newBrand);
        product.SubdivisionId.ShouldBe(newCategory);
        product.IsActive.ShouldBeFalse();
        product.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Should_Throw_When_NameIsBlank(string name)
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            product.Update(name, null, Guid.NewGuid(), Guid.NewGuid(), true));
    }

    [Fact]
    public void Update_Should_Throw_When_BrandIdIsEmpty()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            product.Update("Name", null, Guid.Empty, Guid.NewGuid(), true));
    }

    [Fact]
    public void Update_Should_Throw_When_CategoryIdIsEmpty()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() =>
            product.Update("Name", null, Guid.NewGuid(), Guid.Empty, true));
    }

    #endregion

    #region ChangePrice

    [Fact]
    public void ChangePrice_Should_RaiseEventWithOldAndNewAmounts_When_PriceDiffers()
    {
        // Arrange
        ProfileItem product = CreateValidProduct(countSubordinates: 5);
        product.ClearDomainEvents();
        var newPrice = 100;

        // Act
        product.ChangeSubordinates(newPrice);

        // Assert
        product.Subordinates.ShouldBe(newPrice);
        ProfileSubordinatesChangedDomainEvent evt = product.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProfileSubordinatesChangedDomainEvent>();
        evt.OldAmount.ShouldBe(5);
        evt.NewAmount.ShouldBe(newPrice);
    }

    [Fact]
    public void ChangePrice_Should_BeNoOp_When_PriceEqualsCurrent()
    {
        // Arrange
        ProfileItem product = CreateValidProduct(countSubordinates: 10);
        product.ClearDomainEvents();

        // Act - equal value (records compare by value; currency normalized to upper)
        product.ChangeSubordinates(10);

        // Assert
        product.DomainEvents.ShouldBeEmpty();
        product.UpdatedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void ChangePrice_Should_Throw_When_NewPriceIsNull()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentNullException>(() => product.ChangeSubordinates(-10));
    }

    [Fact]
    public void ChangePrice_Should_Throw_When_NewPriceIsNegative()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentOutOfRangeException>(() => product.ChangeSubordinates(-10));
    }

    #endregion

    #region AdjustStock

    [Fact]
    public void AdjustStock_Should_IncreaseStockAndRaiseEvent_When_DeltaPositive()
    {
        // Arrange
        ProfileItem product = CreateValidProduct(sexValue: 10);
        product.ClearDomainEvents();

        // Act
        product.AdjustStock(5);

        // Assert
        product.Sex.ShouldBe(5);
        ProfileStockAdjustedDomainEvent evt = product.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBeOfType<ProfileStockAdjustedDomainEvent>();
        evt.OldStock.ShouldBe(10);
        evt.NewStock.ShouldBe(5);
        evt.Delta.ShouldBe(5);
    }

    [Fact]
    public void AdjustStock_Should_DecreaseStock_When_DeltaNegativeButResultNonNegative()
    {
        // Arrange
        ProfileItem product = CreateValidProduct(sexValue: 10);

        // Act
        product.AdjustStock(5);

        // Assert
        product.Sex.ShouldBe(5);
    }

    [Fact]
    public void AdjustStock_Should_Throw_When_ResultWouldBeNegative()
    {
        // Arrange
        ProfileItem product = CreateValidProduct(sexValue: 3);

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => product.AdjustStock(-4));
    }

    [Fact]
    public void AdjustStock_Should_NotMutateStock_When_AdjustmentThrows()
    {
        // Arrange
        ProfileItem product = CreateValidProduct(sexValue: 3);

        // Act
        Should.Throw<InvalidOperationException>(() => product.AdjustStock(-4));

        // Assert
        product.Sex.ShouldBe(3);
    }

    #endregion

    #region AddImage / Thumbnail invariants

    [Fact]
    public void AddImage_Should_MarkFirstImageAsThumbnailWithSortZero_When_NoImagesExist()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act
        ProfileImage image = product.AddImage(Guid.NewGuid(), "https://cdn/img1.png");

        // Assert
        image.IsThumbnail.ShouldBeTrue();
        image.SortOrder.ShouldBe(0);
        product.ThumbnailUrl.ShouldBe("https://cdn/img1.png");
        product.Images.Count.ShouldBe(1);
    }

    [Fact]
    public void AddImage_Should_NotMarkAsThumbnailAndIncrementSortOrder_When_ImagesAlreadyExist()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        product.AddImage(null, "https://cdn/img1.png");

        // Act
        ProfileImage second = product.AddImage(null, "https://cdn/img2.png");

        // Assert
        second.IsThumbnail.ShouldBeFalse();
        second.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void AddImage_Should_Throw_When_UrlIsBlank()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentException>(() => product.AddImage(null, "   "));
    }

    [Fact]
    public void ThumbnailUrl_Should_BeNull_When_NoImages()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Assert
        product.ThumbnailUrl.ShouldBeNull();
    }

    #endregion

    #region RemoveImage

    [Fact]
    public void RemoveImage_Should_PromoteLowestSortedImageToThumbnail_When_ThumbnailRemovedAndOthersRemain()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage first = product.AddImage(null, "https://cdn/1.png");  // thumbnail, sort 0
        ProfileImage second = product.AddImage(null, "https://cdn/2.png"); // sort 1
        ProfileImage third = product.AddImage(null, "https://cdn/3.png");  // sort 2

        // Act - remove the thumbnail
        product.RemoveImage(first.Id);

        // Assert - lowest sorted remaining (second, sort 1) is promoted
        second.IsThumbnail.ShouldBeTrue();
        third.IsThumbnail.ShouldBeFalse();
        product.ThumbnailUrl.ShouldBe("https://cdn/2.png");
    }

    [Fact]
    public void RemoveImage_Should_NotPromote_When_RemovedImageWasNotThumbnail()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage first = product.AddImage(null, "https://cdn/1.png");  // thumbnail
        ProfileImage second = product.AddImage(null, "https://cdn/2.png"); // non-thumbnail

        // Act
        product.RemoveImage(second.Id);

        // Assert
        first.IsThumbnail.ShouldBeTrue();
        product.Images.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveImage_Should_LeaveProductWithNoThumbnail_When_LastImageRemoved()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage only = product.AddImage(null, "https://cdn/1.png");

        // Act
        product.RemoveImage(only.Id);

        // Assert
        product.Images.ShouldBeEmpty();
        product.ThumbnailUrl.ShouldBeNull();
    }

    [Fact]
    public void RemoveImage_Should_Throw_When_ImageNotFound()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => product.RemoveImage(Guid.NewGuid()));
    }

    #endregion

    #region SetThumbnail

    [Fact]
    public void SetThumbnail_Should_MoveThumbnailFlagToTarget_When_TargetIsNotCurrentThumbnail()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage first = product.AddImage(null, "https://cdn/1.png");  // thumbnail
        ProfileImage second = product.AddImage(null, "https://cdn/2.png");

        // Act
        product.SetThumbnail(second.Id);

        // Assert - exactly one thumbnail, and it is the target
        first.IsThumbnail.ShouldBeFalse();
        second.IsThumbnail.ShouldBeTrue();
        product.Images.Count(i => i.IsThumbnail).ShouldBe(1);
    }

    [Fact]
    public void SetThumbnail_Should_BeNoOp_When_TargetIsAlreadyThumbnail()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage first = product.AddImage(null, "https://cdn/1.png");
        product.AddImage(null, "https://cdn/2.png");
        DateTime? before = product.UpdatedAtUtc;

        // Act
        product.SetThumbnail(first.Id);

        // Assert - unchanged timestamp signals the early-return branch
        product.UpdatedAtUtc.ShouldBe(before);
        first.IsThumbnail.ShouldBeTrue();
    }

    [Fact]
    public void SetThumbnail_Should_Throw_When_ImageNotFound()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        product.AddImage(null, "https://cdn/1.png");

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => product.SetThumbnail(Guid.NewGuid()));
    }

    #endregion

    #region ReorderImages

    [Fact]
    public void ReorderImages_Should_AssignSortOrderInSuppliedSequence_When_AllIdsProvided()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage a = product.AddImage(null, "https://cdn/a.png"); // sort 0
        ProfileImage b = product.AddImage(null, "https://cdn/b.png"); // sort 1
        ProfileImage c = product.AddImage(null, "https://cdn/c.png"); // sort 2

        // Act - reverse order
        product.ReorderImages([c.Id, b.Id, a.Id]);

        // Assert
        c.SortOrder.ShouldBe(0);
        b.SortOrder.ShouldBe(1);
        a.SortOrder.ShouldBe(2);
    }

    [Fact]
    public void ReorderImages_Should_AppendTrailingImages_When_SequenceIsPartial()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage a = product.AddImage(null, "https://cdn/a.png"); // sort 0
        ProfileImage b = product.AddImage(null, "https://cdn/b.png"); // sort 1
        ProfileImage c = product.AddImage(null, "https://cdn/c.png"); // sort 2

        // Act - only reorder c first; a and b are trailing, kept in existing sort order
        product.ReorderImages([c.Id]);

        // Assert
        c.SortOrder.ShouldBe(0);
        a.SortOrder.ShouldBe(1);
        b.SortOrder.ShouldBe(2);
    }

    [Fact]
    public void ReorderImages_Should_IgnoreUnknownIds_When_SequenceContainsIdsNotOnProduct()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();
        ProfileImage a = product.AddImage(null, "https://cdn/a.png");

        // Act - unknown id is skipped (continue branch), then trailing 'a' appended
        product.ReorderImages([Guid.NewGuid(), a.Id]);

        // Assert
        a.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void ReorderImages_Should_Throw_When_OrderedIdsIsNull()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act / Assert
        Should.Throw<ArgumentNullException>(() => product.ReorderImages(null!));
    }

    #endregion

    #region Restore (soft-delete)

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        // Arrange
        ProfileItem product = CreateValidProduct();

        // Act
        product.Restore();

        // Assert - early-return branch; never marked updated by restore
        product.IsDeleted.ShouldBeFalse();
        product.UpdatedAtUtc.ShouldBeNull();
    }

    #endregion
}
