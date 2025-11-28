# Testing Guide

This document provides comprehensive instructions for running and understanding tests in the CNAB Parser project.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Running Tests](#running-tests)
  - [Run All Tests](#run-all-tests)
  - [Run Specific Test Categories](#run-specific-test-categories)
  - [Run Individual Tests](#run-individual-tests)
- [Test Output Options](#test-output-options)
- [Code Coverage](#code-coverage)
- [Continuous Integration](#continuous-integration)
- [Test Architecture](#test-architecture)
- [Writing New Tests](#writing-new-tests)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

| Software | Version | Installation |
|----------|---------|--------------|
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Docker | Latest | [Download](https://www.docker.com/products/docker-desktop) (optional, for integration tests with DB) |

### Verify Installation

```bash
# Check .NET SDK version
dotnet --version

# Should output 8.x.x or higher
```

---

## Quick Start

```bash
# Navigate to project root
cd desafio-dev

# Restore dependencies
dotnet restore

# Run all tests
dotnet test
```

---

## Running Tests

### Run All Tests

```bash
# Basic execution
dotnet test

# With detailed output
dotnet test --verbosity normal

# With build output
dotnet test --verbosity detailed
```

### Run Specific Test Categories

#### By Project

```bash
# Run only API tests
dotnet test DesafioDev.Api.Tests/DesafioDev.Api.Tests.csproj
```

#### By Test Class

```bash
# Run CnabLineParser tests only
dotnet test --filter "FullyQualifiedName~CnabLineParserTests"

# Run CnabParser tests only
dotnet test --filter "FullyQualifiedName~CnabParserTests"

# Run InMemoryTransactionService tests only
dotnet test --filter "FullyQualifiedName~InMemoryTransactionServiceTests"

# Run Integration tests only
dotnet test --filter "FullyQualifiedName~CnabApiIntegrationTests"
```

#### By Test Category (Namespace)

```bash
# Run all unit tests (Services namespace)
dotnet test --filter "FullyQualifiedName~Services"

# Run all integration tests
dotnet test --filter "FullyQualifiedName~Integration"
```

### Run Individual Tests

```bash
# Run a specific test by name
dotnet test --filter "ParseLine_WithValidDebitLine_ReturnsCorrectTransaction"

# Run tests matching a pattern
dotnet test --filter "Name~ParseLine"

# Run tests containing specific text
dotnet test --filter "DisplayName~Balance"
```

### Run Tests with Multiple Filters

```bash
# Run tests matching either condition (OR)
dotnet test --filter "FullyQualifiedName~CnabLineParser|FullyQualifiedName~CnabParser"

# Run tests matching both conditions (AND)
dotnet test --filter "FullyQualifiedName~Services&Name~Valid"
```

---

## Test Output Options

### Verbosity Levels

| Level | Description | Command |
|-------|-------------|---------|
| quiet | Minimal output | `dotnet test -v q` |
| minimal | Default, shows summary | `dotnet test -v m` |
| normal | Shows test names | `dotnet test -v n` |
| detailed | Shows detailed info | `dotnet test -v d` |
| diagnostic | Maximum verbosity | `dotnet test -v diag` |

### Output Formats

```bash
# Console logger (default)
dotnet test --logger "console;verbosity=detailed"

# TRX format (Visual Studio)
dotnet test --logger "trx;LogFileName=TestResults.trx"

# HTML report
dotnet test --logger "html;LogFileName=TestResults.html"

# JUnit format (for CI/CD)
dotnet test --logger "junit;LogFileName=TestResults.xml"

# Multiple loggers
dotnet test --logger "console" --logger "trx"
```

### Save Results to Directory

```bash
# Save results to specific directory
dotnet test --results-directory ./TestResults
```

---

## Code Coverage

### Generate Coverage Report

```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage"

# Output will be in TestResults/{guid}/coverage.cobertura.xml
```

### Generate HTML Coverage Report

```bash
# Install ReportGenerator tool (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open the report
open coveragereport/index.html  # macOS
# xdg-open coveragereport/index.html  # Linux
# start coveragereport/index.html  # Windows
```

### Coverage Thresholds

```bash
# Fail if coverage is below threshold (example: 80%)
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

---

## Continuous Integration

### GitHub Actions Example

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run tests
      run: dotnet test --no-build --verbosity normal --logger "trx" --results-directory ./TestResults

    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results
        path: ./TestResults/*.trx
```

### Azure DevOps Pipeline Example

```yaml
# azure-pipelines.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '8.0.x'

- script: dotnet restore
  displayName: 'Restore packages'

- script: dotnet build --configuration Release --no-restore
  displayName: 'Build'

- script: dotnet test --configuration Release --no-build --logger trx --results-directory $(Agent.TempDirectory)
  displayName: 'Run tests'

- task: PublishTestResults@2
  condition: always()
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '$(Agent.TempDirectory)/*.trx'
```

---

## Test Architecture

### Project Structure

```
DesafioDev.Api.Tests/
├── DesafioDev.Api.Tests.csproj    # Test project configuration
├── Fixtures/
│   └── CnabTestData.cs            # Shared test data and helpers
├── Services/
│   ├── CnabLineParserTests.cs     # Unit tests for line parser
│   ├── CnabParserTests.cs         # Unit tests for file parser
│   └── InMemoryTransactionServiceTests.cs  # Unit tests for service
└── Integration/
    ├── ApiWebApplicationFactory.cs # Test server configuration
    ├── IntegrationTestBase.cs      # Base class for integration tests
    └── CnabApiIntegrationTests.cs  # API endpoint tests
```

### Test Categories

| Category | Description | Count |
|----------|-------------|-------|
| Unit Tests | Test individual components in isolation | 76 |
| Integration Tests | Test API endpoints end-to-end | 20 |
| **Total** | | **96** |

### Test Dependencies

```xml
<!-- DesafioDev.Api.Tests.csproj -->
<PackageReference Include="xunit" Version="2.5.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="FluentAssertions" Version="8.8.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.12" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />
```

---

## Writing New Tests

### Unit Test Template

```csharp
using FluentAssertions;
using Moq;

namespace DesafioDev.Api.Tests.Services;

public class MyServiceTests
{
    private readonly Mock<IDependency> _mockDependency;
    private readonly MyService _service;

    public MyServiceTests()
    {
        _mockDependency = new Mock<IDependency>();
        _service = new MyService(_mockDependency.Object);
    }

    [Fact]
    public void MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var input = "test";
        _mockDependency.Setup(d => d.DoSomething()).Returns("result");

        // Act
        var result = _service.Method(input);

        // Assert
        result.Should().Be("expected");
    }

    [Theory]
    [InlineData(1, "one")]
    [InlineData(2, "two")]
    public void MethodName_WithVariousInputs_ReturnsExpected(int input, string expected)
    {
        // Arrange & Act
        var result = _service.Convert(input);

        // Assert
        result.Should().Be(expected);
    }
}
```

### Integration Test Template

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DesafioDev.Api.Tests.Integration;

public class MyApiTests : IntegrationTestBase
{
    [Fact]
    public async Task Endpoint_Scenario_ExpectedResult()
    {
        // Arrange
        var requestData = new { Name = "test" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/endpoint", requestData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MyResponse>();
        result.Should().NotBeNull();
    }
}
```

### Test Naming Convention

Follow the pattern: `MethodName_Scenario_ExpectedResult`

Examples:
- `ParseLine_WithValidDebitLine_ReturnsCorrectTransaction`
- `ImportCnabFileAsync_WhenParserThrows_ReturnsErrorResponse`
- `GetStoreBalances_WithNoData_ReturnsEmptyList`

---

## Troubleshooting

### Common Issues

#### Tests Not Found

```bash
# Rebuild the solution
dotnet clean
dotnet build

# Run tests with rebuild
dotnet test --no-restore
```

#### Integration Tests Failing

```bash
# Ensure no port conflicts
# Check if ports 5001, 5002 are in use
lsof -i :5001
lsof -i :5002

# Clear test data
dotnet test --filter "ClearData"
```

#### Coverage Not Generated

```bash
# Ensure coverlet is installed
dotnet add package coverlet.collector

# Run with explicit coverage flag
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

#### Slow Tests

```bash
# Run tests in parallel (default)
dotnet test --parallel

# Limit parallelization
dotnet test -- xUnit.MaxParallelThreads=4

# Run sequentially (for debugging)
dotnet test -- xUnit.MaxParallelThreads=1
```

### Debug Tests in IDE

#### Visual Studio Code

1. Install C# Dev Kit extension
2. Open Test Explorer (View > Testing)
3. Click the play button next to a test
4. Right-click for "Debug Test" option

#### Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Right-click on test > Debug
3. Set breakpoints as needed

#### JetBrains Rider

1. Open Unit Tests window
2. Right-click on test > Debug
3. Use built-in debugger

### Environment Variables

```bash
# Set test environment
export ASPNETCORE_ENVIRONMENT=Testing

# Run with specific configuration
dotnet test --configuration Release
```

---

## Summary Commands

| Task | Command |
|------|---------|
| Run all tests | `dotnet test` |
| Run with verbose output | `dotnet test -v n` |
| Run specific class | `dotnet test --filter "FullyQualifiedName~ClassName"` |
| Run specific test | `dotnet test --filter "TestMethodName"` |
| Run unit tests only | `dotnet test --filter "FullyQualifiedName~Services"` |
| Run integration tests only | `dotnet test --filter "FullyQualifiedName~Integration"` |
| Generate coverage | `dotnet test --collect:"XPlat Code Coverage"` |
| Save results as TRX | `dotnet test --logger "trx"` |
| Run in release mode | `dotnet test -c Release` |

---

## Related Documentation

- [DEPLOY.md](DEPLOY.md) - Deployment instructions
- [README.md](README.md) - Project overview
- [database/README.md](database/README.md) - Database setup
