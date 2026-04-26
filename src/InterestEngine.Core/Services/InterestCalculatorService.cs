using InterestEngine.Core.Interfaces;
using InterestEngine.Core.Models;

namespace InterestEngine.Core.Services;

/// <summary>
/// Core interest calculation engine for pension fund accounts.
///
/// Business rules implemented:
///   1. Inactive accounts are skipped — no interest posted.
///   2. Zero or negative balance accounts are skipped.
///   3. Members who joined mid-year receive prorated interest based on full months eligible.
///   4. All calculations use decimal arithmetic to avoid floating-point rounding errors
///      on financial figures — critical for audit-accuracy in production pension systems.
///   5. Interest is simple annual interest: Balance × (Rate / 100).
///      Proration: Balance × (Rate / 100) × (MonthsEligible / 12).
/// </summary>
public class InterestCalculatorService : IInterestCalculatorService
{
    private const int MonthsInYear = 12;

    public InterestPostingResult Calculate(MemberAccount account, InterestRate rate, int postingYear)
    {
        var result = new InterestPostingResult
        {
            MemberId        = account.MemberId,
            AccountNumber   = account.AccountNumber,
            FullName        = account.FullName,
            OpeningBalance  = account.Balance,
            AnnualRatePercent = rate.AnnualRatePercent,
            PostingDate     = new DateTime(postingYear, 12, 31)
        };

        // Rule 1 — skip inactive accounts
        if (!account.IsActive)
        {
            result.SkipReason = "Account is inactive.";
            return result;
        }

        // Rule 2 — skip zero or negative balance
        if (account.Balance <= 0)
        {
            result.SkipReason = "Balance is zero or negative — no interest applicable.";
            return result;
        }

        // Rule 3 — proration for mid-year joiners
        bool joinedThisYear = account.DateOfJoining.Year == postingYear;

        if (joinedThisYear)
        {
            int monthsEligible = MonthsInYear - account.DateOfJoining.Month + 1;

            // Member joined in December — only 1 month; below threshold, skip posting
            if (monthsEligible <= 0)
            {
                result.SkipReason = "Joined in final month of year — insufficient eligible period.";
                return result;
            }

            decimal proratedInterest = account.Balance
                * (rate.AnnualRatePercent / 100m)
                * ((decimal)monthsEligible / MonthsInYear);

            result.InterestAmount  = Math.Round(proratedInterest, 2, MidpointRounding.AwayFromZero);
            result.IsProrated      = true;
            result.MonthsEligible  = monthsEligible;
        }
        else
        {
            // Full year interest
            decimal fullInterest = account.Balance * (rate.AnnualRatePercent / 100m);
            result.InterestAmount = Math.Round(fullInterest, 2, MidpointRounding.AwayFromZero);
            result.IsProrated     = false;
        }

        result.ClosingBalance = account.Balance + result.InterestAmount;
        return result;
    }

    public PostingSummaryReport RunBatchPosting(
        IEnumerable<MemberAccount> accounts,
        InterestRate rate,
        int postingYear)
    {
        var results = accounts
            .Select(a => Calculate(a, rate, postingYear))
            .ToList();

        var posted  = results.Where(r => !r.IsSkipped).ToList();
        var skipped = results.Where(r => r.IsSkipped).ToList();

        return new PostingSummaryReport
        {
            PostingYear               = postingYear,
            GeneratedAt               = DateTime.UtcNow,
            AnnualRatePercent         = rate.AnnualRatePercent,
            TotalAccountsProcessed    = results.Count,
            TotalPosted               = posted.Count,
            TotalSkipped              = skipped.Count,
            TotalProrated             = posted.Count(r => r.IsProrated),
            TotalInterestDistributed  = posted.Sum(r => r.InterestAmount),
            TotalClosingBalance       = posted.Sum(r => r.ClosingBalance),
            Results                   = results
        };
    }
}
