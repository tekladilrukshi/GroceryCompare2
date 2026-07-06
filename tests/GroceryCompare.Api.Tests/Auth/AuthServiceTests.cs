using GroceryCompare.Api.Auth;
using GroceryCompare.Domain.Entities;
using GroceryCompare.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GroceryCompare.Api.Tests.Auth;

public sealed class AuthServiceTests : IDisposable
{
    private static readonly GoogleTokenPayload SamplePayload =
        new("google-sub-123", "shopper@example.com", "Test Shopper");

    private readonly SqliteConnection _connection;
    private readonly GroceryCompareDbContext _db;
    private readonly TokenService _tokenService;

    public AuthServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new GroceryCompareDbContext(
            new DbContextOptionsBuilder<GroceryCompareDbContext>()
                .UseSqlite(_connection)
                .Options);
        _db.Database.EnsureCreated();

        _tokenService = new TokenService(
            Options.Create(new JwtOptions
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                SigningKey = new string('k', 32),
            }),
            TimeProvider.System);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private AuthService CreateService(GoogleTokenPayload? validatorResult) =>
        new(_db, new FakeGoogleTokenValidator(validatorResult), _tokenService, TimeProvider.System);

    [Fact]
    public async Task SignInWithGoogle_FirstSignIn_CreatesUserAndStoresHashedRefreshToken()
    {
        var result = await CreateService(SamplePayload).SignInWithGoogleAsync("valid-token");

        Assert.NotNull(result);
        var user = Assert.Single(_db.Users);
        Assert.Equal(SamplePayload.Subject, user.GoogleSubjectId);
        Assert.Equal(SamplePayload.Email, user.Email);

        var storedToken = Assert.Single(_db.RefreshTokens);
        Assert.NotEqual(result.RefreshToken, storedToken.TokenHash);
        Assert.Equal(_tokenService.HashRefreshToken(result.RefreshToken), storedToken.TokenHash);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
    }

    [Fact]
    public async Task SignInWithGoogle_ExistingUser_MatchesInsteadOfDuplicating()
    {
        _db.Users.Add(new User
        {
            GoogleSubjectId = SamplePayload.Subject,
            Email = "old@example.com",
            DisplayName = "Old Name",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        });
        await _db.SaveChangesAsync();

        var result = await CreateService(SamplePayload).SignInWithGoogleAsync("valid-token");

        Assert.NotNull(result);
        var user = Assert.Single(_db.Users);
        Assert.Equal(SamplePayload.Email, user.Email);
        Assert.Equal(SamplePayload.DisplayName, user.DisplayName);
    }

    [Fact]
    public async Task SignInWithGoogle_InvalidToken_ReturnsNullAndCreatesNothing()
    {
        var result = await CreateService(validatorResult: null).SignInWithGoogleAsync("bad-token");

        Assert.Null(result);
        Assert.Empty(_db.Users);
        Assert.Empty(_db.RefreshTokens);
    }

    private sealed class FakeGoogleTokenValidator(GoogleTokenPayload? result) : IGoogleTokenValidator
    {
        public Task<GoogleTokenPayload?> ValidateAsync(
            string idToken, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
