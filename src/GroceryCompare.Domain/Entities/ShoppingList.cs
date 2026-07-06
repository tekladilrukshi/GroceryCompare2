namespace GroceryCompare.Domain.Entities;

public class ShoppingList
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ShoppingListItem> Items { get; set; } = [];
}
