namespace InterestEngine.Core.Models;

/// <summary>
/// Annual interest rate declared by the pension board for a given financial year.
/// </summary>
public class InterestRate
{
    public int Year { get; set; }

    /// <summary>
    /// Annual rate expressed as a percentage, e.g. 8.15 means 8.15%.
    /// </summary>
    public decimal AnnualRatePercent { get; set; }
}
