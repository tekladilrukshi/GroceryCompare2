namespace GroceryCompare.Domain.Entities;

/// <summary>Join entity: the user's personally selected set of stores.</summary>
public class UserStoreSelection
{
    public int UserId { get; set; }

    public User? User { get; set; }

    public int StoreId { get; set; }

    public Store? Store { get; set; }

    public DateTime CreatedAt { get; set; }
}
