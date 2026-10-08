using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.UserAccess.Domain.Users;

namespace DiceRoller.UserAccess.UnitTests.Domain;

public sealed class PersonNameTests
{
    [Fact]
    public void Create_PartsWithSurroundingSpaces_TrimsBoth()
    {
        var name = PersonName.Create("  Ada ", " Lovelace  ");

        name.FirstName.ShouldBe("Ada");
        name.LastName.ShouldBe("Lovelace");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingFirstName_ThrowsInvalidName(string? firstName)
    {
        var exception = Should.Throw<DomainException>(() => PersonName.Create(firstName, "Lovelace"));

        exception.Error.Code.ShouldBe(UserErrors.InvalidName.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingLastName_ThrowsInvalidName(string? lastName)
    {
        var exception = Should.Throw<DomainException>(() => PersonName.Create("Ada", lastName));

        exception.Error.Code.ShouldBe(UserErrors.InvalidName.Code);
    }

    [Fact]
    public void Create_PartsAtMaxLength_Succeeds()
    {
        var part = new string('a', PersonName.MaxLength);

        var name = PersonName.Create(part, part);

        name.FirstName.ShouldBe(part);
        name.LastName.ShouldBe(part);
    }

    [Fact]
    public void Create_FirstNameTooLong_ThrowsInvalidName()
    {
        var exception = Should.Throw<DomainException>(
            () => PersonName.Create(new string('a', PersonName.MaxLength + 1), "Lovelace"));

        exception.Error.Code.ShouldBe(UserErrors.InvalidName.Code);
    }

    [Fact]
    public void Create_LastNameTooLong_ThrowsInvalidName()
    {
        var exception = Should.Throw<DomainException>(
            () => PersonName.Create("Ada", new string('a', PersonName.MaxLength + 1)));

        exception.Error.Code.ShouldBe(UserErrors.InvalidName.Code);
    }
}
