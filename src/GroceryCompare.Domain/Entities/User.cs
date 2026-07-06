namespace GroceryCompare.Domain.Entities;

public class User
{
    public int Id { get; set; }

    /// <summary>Stable Google subject ("sub") claim — the durable identity key.</summary>
    public required string GoogleSubjectId { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
