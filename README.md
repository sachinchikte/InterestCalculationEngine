# Interest Calculation Engine

A production-grade **.NET 7 REST API** that automates annual interest posting for pension fund member accounts. Built to demonstrate real-world patterns used in enterprise financial systems — including proration for mid-year joiners, batch processing, JWT-secured endpoints, and audit-ready reporting.

> Inspired by financial module development work on government pension administration systems.

---

## Features

- **Batch interest posting** — processes all active member accounts in a single run
- **Single account preview** — calculate interest for one member before committing a batch
- **Proration logic** — mid-year joiners receive interest for the exact months they were active
- **Skip rules** — inactive accounts and zero-balance accounts are automatically excluded with documented reasons
- **JWT Authentication** — all posting endpoints are secured with bearer token authentication
- **Swagger / OpenAPI** — fully documented interactive API explorer at root URL
- **Custom Middleware** — request/response logging pipeline
- **xUnit Tests** — 18 unit tests covering all business rules and edge cases
- **Repository Pattern** — data access fully decoupled from business logic

---

## Architecture

```
InterestCalculationEngine/
├── src/
│   ├── InterestEngine.Core/          # Business logic — no framework dependencies
│   │   ├── Models/                   # Domain models (MemberAccount, InterestRate, etc.)
│   │   ├── Interfaces/               # IInterestCalculatorService, IAccountRepository
│   │   └── Services/                 # InterestCalculatorService, InMemoryAccountRepository
│   │
│   └── InterestEngine.API/           # ASP.NET Core Web API
│       ├── Controllers/              # InterestPostingController, AuthController
│       ├── DTOs/                     # Request/Response objects
│       ├── Middleware/               # RequestLoggingMiddleware
│       └── Program.cs                # DI wiring, JWT, Swagger setup
│
└── tests/
    └── InterestEngine.Tests/         # xUnit tests — 18 test cases
```

**Key design decisions:**

- `InterestEngine.Core` has zero framework dependencies — pure C# class library. This means the business rules are fully testable in isolation without spinning up a web host.
- `IAccountRepository` abstracts data access — swap `InMemoryAccountRepository` with an EF Core + SQL Server implementation without touching any business logic.
- All financial arithmetic uses `decimal` (not `double` or `float`) to prevent floating-point rounding errors — critical for financial accuracy.
- Rounding uses `MidpointRounding.AwayFromZero` — the standard for financial calculations.

---

## Getting Started

### Prerequisites
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- Visual Studio 2022 or VS Code

### Run the API

```bash
# Clone the repo
git clone https://github.com/sachinchikte/dotnet-portfolio.git
cd dotnet-portfolio/InterestCalculationEngine

# Run the API
cd src/InterestEngine.API
dotnet run

# API + Swagger UI available at:
# http://localhost:5000
```

### Run the Tests

```bash
cd tests/InterestEngine.Tests
dotnet test --logger "console;verbosity=detailed"
```

Expected output:
```
Test run for InterestEngine.Tests
Passed! - 18 tests passed, 0 failed, 0 skipped
```

---

## API Endpoints

### Authentication

**POST** `/api/auth/login`

Get a JWT token. Use it as `Authorization: Bearer {token}` on all other requests.

```json
// Request
{
  "username": "admin",
  "password": "Admin@123"
}

// Response
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "username": "admin",
  "expiresAt": "2024-01-15T14:30:00Z"
}
```

---

### Batch Interest Posting

**POST** `/api/interestposting/batch` *(requires JWT)*

Runs the interest posting engine across all active member accounts.

```json
// Request
{
  "postingYear": 2023,
  "annualRatePercent": 8.15
}

// Response (summary)
{
  "postingYear": 2023,
  "annualRatePercent": 8.15,
  "totalAccountsProcessed": 9,
  "totalPosted": 7,
  "totalSkipped": 2,
  "totalProrated": 2,
  "totalInterestDistributed": 148320.50,
  "totalClosingBalance": 1983210.75,
  "results": [ ... ]
}
```

---

### Single Account Calculation

**POST** `/api/interestposting/calculate-single` *(requires JWT)*

Preview interest for one member without committing a batch run.

```json
// Request
{
  "memberId": 4,
  "postingYear": 2023,
  "annualRatePercent": 8.15
}

// Response — member 4 joined Aug 2023, so prorated for 5 months
{
  "memberId": 4,
  "fullName": "Sunita Rao",
  "openingBalance": 75000.00,
  "interestAmount": 2546.88,
  "closingBalance": 77546.88,
  "isProrated": true,
  "monthsEligible": 5,
  "isSkipped": false
}
```

---

### Get All Accounts

**GET** `/api/interestposting/accounts` *(requires JWT)*

Returns the seeded member account list for verification.

---

## Business Rules

| Rule | Behaviour |
|------|-----------|
| Inactive account | Skipped — no interest posted |
| Zero or negative balance | Skipped — no interest applicable |
| Joined before posting year | Full annual interest: `Balance × (Rate / 100)` |
| Joined during posting year | Prorated: `Balance × (Rate / 100) × (MonthsEligible / 12)` |
| Months eligible | `12 - JoiningMonth + 1` |
| Rounding | `MidpointRounding.AwayFromZero` to 2 decimal places |

---

## Test Coverage

| Test Case | Rule Verified |
|-----------|---------------|
| Full-year interest — standard balance | Core calculation |
| Full-year interest — large balance | No overflow / precision loss |
| Decimal rounding | `MidpointRounding.AwayFromZero` |
| Mid-year joiner — July | 6-month proration |
| Mid-year joiner — April | 9-month proration |
| Mid-year joiner — January | 12-month (full year) |
| Mid-year joiner — December | 1-month proration |
| Inactive account skip | Skip rule 1 |
| Zero balance skip | Skip rule 2 |
| Negative balance skip | Skip rule 2 |
| Active account not skipped | Positive assertion |
| Posting date is Dec 31 | Metadata accuracy |
| Member info in result | Data mapping |
| Batch — correct aggregates | Sum, count, totals |
| Batch — empty list | Edge case |
| Batch — prorated count | Proration tracking |
| Batch — report year | Metadata |
| Batch — all inactive | All skipped |

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 11 |
| Framework | ASP.NET Core 7 / .NET 7 |
| ORM | Entity Framework Core (interface-ready) |
| Auth | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) |
| API Docs | Swagger / Swashbuckle / OpenAPI |
| Testing | xUnit + FluentAssertions |
| Patterns | Repository Pattern, Dependency Injection, Middleware Pipeline |
| Architecture | Clean separation — Core library + API layer |

---

## Author

**Sachin Chikte** — Backend .NET Developer  
[LinkedIn](https://linkedin.com/in/sachin-chikte-47a643227) · [GitHub](https://github.com/sachinchikte/dotnet-portfolio)
