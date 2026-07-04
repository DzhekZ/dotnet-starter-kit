using FSH.Modules.Profile.Domain;

namespace Profile.Tests.Domain;

public sealed class SubdivisionTests
{
    #region Create

    [Fact]
    public void Create_Should_TrimNameSlugifyAndKeepParent_When_Valid()
    {
        // Arrange
        Guid parent = Guid.NewGuid();

        // Act
        Subdivision category = Subdivision.Create("  Power Tools & More  ", "  desc  ", "  some type  ", parent);

        // Assert
        category.Name.ShouldBe("Power Tools & More");
        category.Slug.ShouldBe("power-tools-more");
        category.Description.ShouldBe("desc");
        category.TypeSubdivision.ShouldBe("some type");
        category.ParentSubdivisionId.ShouldBe(parent);
        category.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_LeaveDescriptionAndParentNull_When_NotProvided()
    {
        // Act
        Subdivision category = Subdivision.Create("Root", null, "", null);

        // Assert
        category.Description.ShouldBeNull();
        category.ParentSubdivisionId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Subdivision.Create(name, null, "", null));
    }

    [Fact]
    public void Create_Should_Throw_When_NameIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Subdivision.Create(null!, null, "", null));
    }

    #endregion

    #region Update - cycle / parent guard

    [Fact]
    public void Update_Should_Throw_When_ParentEqualsOwnId()
    {
        // Arrange
        Subdivision category = Subdivision.Create("Cat", null, "", null);

        // Act / Assert - a category cannot be its own parent
        Should.Throw<InvalidOperationException>(() =>
            category.Update("Cat", null, "", category.Id));
    }

    [Fact]
    public void Update_Should_ReparentAndRestamp_When_ParentIsDifferentId()
    {
        // Arrange
        Subdivision category = Subdivision.Create("Cat", null, " some type ", null);
        Guid newParent = Guid.NewGuid();

        // Act
        category.Update("  Renamed  ", "  new  ", " some type ", newParent);

        // Assert
        category.Name.ShouldBe("Renamed");
        category.Slug.ShouldBe("renamed");
        category.Description.ShouldBe("new");
        category.ParentSubdivisionId.ShouldBe(newParent);
        category.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Update_Should_AllowNullParent_When_PromotingToRoot()
    {
        // Arrange
        Subdivision category = Subdivision.Create("Cat", null, "", Guid.NewGuid());

        // Act
        category.Update("Cat", null, "", null);

        // Assert
        category.ParentSubdivisionId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Should_Throw_When_NameIsBlank(string name)
    {
        // Arrange
        Subdivision category = Subdivision.Create("Cat", null, "", null);

        // Act / Assert
        Should.Throw<ArgumentException>(() => category.Update(name, null, "", null));
    }

    #endregion

    #region Restore

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        // Arrange
        Subdivision category = Subdivision.Create("Cat", null, "", null);

        // Act
        category.Restore();

        // Assert
        category.IsDeleted.ShouldBeFalse();
        category.UpdatedAtUtc.ShouldBeNull();
    }

    #endregion
}
