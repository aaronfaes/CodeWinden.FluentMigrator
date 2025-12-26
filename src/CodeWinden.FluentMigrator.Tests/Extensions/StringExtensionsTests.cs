using CodeWinden.FluentMigrator.SqlServer;
using Xunit;

namespace CodeWinden.FluentMigrator.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("Server=localhost;Database=TestDb;User Id=sa;Password=SecretPass123;", "SecretPass123")]
    [InlineData("Server=localhost;Database=TestDb;User Id=sa;PASSWORD=SecretPass123;", "SecretPass123")]
    [InlineData("Server=localhost;Database=TestDb;User Id=sa;Password=P@ss!w0rd#123;", "P@ss!w0rd#123")]
    [InlineData("Server = localhost; Database = TestDb; User Id = sa; Password = SecretPass;", "SecretPass")]
    [InlineData("Server=localhost;Database=TestDb;User Id=sa;Password=;", "")]
    [InlineData("Server=myServer;Database=myDb;User Id=myUser;Password=myPass;Encrypt=true;", "myPass")]
    public void RemoveConnectionStringSecrets_WithPassword_RemovesPassword(string connectionString, string password)
    {
        // Act
        var result = connectionString.RemoveConnectionStringSecrets();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("password", result, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(password))
        {
            Assert.DoesNotContain(password, result);
        }
    }

    [Fact]
    public void RemoveConnectionStringSecrets_WithIntegratedSecurity_PreservesAllProperties()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=TestDb;Integrated Security=true;";

        // Act
        var result = connectionString.RemoveConnectionStringSecrets();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("localhost", result);
        Assert.Contains("TestDb", result);
        Assert.Contains("Integrated Security", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveConnectionStringSecrets_WithEmptyConnectionString_ReturnsEmptyString()
    {
        // Arrange
        var connectionString = string.Empty;

        // Act
        var result = connectionString.RemoveConnectionStringSecrets();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }
}
