using InterestEngine.Core.Models;

namespace InterestEngine.Core.Interfaces;

/// <summary>
/// Repository contract for member account data access.
/// Follows Repository Pattern to decouple business logic from persistence.
/// </summary>
public interface IAccountRepository
{
    Task<IEnumerable<MemberAccount>> GetAllActiveAsync();
    Task<MemberAccount?> GetByIdAsync(int memberId);
    Task<IEnumerable<MemberAccount>> GetByJoiningYearAsync(int year);
}
