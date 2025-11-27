# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a CNAB (Centro Nacional de Automação Bancária) file parser challenge. The application parses fixed-width financial transaction files and stores them in a relational database. This is a coding challenge for a developer position at ByCoders.

**Technology Stack:**
- .NET 8.0 (C# ASP.NET Core Web API)
- Minimal APIs architecture
- Swagger/OpenAPI for API documentation
- Currently includes only boilerplate WeatherForecast endpoint

## CNAB File Format

The CNAB.txt file contains fixed-width records with the following structure (each line is 81 characters):

| Field          | Start | End | Size | Description |
|----------------|-------|-----|------|-------------|
| Type           | 1     | 1   | 1    | Transaction type (1-9) |
| Date           | 2     | 9   | 8    | Date (YYYYMMDD format) |
| Value          | 10    | 19  | 10   | Amount in cents (divide by 100 to normalize) |
| CPF            | 20    | 30  | 11   | Beneficiary CPF |
| Card           | 31    | 42  | 12   | Card number (masked with ****) |
| Time           | 43    | 48  | 6    | Time in HHMMSS format (UTC-3) |
| Store Owner    | 49    | 62  | 14   | Store representative name |
| Store Name     | 63    | 81  | 19   | Store name |

**Transaction Types:**
- Type 1 (Debit): Income (+)
- Type 2 (Boleto): Expense (-)
- Type 3 (Financing): Expense (-)
- Type 4 (Credit): Income (+)
- Type 5 (Loan Receipt): Income (+)
- Type 6 (Sales): Income (+)
- Type 7 (TED Receipt): Income (+)
- Type 8 (DOC Receipt): Income (+)
- Type 9 (Rent): Expense (-)

## Development Commands

### Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API (default ports: https://localhost:7149, http://localhost:5118)
dotnet run --project DesafioDev.Api

# Run with specific profile
dotnet run --project DesafioDev.Api --launch-profile http
dotnet run --project DesafioDev.Api --launch-profile https
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Clean
```bash
# Clean build artifacts
dotnet clean
```

## Project Requirements (from README)

The application MUST:
1. Accept CNAB file uploads via web form
2. Parse fixed-width format and normalize data (value/100.00)
3. Store transactions in a relational database (PostgreSQL, MySQL, or SQL Server)
4. Display transactions grouped by store with balance totals
5. Include automated tests
6. Use atomic, well-described Git commits
7. Provide API endpoint documentation

Optional (bonus points):
- Docker Compose setup
- OAuth authentication
- Custom CSS (no popular frameworks)
- OpenAPI/Swagger documentation

## Architecture Notes

- The project uses .NET 8.0 Minimal APIs pattern
- Current structure is boilerplate - needs implementation of:
  - CNAB file upload endpoint
  - Fixed-width file parser with validation
  - Database models and EF Core/Dapper configuration
  - Transaction type mapping with nature (income/expense) logic
  - Store grouping and balance calculation
  - Unit tests for parser and business logic

- When implementing the parser, use `ReadOnlySpan<char>` or `string.Substring()` for efficient fixed-width field extraction
- Date format is YYYYMMDD, Time format is HHMMSS (both as strings in file)
- Values must be divided by 100.00 to convert from cents to currency units
- CPF and Card fields contain fixed-length strings (may have padding)

## Database Design Considerations

Suggested entities:
- **Store**: StoreId, StoreName, StoreOwner
- **Transaction**: TransactionId, Type, Date, Time, Value, CPF, CardNumber, StoreId
- **TransactionType**: Type (1-9), Description, Nature (Income/Expense), Sign (+/-)

Consider indexing on:
- Date fields for temporal queries
- StoreId for grouping operations
- Type for filtering by transaction category

## Development Workflow

When adding features:
1. Create domain models first (Transaction, Store entities)
2. Implement the CNAB parser with comprehensive tests
3. Set up database context and migrations
4. Create API endpoints (file upload, transaction listing)
5. Add validation and error handling
6. Write integration tests for the full pipeline

The file parsing logic should validate:
- Line length (exactly 81 characters)
- Transaction type (1-9 range)
- Date format (valid YYYYMMDD)
- Time format (valid HHMMSS)
- Numeric value fields
