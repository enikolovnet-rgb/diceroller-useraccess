using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.UserAccess.Domain.Users;

namespace DiceRoller.UserAccess.UnitTests.Domain;

public sealed class EmailTests
{
    [Fact]
    public void Create_MixedCaseWithSurroundingSpaces_TrimsAndLowerCases()
    {
        var email = Email.Create("  John.Doe@Example.COM ");

        email.Value.ShouldBe("john.doe@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingValue_ThrowsInvalidEmail(string? value)
    {
        var exception = Should.Throw<DomainException>(() => Email.Create(value));

        exception.Error.Code.ShouldBe(UserErrors.InvalidEmail.Code);
    }

    [Theory]
    [InlineData("john.example.com")]
    [InlineData("john@example")]
    [InlineData("john doe@example.com")]
    [InlineData("john@@example.com")]
    [InlineData("john@ex@ample.com")]
    [InlineData("@example.com")]
    public void Create_MalformedAddress_ThrowsInvalidEmail(string value)
    {
        var exception = Should.Throw<DomainException>(() => Email.Create(value));

        exception.Error.Code.ShouldBe(UserErrors.InvalidEmail.Code);
    }

    [Fact]
    public void Create_MaxLength_Succeeds()
    {
        var value = AddressOfLength(Email.MaxLength);

        Email.Create(value).Value.ShouldBe(value);
    }

    [Fact]
    public void Create_LongerThanMaxLength_ThrowsInvalidEmail()
    {
        var exception = Should.Throw<DomainException>(() => Email.Create(AddressOfLength(Email.MaxLength + 1)));

        exception.Error.Code.ShouldBe(UserErrors.InvalidEmail.Code);
    }

    [Fact]
    public void Create_SameAddressDifferentCase_ProducesEqualValues()
    {
        Email.Create("John@Example.com").ShouldBe(Email.Create("john@example.com"));
    }

    private static string AddressOfLength(int length)
    {
        const string Domain = "@example.com";
        return new string('a', length - Domain.Length) + Domain;
    }
}
