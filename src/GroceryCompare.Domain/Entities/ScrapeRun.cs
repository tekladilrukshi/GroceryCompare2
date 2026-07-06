namespace GroceryCompare.Domain.Entities;

public enum ScrapeRunStatus
{
    Running,
    Succeeded,
    Failed,
    PartiallySucceeded,
}

/// <summary>Operational log for ETL jobs (store-directory + price sync),
/// so data-freshness problems are visible rather than silent.</summary>
public class ScrapeRun
{
    public int Id { get; set; }

    public int FranchiseId { get; set; }

    public Franchise? Franchise { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public ScrapeRunStatus Status { get; set; }

    public int ItemsUpdated { get; set; }

    public string? ErrorSummary { get; set; }
}
