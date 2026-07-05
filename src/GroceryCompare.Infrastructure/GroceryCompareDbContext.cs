using Microsoft.EntityFrameworkCore;

namespace GroceryCompare.Infrastructure;

public class GroceryCompareDbContext(DbContextOptions<GroceryCompareDbContext> options)
    : DbContext(options)
{
    // Entities are added in PBI-010 (see BACKLOG.md); this starts empty so the
    // EF Core wiring can be verified end-to-end first.
}
