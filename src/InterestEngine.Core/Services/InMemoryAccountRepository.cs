using InterestEngine.Core.Interfaces;
using InterestEngine.Core.Models;

namespace InterestEngine.Core.Services;

/// <summary>
/// In-memory implementation of IAccountRepository.
/// Seeded with realistic pension member data for demo and testing purposes.
/// In a production system, replace with an EF Core / SQL Server implementation.
/// </summary>
public class InMemoryAccountRepository : IAccountRepository
{
    private readonly List<MemberAccount> _accounts = new()
    {
        new() { MemberId = 1,  FullName = "Rajesh Kumar",    AccountNumber = "PF-001", Balance = 150000.00m, DateOfJoining = new DateTime(2019, 4, 1),  IsActive = true  },
        new() { MemberId = 2,  FullName = "Priya Sharma",    AccountNumber = "PF-002", Balance = 320000.50m, DateOfJoining = new DateTime(2018, 6, 15), IsActive = true  },
        new() { MemberId = 3,  FullName = "Amit Desai",      AccountNumber = "PF-003", Balance = 0m,         DateOfJoining = new DateTime(2020, 1, 1),  IsActive = true  }, // Zero balance
        new() { MemberId = 4,  FullName = "Sunita Rao",      AccountNumber = "PF-004", Balance = 75000.00m,  DateOfJoining = new DateTime(2023, 8, 1),  IsActive = true  }, // Mid-year joiner
        new() { MemberId = 5,  FullName = "Vikram Nair",     AccountNumber = "PF-005", Balance = 540000.00m, DateOfJoining = new DateTime(2017, 3, 1),  IsActive = false }, // Inactive
        new() { MemberId = 6,  FullName = "Meena Joshi",     AccountNumber = "PF-006", Balance = 210000.75m, DateOfJoining = new DateTime(2021, 1, 1),  IsActive = true  },
        new() { MemberId = 7,  FullName = "Sandeep Patil",   AccountNumber = "PF-007", Balance = 89500.00m,  DateOfJoining = new DateTime(2023, 11, 1), IsActive = true  }, // Late-year joiner
        new() { MemberId = 8,  FullName = "Kavita Mehta",    AccountNumber = "PF-008", Balance = 412000.00m, DateOfJoining = new DateTime(2016, 7, 1),  IsActive = true  },
        new() { MemberId = 9,  FullName = "Rohit Gupta",     AccountNumber = "PF-009", Balance = 185000.00m, DateOfJoining = new DateTime(2022, 4, 1),  IsActive = true  },
        new() { MemberId = 10, FullName = "Anjali Singh",    AccountNumber = "PF-010", Balance = 630000.00m, DateOfJoining = new DateTime(2015, 1, 1),  IsActive = true  },
    };

    public Task<IEnumerable<MemberAccount>> GetAllActiveAsync()
        => Task.FromResult(_accounts.Where(a => a.IsActive).AsEnumerable());

    public Task<MemberAccount?> GetByIdAsync(int memberId)
        => Task.FromResult(_accounts.FirstOrDefault(a => a.MemberId == memberId));

    public Task<IEnumerable<MemberAccount>> GetByJoiningYearAsync(int year)
        => Task.FromResult(_accounts.Where(a => a.DateOfJoining.Year == year).AsEnumerable());
}
