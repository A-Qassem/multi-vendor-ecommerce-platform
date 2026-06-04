using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MultiVendor.Ecommerce.Application.DTOs.Auth;
using MultiVendor.Ecommerce.Application.Interfaces.Auth;
using MultiVendor.Ecommerce.Domain.Entities;
using MultiVendor.Ecommerce.Infrastructure.Data;
using MultiVendor.Ecommerce.Infrastructure.Services.Auth;

namespace MultiVendor.Ecommerce.Application.Tests.Auth;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly AppDbContext _context;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Merchant>()))
            .Returns(("access_token", DateTime.UtcNow.AddMinutes(15)));

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token_value");

        _sut = new AuthService(_context, _tokenServiceMock.Object);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task RegisterAsync_ShouldCreateMerchant_AndReturnTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name     = "Test User",
            Email    = "test@example.com",
            Password = "Secret123!"
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token_value");
        result.MerchantId.Should().NotBeEmpty();

        var saved = await _context.Merchants.FirstOrDefaultAsync(m => m.Email == request.Email);
        Assert.NotNull(saved);
        BCrypt.Net.BCrypt.Verify(request.Password, saved!.Password).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenEmailAlreadyExists()
    {
        // Arrange
        _context.Merchants.Add(new Merchant
        {
            Name     = "Existing",
            Email    = "dupe@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("pass")
        });
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Name     = "Another",
            Email    = "dupe@example.com",
            Password = "pass"
        };

        // Act
        var act = async () => await _sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        const string password = "MyPassword!";
        _context.Merchants.Add(new Merchant
        {
            Name     = "Valid User",
            Email    = "valid@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword(password)
        });
        await _context.SaveChangesAsync();

        var request = new LoginRequest { Email = "valid@example.com", Password = password };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token_value");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsWrong()
    {
        // Arrange
        _context.Merchants.Add(new Merchant
        {
            Name     = "User",
            Email    = "user@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correct")
        });
        await _context.SaveChangesAsync();

        var request = new LoginRequest { Email = "user@example.com", Password = "wrong" };

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnNewTokens_AndRevokeOldToken()
    {
        // Arrange
        var merchant = new Merchant
        {
            Name     = "Merchant",
            Email    = "merchant@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("pass")
        };
        _context.Merchants.Add(merchant);

        var oldToken = new RefreshToken
        {
            MerchantId = merchant.Id,
            Token      = "old_refresh_token",
            ExpiresAt  = DateTime.UtcNow.AddDays(7),
            IsRevoked  = false
        };
        _context.RefreshTokens.Add(oldToken);
        await _context.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "old_refresh_token" };

        // Act
        var result = await _sut.RefreshAsync(request);

        // Assert
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token_value");

        var revoked = await _context.RefreshTokens.FindAsync(oldToken.Id);
        revoked!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenTokenIsRevoked()
    {
        // Arrange
        var merchant = new Merchant
        {
            Name     = "M",
            Email    = "m@example.com",
            Password = "x"
        };
        _context.Merchants.Add(merchant);

        _context.RefreshTokens.Add(new RefreshToken
        {
            MerchantId = merchant.Id,
            Token      = "revoked_token",
            ExpiresAt  = DateTime.UtcNow.AddDays(7),
            IsRevoked  = true
        });
        await _context.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "revoked_token" };

        // Act
        var act = async () => await _sut.RefreshAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
