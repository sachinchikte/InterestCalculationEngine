namespace InterestEngine.Core.Models;

/// <summary>
/// Result of the interest calculation for a single member account.
/// </summary>
public class InterestPostingResult
{
    public int MemberId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal AnnualRatePercent { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal ClosingBalance { get; set; }
    public bool IsProrated { get; set; }

    /// <summary>
    /// Number of full months used if prorated (mid-year joiners).
    /// </summary>
    public int? MonthsEligible { get; set; }

    public string? SkipReason { get; set; }
    public bool IsSkipped => !string.IsNullOrEmpty(SkipReason);
    public DateTime PostingDate { get; set; }
}
