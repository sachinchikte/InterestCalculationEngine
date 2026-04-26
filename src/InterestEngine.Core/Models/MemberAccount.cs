namespace InterestEngine.Core.Models;

/// <summary>
/// Represents a pension fund member account eligible for interest posting.
/// </summary>
public class MemberAccount
{
    public int MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    /// <summary>
    /// Date the member joined the scheme. Used for proration on first posting year.
    /// </summary>
    public DateTime DateOfJoining { get; set; }

    public bool IsActive { get; set; } = true;
}
