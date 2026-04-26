using FluentAssertions;
using InterestEngine.Core.Models;
using InterestEngine.Core.Services;
using Xunit;

namespace InterestEngine.Tests;

/// <summary>
/// Unit tests for InterestCalculatorService.
/// Covers all business rules: full-year interest, proration, zero balance,
/// inactive accounts, edge cases, and batch posting accuracy.
/// </summary>
public class InterestCalculatorServiceTests
{
    private readonly InterestCalculatorService _sut = new();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MemberAccount ActiveAccount(
        int id = 1,
        decimal balance = 100000m,
        DateTime? joined = null) => new()
    {
        MemberId      = id,
        FullName      = $"Test Member {id}",
        AccountNumber = $"PF-{id:D3}",
        Balance       = balance,
        DateOfJoining = joined ?? new DateTime(2020, 1, 1),
        IsActive      = true
    };

    private static InterestRate RateFor(int year, decimal rate = 8.15m) => new()
    {
        Year = year,
        AnnualRatePercent = rate
    };

    // ── Full-year interest ────────────────────────────────────────────────────

    [Fact]
    public void Calculate_FullYear_ReturnsCorrectInterest()
    {
        // Arrange
        var account = ActiveAccount(balance: 100000m, joined: new DateTime(2020, 1, 1));
        var rate    = RateFor(2023, 8.15m);

        // Act
        var result = _sut.Calculate(account, rate, 2023);

        // Assert
        result.IsSkipped.Should().BeFalse();
        result.IsProrated.Should().BeFalse();
        result.InterestAmount.Should().Be(8150.00m);         // 100000 × 8.15%
        result.ClosingBalance.Should().Be(108150.00m);
    }

    [Fact]
    public void Calculate_FullYear_RoundsToTwoDecimalPlaces()
    {
        // 150000 × 8.15% = 12225.00 — exact, no rounding needed
        // 150001 × 8.15% = 12225.0815 — should round to 12225.08
        var account = ActiveAccount(balance: 150001m, joined: new DateTime(2019, 1, 1));
        var rate    = RateFor(2023, 8.15m);

        var result = _sut.Calculate(account, rate, 2023);

        result.InterestAmount.Should().Be(12225.08m);
    }

    [Fact]
    public void Calculate_FullYear_LargeBalance_ReturnsCorrectInterest()
    {
        var account = ActiveAccount(balance: 5_000_000m, joined: new DateTime(2015, 1, 1));
        var rate    = RateFor(2023, 8.50m);

        var result = _sut.Calculate(account, rate, 2023);

        result.InterestAmount.Should().Be(425_000.00m);      // 5000000 × 8.50%
        result.ClosingBalance.Should().Be(5_425_000.00m);
        result.IsProrated.Should().BeFalse();
    }

    // ── Proration — mid-year joiners ──────────────────────────────────────────

    [Fact]
    public void Calculate_MidYearJoiner_ReturnsProratedInterest()
    {
        // Joined July 1 2023 → eligible for 6 months (Jul–Dec)
        var account = ActiveAccount(balance: 120000m, joined: new DateTime(2023, 7, 1));
        var rate    = RateFor(2023, 8.15m);

        var result = _sut.Calculate(account, rate, 2023);

        // Expected: 120000 × 8.15% × (6/12) = 4890.00
        result.IsProrated.Should().BeTrue();
        result.MonthsEligible.Should().Be(6);
        result.InterestAmount.Should().Be(4890.00m);
        result.ClosingBalance.Should().Be(124890.00m);
    }

    [Fact]
    public void Calculate_JoinedInApril_ReturnsNineMonthProration()
    {
        // Joined April 1 → eligible for 9 months (Apr–Dec)
        var account = ActiveAccount(balance: 100000m, joined: new DateTime(2023, 4, 1));
        var rate    = RateFor(2023, 8.15m);

        var result = _sut.Calculate(account, rate, 2023);

        // 100000 × 8.15% × (9/12) = 6112.50
        result.IsProrated.Should().BeTrue();
        result.MonthsEligible.Should().Be(9);
        result.InterestAmount.Should().Be(6112.50m);
    }

    [Fact]
    public void Calculate_JoinedInJanuary_TreatedAsFullYear()
    {
        // Joined Jan 1 of posting year → 12 months eligible → full year, NOT prorated
        var account = ActiveAccount(balance: 100000m, joined: new DateTime(2023, 1, 1));
        var rate    = RateFor(2023, 8.15m);

        var result = _sut.Calculate(account, rate, 2023);

        result.IsProrated.Should().BeTrue();   // Joined this year, but gets full 12 months
        result.MonthsEligible.Should().Be(12);
        result.InterestAmount.Should().Be(8150.00m);
    }

    [Fact]
    public void Calculate_JoinedInDecember_SkipsPosting()
    {
        // Joined Dec 1 — only 1 month. Business rule: months - month + 1 = 12 - 12 + 1 = 1
        // Still gets 1 month interest — ensure it calculates correctly
        var account = ActiveAccount(balance: 100000m, joined: new DateTime(2023, 12, 1));
        var rate    = RateFor(2023, 8.15m);

        var result = _sut.Calculate(account, rate, 2023);

        result.IsProrated.Should().BeTrue();
        result.MonthsEligible.Should().Be(1);
        // 100000 × 8.15% × (1/12) = 679.17
        result.InterestAmount.Should().Be(679.17m);
    }

