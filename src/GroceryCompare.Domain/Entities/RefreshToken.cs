namespace GroceryCompare.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    /// <summary>SHA-256 hash of the raw token; the raw value is never stored.</summary>
    public required string TokenHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;
}
