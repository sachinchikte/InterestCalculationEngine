namespace InterestEngine.Core.Models;

/// <summary>
/// Aggregated report generated after a batch interest posting run.
/// </summary>
public class PostingSummaryReport
{
    public int PostingYear { get; set; }
    public DateTime GeneratedAt { get; set; }
    public decimal AnnualRatePercent { get; set; }
    public int TotalAccountsProcessed { get; set; }
    public int TotalPosted { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalProrated { get; set; }
    public decimal TotalInterestDistributed { get; set; }
    public decimal TotalClosingBalance { get; set; }
    public List<InterestPostingResult> Results { get; set; } = new();
}