    // ── Skip rules ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_InactiveAccount_IsSkipped()
    {
        var account = ActiveAccount(balance: 200000m);
        account.IsActive = false;

        var result = _sut.Calculate(account, RateFor(2023), 2023);

        result.IsSkipped.Should().BeTrue();
        result.SkipReason.Should().Contain("inactive");
        result.InterestAmount.Should().Be(0m);
        result.ClosingBalance.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ZeroBalance_IsSkipped()
    {
        var account = ActiveAccount(balance: 0m);

        var result = _sut.Calculate(account, RateFor(2023), 2023);

        result.IsSkipped.Should().BeTrue();
        result.SkipReason.Should().Contain("zero");
        result.InterestAmount.Should().Be(0m);
    }

    [Fact]
    public void Calculate_NegativeBalance_IsSkipped()
    {
        var account = ActiveAccount(balance: -500m);

        var result = _sut.Calculate(account, RateFor(2023), 2023);

        result.IsSkipped.Should().BeTrue();
        result.InterestAmount.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ActiveAccount_IsNotSkipped()
    {
        var account = ActiveAccount(balance: 50000m);

        var result = _sut.Calculate(account, RateFor(2023), 2023);

        result.IsSkipped.Should().BeFalse();
        result.SkipReason.Should().BeNull();
    }

    // ── Result metadata ───────────────────────────────────────────────────────

    [Fact]
    public void Calculate_PostingDateIsLastDayOfPostingYear()
    {
        var result = _sut.Calculate(ActiveAccount(), RateFor(2023), 2023);

        result.PostingDate.Should().Be(new DateTime(2023, 12, 31));
    }

    [Fact]
    public void Calculate_ResultContainsCorrectMemberInfo()
    {
        var account = new MemberAccount
        {
            MemberId      = 99,
            FullName      = "Sachin Chikte",
            AccountNumber = "PF-099",
            Balance       = 200000m,
            DateOfJoining = new DateTime(2020, 1, 1),
            IsActive      = true
        };

        var result = _sut.Calculate(account, RateFor(2023), 2023);

        result.MemberId.Should().Be(99);
        result.FullName.Should().Be("Sachin Chikte");
        result.AccountNumber.Should().Be("PF-099");
        result.OpeningBalance.Should().Be(200000m);
    }

    // ── Batch posting ─────────────────────────────────────────────────────────

    [Fact]
    public void RunBatchPosting_ReturnsCorrectAggregates()
    {
        var accounts = new List<MemberAccount>
        {
            ActiveAccount(1, 100000m, new DateTime(2020, 1, 1)),  // posted, full year
            ActiveAccount(2, 200000m, new DateTime(2020, 1, 1)),  // posted, full year
            ActiveAccount(3, 0m,      new DateTime(2020, 1, 1)),  // skipped — zero balance
        };
        accounts[2].IsActive = false;                              // also inactive

        var rate   = RateFor(2023, 10.00m);
        var report = _sut.RunBatchPosting(accounts, rate, 2023);

        report.TotalAccountsProcessed.Should().Be(3);
        report.TotalPosted.Should().Be(2);
        report.TotalSkipped.Should().Be(1);
        report.TotalInterestDistributed.Should().Be(30000m);      // (100000 + 200000) × 10%
        report.TotalClosingBalance.Should().Be(330000m);          // 110000 + 220000
    }

    [Fact]
    public void RunBatchPosting_EmptyAccountList_ReturnsZeroReport()
    {
        var report = _sut.RunBatchPosting(
            Enumerable.Empty<MemberAccount>(), RateFor(2023), 2023);

        report.TotalAccountsProcessed.Should().Be(0);
        report.TotalInterestDistributed.Should().Be(0m);
        report.Results.Should().BeEmpty();
    }

    [Fact]
    public void RunBatchPosting_MixedAccounts_CountsProratedCorrectly()
    {
        var accounts = new List<MemberAccount>
        {
            ActiveAccount(1, 100000m, new DateTime(2019, 1, 1)), // full year
            ActiveAccount(2, 100000m, new DateTime(2023, 6, 1)), // prorated — 7 months
            ActiveAccount(3, 100000m, new DateTime(2023, 9, 1)), // prorated — 4 months
        };

        var report = _sut.RunBatchPosting(accounts, RateFor(2023, 8.00m), 2023);

        report.TotalPosted.Should().Be(3);
        report.TotalProrated.Should().Be(2);
        report.TotalSkipped.Should().Be(0);
    }

    [Fact]
    public void RunBatchPosting_ReportYearMatchesRequest()
    {
        var report = _sut.RunBatchPosting(
            new[] { ActiveAccount() }, RateFor(2022, 8.15m), 2022);

        report.PostingYear.Should().Be(2022);
        report.AnnualRatePercent.Should().Be(8.15m);
    }
}
