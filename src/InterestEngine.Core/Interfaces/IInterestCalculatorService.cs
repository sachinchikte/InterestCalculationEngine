using InterestEngine.Core.Models;

namespace InterestEngine.Core.Interfaces;

/// <summary>
/// Contract for the interest calculation engine.
/// Implementations must be deterministic — same inputs always produce same outputs.
/// </summary>
public interface IInterestCalculatorService
{
    /// <summary>
    /// Calculates interest for a single member account for the given posting year.
    /// Handles zero balances, inactive accounts, and mid-year joiners (proration).
    /// </summary>
    InterestPostingResult Calculate(MemberAccount account, InterestRate rate, int postingYear);

    /// <summary>
    /// Runs a batch calculation across all provided accounts and returns a full summary report.
    /// </summary>
    PostingSummaryReport RunBatchPosting(IEnumerable<MemberAccount> accounts, InterestRate rate, int postingYear);
}
