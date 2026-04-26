using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InterestEngine.Core.Interfaces;
using InterestEngine.Core.Models;
using InterestEngine.API.DTOs;

namespace InterestEngine.API.Controllers;

/// <summary>
/// Handles interest posting operations for pension fund member accounts.
/// All endpoints require a valid JWT bearer token.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class InterestPostingController : ControllerBase
{
    private readonly IInterestCalculatorService _calculatorService;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<InterestPostingController> _logger;

    public InterestPostingController(
        IInterestCalculatorService calculatorService,
        IAccountRepository accountRepository,
        ILogger<InterestPostingController> logger)
    {
        _calculatorService = calculatorService;
        _accountRepository = accountRepository;
        _logger            = logger;
    }

    /// <summary>
    /// Runs interest posting for ALL active member accounts for a given year.
    /// Returns a full summary report including posted, skipped, and prorated accounts.
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunBatchPosting([FromBody] RunBatchPostingRequest request)
    {
        if (request.PostingYear < 2000 || request.PostingYear > DateTime.UtcNow.Year)
            return BadRequest(new ApiErrorResponse("Invalid year", $"Posting year must be between 2000 and {DateTime.UtcNow.Year}."));

        if (request.AnnualRatePercent <= 0 || request.AnnualRatePercent > 30)
            return BadRequest(new ApiErrorResponse("Invalid rate", "Annual rate must be between 0.01% and 30%."));

        _logger.LogInformation("Starting batch posting for year {Year} at rate {Rate}%",
            request.PostingYear, request.AnnualRatePercent);

        var accounts = await _accountRepository.GetAllActiveAsync();
        var rate     = new InterestRate { Year = request.PostingYear, AnnualRatePercent = request.AnnualRatePercent };
        var report   = _calculatorService.RunBatchPosting(accounts, rate, request.PostingYear);

        _logger.LogInformation("Batch complete — {Posted} posted, {Skipped} skipped, ₹{Total} distributed",
            report.TotalPosted, report.TotalSkipped, report.TotalInterestDistributed);

        return Ok(MapToDto(report));
    }

    /// <summary>
    /// Calculates interest for a single member account.
    /// Useful for previewing interest before committing a batch run.
    /// </summary>
    [HttpPost("calculate-single")]
    [ProducesResponseType(typeof(InterestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateSingle([FromBody] CalculateSingleRequest request)
    {
        if (request.AnnualRatePercent <= 0 || request.AnnualRatePercent > 30)
            return BadRequest(new ApiErrorResponse("Invalid rate", "Annual rate must be between 0.01% and 30%."));

        var account = await _accountRepository.GetByIdAsync(request.MemberId);
        if (account is null)
            return NotFound(new ApiErrorResponse("Not found", $"Member ID {request.MemberId} does not exist."));

        var rate   = new InterestRate { Year = request.PostingYear, AnnualRatePercent = request.AnnualRatePercent };
        var result = _calculatorService.Calculate(account, rate, request.PostingYear);

        return Ok(MapResultToDto(result));
    }

    /// <summary>
    /// Returns all member accounts in the system (active and inactive).
    /// Useful for verifying seed data and account details.
    /// </summary>
    [HttpGet("accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _accountRepository.GetAllActiveAsync();
        return Ok(accounts);
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static BatchSummaryDto MapToDto(Core.Models.PostingSummaryReport r) => new(
        r.PostingYear, r.GeneratedAt, r.AnnualRatePercent,
        r.TotalAccountsProcessed, r.TotalPosted, r.TotalSkipped,
        r.TotalProrated, r.TotalInterestDistributed, r.TotalClosingBalance,
        r.Results.Select(MapResultToDto));

    private static InterestResultDto MapResultToDto(Core.Models.InterestPostingResult r) => new(
        r.MemberId, r.AccountNumber, r.FullName, r.OpeningBalance,
        r.AnnualRatePercent, r.InterestAmount, r.ClosingBalance,
        r.IsProrated, r.MonthsEligible, r.IsSkipped, r.SkipReason, r.PostingDate);
}
