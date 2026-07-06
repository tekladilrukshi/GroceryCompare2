namespace GroceryCompare.Domain.Entities;

/// <summary>Join entity: an item on a list with a quantity (composite key
/// ShoppingListId + GroceryItemId — an item appears on a list at most once).</summary>
public class ShoppingListItem
{
    public int ShoppingListId { get; set; }

    public ShoppingList? ShoppingList { get; set; }

    public int GroceryItemId { get; set; }

    public GroceryItem? GroceryItem { get; set; }

    public int Quantity { get; set; } = 1;
}
