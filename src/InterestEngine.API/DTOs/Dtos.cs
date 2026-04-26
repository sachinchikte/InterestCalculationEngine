namespace InterestEngine.API.DTOs;

// ── Request DTOs ─────────────────────────────────────────────────────────────

public record RunBatchPostingRequest(
    int PostingYear,
    decimal AnnualRatePercent
);

public record CalculateSingleRequest(
    int MemberId,
    int PostingYear,
    decimal AnnualRatePercent
);

public record LoginRequest(
    string Username,
    string Password
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record InterestResultDto(
    int MemberId,
    string AccountNumber,
    string FullName,
    decimal OpeningBalance,
    decimal AnnualRatePercent,
    decimal InterestAmount,
    decimal ClosingBalance,
    bool IsProrated,
    int? MonthsEligible,
    bool IsSkipped,
    string? SkipReason,
    DateTime PostingDate
);

public record BatchSummaryDto(
    int PostingYear,
    DateTime GeneratedAt,
    decimal AnnualRatePercent,
    int TotalAccountsProcessed,
    int TotalPosted,
    int TotalSkipped,
    int TotalProrated,
    decimal TotalInterestDistributed,
    decimal TotalClosingBalance,
    IEnumerable<InterestResultDto> Results
);

public record LoginResponse(
    string Token,
    string Username,
    DateTime ExpiresAt
);

public record ApiErrorResponse(
    string Error,
    string Detail
);
