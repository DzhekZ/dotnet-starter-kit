using FSH.Modules.Profile.Domain;

namespace Profile.Tests.Domain;

public sealed class PositionTests
{
    #region Create

    [Fact]
    public void Create_Should_TrimFieldsAndSlugify_When_Valid()
    {
        // Act
        Position brand = Position.Create("  Acme Corp.  ", "  desc  ");

        // Assert
        brand.Name.ShouldBe("Acme Corp.");
        brand.Slug.ShouldBe("acme-corp");
        brand.Description.ShouldBe("desc");
        brand.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_LeaveOptionalFieldsNull_When_NotProvided()
    {
        // Act
        Position brand = Position.Create("Acme", null);

        // Assert
        brand.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Position.Create(name, null));
    }

    [Fact]
    public void Create_Should_Throw_When_NameIsNull()
    {
        // Act / Assert
        Should.Throw<ArgumentException>(() => Position.Create(null!, null));
    }

    #endregion

    #region Update

    [Fact]
    public void Update_Should_MutateFieldsAndRestamp_When_Valid()
    {
        // Arrange
        Position brand = Position.Create("Acme", null);

        // Act
        brand.Update("  New Brand  ", "  new desc  ");

        // Assert
        brand.Name.ShouldBe("New Brand");
        brand.Slug.ShouldBe("new-brand");
        brand.Description.ShouldBe("new desc");
        brand.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Update_Should_ClearOptionalFields_When_NullsProvided()
    {
        // Arrange
        Position brand = Position.Create("Acme", "desc");

        // Act
        brand.Update("Acme", null);

        // Assert
        brand.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_Should_Throw_When_NameIsBlank(string name)
    {
        // Arrange
        Position brand = Position.Create("Acme", null);

        // Act / Assert
        Should.Throw<ArgumentException>(() => brand.Update(name, null));
    }

    #endregion

    #region Restore

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        // Arrange
        Position brand = Position.Create("Acme", null);

        // Act
        brand.Restore();

        // Assert
        brand.IsDeleted.ShouldBeFalse();
        brand.UpdatedAtUtc.ShouldBeNull();
    }

    #endregion
}
