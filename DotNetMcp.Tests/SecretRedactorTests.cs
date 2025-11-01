using DotNetMcp;
using FluentAssertions;
using Xunit;

namespace DotNetMcp.Tests;

public class SecretRedactorTests
{
    [Fact]
    public void Redact_WithNullInput_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = SecretRedactor.Redact(input!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Redact_WithEmptyInput_ReturnsEmpty()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Redact_WithNoSensitiveData_ReturnsUnchanged()
    {
        // Arrange
        var input = "This is a normal string with no secrets";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("Server=localhost;Database=test;Password=secret123;", "Server=localhost;Database=test;Password=[REDACTED];")]
    [InlineData("Server=localhost;Database=test;pwd=mysecret;", "Server=localhost;Database=test;pwd=[REDACTED];")]
    [InlineData("Server=localhost;Database=test;passwd=p@ssw0rd;", "Server=localhost;Database=test;passwd=[REDACTED];")]
    [InlineData("Server=localhost;Database=test;pass=12345;", "Server=localhost;Database=test;pass=[REDACTED];")]
    public void Redact_WithConnectionStringPasswords_RedactsPassword(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Password=\"my secret\"", "Password=[REDACTED]")]
    [InlineData("password='another secret'", "password=[REDACTED]")]
    [InlineData("PWD=\"quoted\"", "PWD=[REDACTED]")]
    public void Redact_WithQuotedPasswords_RedactsPassword(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("mongodb://user:password123@localhost:27017/db", "mongodb://user:[REDACTED]@localhost:27017/db")]
    [InlineData("mongodb+srv://admin:secr3t@cluster.mongodb.net/db", "mongodb+srv://admin:[REDACTED]@cluster.mongodb.net/db")]
    public void Redact_WithMongoDbConnectionString_RedactsPassword(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://user:password@github.com/repo.git", "https://user:[REDACTED]@github.com/repo.git")]
    [InlineData("ftp://admin:secret123@ftp.example.com", "ftp://admin:[REDACTED]@ftp.example.com")]
    [InlineData("postgresql://dbuser:dbpass@localhost/mydb", "postgresql://dbuser:[REDACTED]@localhost/mydb")]
    public void Redact_WithCredentialsInUrls_RedactsPassword(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("api_key=1234567890abcdef1234567890abcdef", "api_key=[REDACTED]")]
    [InlineData("apiKey=abcdefghijklmnopqrstuvwxyz12345678", "apiKey=[REDACTED]")]
    [InlineData("API-KEY=ABCDEFGHIJKLMNOPQRSTUVWXYZ", "API-KEY=[REDACTED]")]
    [InlineData("access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", "access_token=[REDACTED]")]
    [InlineData("bearer_token=1a2b3c4d5e6f7g8h9i0j1a2b3c4d5e6f", "bearer_token=[REDACTED]")]
    [InlineData("client_secret=very_long_secret_key_12345678", "client_secret=[REDACTED]")]
    public void Redact_WithApiKeys_RedactsKey(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("aws_access_key_id=AKIAIOSFODNN7EXAMPLE", "aws_access_key_id=[REDACTED]")]
    [InlineData("AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", "AWS_SECRET_ACCESS_KEY=[REDACTED]")]
    public void Redact_WithAwsCredentials_RedactsCredentials(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("AccountKey=abcdef1234567890ABCDEF==", "AccountKey=[REDACTED]")]
    [InlineData("SharedAccessKey=base64encodedkey1234567890==", "SharedAccessKey=[REDACTED]")]
    public void Redact_WithAzureKeys_RedactsKey(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Redact_WithJwtToken_RedactsToken()
    {
        // Arrange
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var input = $"Authorization: Bearer {jwt}";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public void Redact_WithPrivateKey_RedactsKey()
    {
        // Arrange
        var privateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKj
MzEfYyjiWA4R4/M2bS1+fWIcPm15A4d9NHpCwmT6MQZ3oY0RXmL3KzUr0Y4HME6k
-----END PRIVATE KEY-----";
        var input = $"Private key content:\n{privateKey}";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKj");
    }

    [Theory]
    [InlineData("secret=my_super_long_secret_value_1234567890", "secret=[REDACTED]")]
    [InlineData("token=abcdefghijklmnopqrstuvwxyz1234567890", "token=[REDACTED]")]
    [InlineData("key=base64encodedvalue1234567890abcdef", "key=[REDACTED]")]
    [InlineData("password=verylongpassword1234567890abcdef", "password=[REDACTED]")]
    public void Redact_WithLabeledHighEntropyStrings_RedactsValue(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Redact_WithMultipleSecrets_RedactsAll()
    {
        // Arrange
        var input = "Server=localhost;Password=secret123;api_key=abcdef123456;token=xyz789;";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("Password=[REDACTED]");
        result.Should().Contain("api_key=[REDACTED]");
        result.Should().Contain("token=[REDACTED]");
        result.Should().NotContain("secret123");
        result.Should().NotContain("abcdef123456");
        result.Should().NotContain("xyz789");
    }

    [Fact]
    public void Redact_WithComplexConnectionString_RedactsOnlyPassword()
    {
        // Arrange
        var input = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("Server=myServerAddress");
        result.Should().Contain("Database=myDataBase");
        result.Should().Contain("User Id=myUsername");
        result.Should().Contain("Password=[REDACTED]");
        result.Should().NotContain("myPassword");
    }

    [Fact]
    public void Redact_WithRealWorldEfConnectionString_RedactsPassword()
    {
        // Arrange
        var input = @"info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (23ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'
      Connection string: Server=localhost;Database=MyApp;User ID=sa;Password=MyS3cr3tP@ssw0rd;";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("Password=[REDACTED]");
        result.Should().NotContain("MyS3cr3tP@ssw0rd");
        result.Should().Contain("Server=localhost");
        result.Should().Contain("User ID=sa");
    }

    [Theory]
    [InlineData("The word password appears in text", "The word password appears in text")]
    [InlineData("This is not a secret", "This is not a secret")]
    [InlineData("User logged in successfully", "User logged in successfully")]
    public void Redact_WithFalsePositives_DoesNotRedact(string input, string expected)
    {
        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Redact_WithEfMigrationOutput_RedactsConnectionStringIfPresent()
    {
        // Arrange
        var input = @"Build started...
Build succeeded.
info: Microsoft.EntityFrameworkCore.Infrastructure[10403]
      Entity Framework Core 9.0.0 initialized 'ApplicationDbContext' using provider 'Microsoft.EntityFrameworkCore.SqlServer:9.0.0' with options: None
Connection string: Server=(localdb)\mssqllocaldb;Database=MyApp;Password=Dev123!;Trusted_Connection=true;
Done.";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("Password=[REDACTED]");
        result.Should().NotContain("Dev123!");
    }

    [Fact]
    public void Redact_PerformanceTest_CompletesQuickly()
    {
        // Arrange - large output with no secrets
        var input = string.Join("\n", Enumerable.Repeat("Normal log line without secrets", 10000));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = SecretRedactor.Redact(input);
        sw.Stop();

        // Assert - should complete in reasonable time (well under 5% overhead for typical operations)
        sw.ElapsedMilliseconds.Should().BeLessThan(100, "redaction should be fast on large inputs");
        result.Should().Be(input);
    }

    [Fact]
    public void Redact_WithMixedSecrets_RedactsAllTypes()
    {
        // Arrange
        var input = @"
Deploying application with config:
- Database: Server=db.example.com;Database=prod;Password=SuperSecret123;
- API Endpoint: https://api:SecretKey456@api.example.com
- MongoDB: mongodb://admin:MongoPass789@cluster.example.com
- API Key: api_key=1234567890abcdefghijklmnopqrstuvwxyz
- AWS Access: aws_access_key_id=AKIAIOSFODNN7EXAMPLE
";

        // Act
        var result = SecretRedactor.Redact(input);

        // Assert
        result.Should().Contain("Password=[REDACTED]");
        result.Should().Contain("api:[REDACTED]@api.example.com");
        result.Should().Contain("admin:[REDACTED]@cluster.example.com");
        result.Should().Contain("api_key=[REDACTED]");
        result.Should().Contain("aws_access_key_id=[REDACTED]");
        result.Should().NotContain("SuperSecret123");
        result.Should().NotContain("SecretKey456");
        result.Should().NotContain("MongoPass789");
        result.Should().NotContain("1234567890abcdefghijklmnopqrstuvwxyz");
        result.Should().NotContain("AKIAIOSFODNN7EXAMPLE");
    }
}
