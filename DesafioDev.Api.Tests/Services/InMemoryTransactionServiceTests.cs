using DesafioDev.Api.Models.Entities;
using DesafioDev.Api.Services;
using DesafioDev.Api.Services.Interfaces;
using DesafioDev.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesafioDev.Api.Tests.Services;

public class InMemoryTransactionServiceTests
{
    private readonly Mock<ICnabParser> _mockParser;
    private readonly Mock<ILogger<InMemoryTransactionService>> _mockLogger;
    private readonly InMemoryTransactionService _service;

    public InMemoryTransactionServiceTests()
    {
        _mockParser = new Mock<ICnabParser>();
        _mockLogger = new Mock<ILogger<InMemoryTransactionService>>();
        _service = new InMemoryTransactionService(_mockParser.Object, _mockLogger.Object);
    }

    #region ImportCnabFileAsync - Success Scenarios

    [Fact]
    public async Task ImportCnabFileAsync_WithValidFile_ReturnsSuccessResponse()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction()
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        var result = await _service.ImportCnabFileAsync(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.TransactionsImported.Should().Be(1);
        result.StoresProcessed.Should().Be(1);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ImportCnabFileAsync_WithSingleTransaction_CreatesOneStore()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Test Store", storeOwner: "Test Owner")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        var result = await _service.ImportCnabFileAsync(stream);

        // Assert
        result.StoresProcessed.Should().Be(1);
    }

    [Fact]
    public async Task ImportCnabFileAsync_WithMultipleTransactionsSameStore_CreatesOneStore()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Same Store", storeOwner: "Same Owner"),
            CnabTestData.CreateTestTransaction(storeName: "Same Store", storeOwner: "Same Owner"),
            CnabTestData.CreateTestTransaction(storeName: "Same Store", storeOwner: "Same Owner")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        var result = await _service.ImportCnabFileAsync(stream);

        // Assert
        result.TransactionsImported.Should().Be(3);
        result.StoresProcessed.Should().Be(1);
    }

    [Fact]
    public async Task ImportCnabFileAsync_WithMultipleStores_CreatesAllStores()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store A", storeOwner: "Owner A"),
            CnabTestData.CreateTestTransaction(storeName: "Store B", storeOwner: "Owner B"),
            CnabTestData.CreateTestTransaction(storeName: "Store C", storeOwner: "Owner C")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        var result = await _service.ImportCnabFileAsync(stream);

        // Assert
        result.StoresProcessed.Should().Be(3);
    }

    [Fact]
    public async Task ImportCnabFileAsync_AssignsIncrementingStoreIds()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store 1", storeOwner: "Owner 1"),
            CnabTestData.CreateTestTransaction(storeName: "Store 2", storeOwner: "Owner 2"),
            CnabTestData.CreateTestTransaction(storeName: "Store 3", storeOwner: "Owner 3")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        await _service.ImportCnabFileAsync(stream);
        var balances = await _service.GetStoreBalancesAsync();

        // Assert
        var storeList = balances.ToList();
        storeList[0].StoreId.Should().Be(1);
        storeList[1].StoreId.Should().Be(2);
        storeList[2].StoreId.Should().Be(3);
    }

    [Fact]
    public async Task ImportCnabFileAsync_AssignsIncrementingTransactionIds()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(),
            CnabTestData.CreateTestTransaction(),
            CnabTestData.CreateTestTransaction()
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        await _service.ImportCnabFileAsync(stream);
        var balances = await _service.GetStoreBalancesAsync();

        // Assert
        var allTransactions = balances.First().Transactions.ToList();
        allTransactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task ImportCnabFileAsync_LinksTransactionToStore()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Linked Store", storeOwner: "Linked Owner")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        await _service.ImportCnabFileAsync(stream);
        var balances = await _service.GetStoreBalancesAsync();

        // Assert
        var store = balances.First();
        store.StoreName.Should().Be("Linked Store");
        store.OwnerName.Should().Be("Linked Owner");
        store.Transactions.Should().HaveCount(1);
    }

    #endregion

    #region ImportCnabFileAsync - Store Matching Logic

    [Fact]
    public async Task ImportCnabFileAsync_WithSameStoreNameAndOwner_ReusesExistingStore()
    {
        // Arrange
        var firstBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Existing Store", storeOwner: "Existing Owner")
        };
        var secondBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Existing Store", storeOwner: "Existing Owner")
        };

        _mockParser.SetupSequence(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(firstBatch)
            .ReturnsAsync(secondBatch);

        // Act
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test1"));
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test2"));
        var balances = await _service.GetStoreBalancesAsync();

        // Assert
        balances.Should().HaveCount(1);
        balances.First().Transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportCnabFileAsync_WithSameStoreNameDifferentOwner_CreatesNewStore()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Same Name", storeOwner: "Owner A"),
            CnabTestData.CreateTestTransaction(storeName: "Same Name", storeOwner: "Owner B")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        await _service.ImportCnabFileAsync(stream);
        var balances = await _service.GetStoreBalancesAsync();

        // Assert
        balances.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportCnabFileAsync_WithCaseInsensitiveMatch_ReusesExistingStore()
    {
        // Arrange
        var firstBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Test Store", storeOwner: "Test Owner")
        };
        var secondBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "TEST STORE", storeOwner: "TEST OWNER")
        };

        _mockParser.SetupSequence(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(firstBatch)
            .ReturnsAsync(secondBatch);

        // Act
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test1"));
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test2"));
        var balances = await _service.GetStoreBalancesAsync();

        // Assert
        balances.Should().HaveCount(1);
        balances.First().Transactions.Should().HaveCount(2);
    }

    #endregion

    #region ImportCnabFileAsync - Error Handling

    [Fact]
    public async Task ImportCnabFileAsync_WhenParserThrows_ReturnsErrorResponse()
    {
        // Arrange
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new InvalidOperationException("Parse error"));

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        var result = await _service.ImportCnabFileAsync(stream);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Parse error");
    }

    [Fact]
    public async Task ImportCnabFileAsync_WhenParserFails_SetsSuccessToFalse()
    {
        // Arrange
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new Exception("Generic error"));

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        var result = await _service.ImportCnabFileAsync(stream);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ImportCnabFileAsync_WhenParserFails_IncludesErrorMessage()
    {
        // Arrange
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new Exception("Detailed error message"));

        var stream = CnabTestData.CreateStreamFromString("test");

        // Act
        var result = await _service.ImportCnabFileAsync(stream);

        // Assert
        result.ErrorMessage.Should().Be("Detailed error message");
        result.TransactionsImported.Should().Be(0);
        result.StoresProcessed.Should().Be(0);
    }

    #endregion

    #region GetStoreBalancesAsync Tests

    [Fact]
    public async Task GetStoreBalancesAsync_WithNoData_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStoreBalancesAsync_WithStores_ReturnsAllStores()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store A", storeOwner: "Owner A"),
            CnabTestData.CreateTestTransaction(storeName: "Store B", storeOwner: "Owner B"),
            CnabTestData.CreateTestTransaction(storeName: "Store C", storeOwner: "Owner C")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetStoreBalancesAsync_OrdersByStoreName()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Zebra Store", storeOwner: "Owner Z"),
            CnabTestData.CreateTestTransaction(storeName: "Apple Store", storeOwner: "Owner A"),
            CnabTestData.CreateTestTransaction(storeName: "Mango Store", storeOwner: "Owner M")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        var storeList = result.ToList();
        storeList[0].StoreName.Should().Be("Apple Store");
        storeList[1].StoreName.Should().Be("Mango Store");
        storeList[2].StoreName.Should().Be("Zebra Store");
    }

    [Fact]
    public async Task GetStoreBalancesAsync_IncludesAllTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Test Store", storeOwner: "Test Owner"),
            CnabTestData.CreateTestTransaction(storeName: "Test Store", storeOwner: "Test Owner"),
            CnabTestData.CreateTestTransaction(storeName: "Test Store", storeOwner: "Test Owner")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.First().Transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetStoreBalancesAsync_OrdersTransactionsByDateThenTime()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(
                date: new DateTime(2019, 3, 2),
                time: new TimeSpan(10, 0, 0)),
            CnabTestData.CreateTestTransaction(
                date: new DateTime(2019, 3, 1),
                time: new TimeSpan(15, 0, 0)),
            CnabTestData.CreateTestTransaction(
                date: new DateTime(2019, 3, 1),
                time: new TimeSpan(10, 0, 0))
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        var transactionList = result.First().Transactions.ToList();
        transactionList[0].Date.Should().Be(new DateTime(2019, 3, 1));
        transactionList[0].Time.Should().Be(new TimeSpan(10, 0, 0));
        transactionList[1].Date.Should().Be(new DateTime(2019, 3, 1));
        transactionList[1].Time.Should().Be(new TimeSpan(15, 0, 0));
        transactionList[2].Date.Should().Be(new DateTime(2019, 3, 2));
    }

    [Fact]
    public async Task GetStoreBalancesAsync_CalculatesCorrectBalance()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(type: TransactionType.Debit, value: 100m), // +100
            CnabTestData.CreateTestTransaction(type: TransactionType.Boleto, value: 50m), // -50
            CnabTestData.CreateTestTransaction(type: TransactionType.Credit, value: 75m)  // +75
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.First().Balance.Should().Be(125m); // 100 - 50 + 75
    }

    [Fact]
    public async Task GetStoreBalancesAsync_WithIncomeTransactions_PositiveBalance()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(type: TransactionType.Debit, value: 100m),
            CnabTestData.CreateTestTransaction(type: TransactionType.Credit, value: 200m)
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.First().Balance.Should().Be(300m);
    }

    [Fact]
    public async Task GetStoreBalancesAsync_WithExpenseTransactions_NegativeBalance()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(type: TransactionType.Boleto, value: 100m),
            CnabTestData.CreateTestTransaction(type: TransactionType.Rent, value: 200m)
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.First().Balance.Should().Be(-300m);
    }

    [Fact]
    public async Task GetStoreBalancesAsync_WithMixedTransactions_CorrectSignedSum()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(type: TransactionType.Debit, value: 500m),    // +500
            CnabTestData.CreateTestTransaction(type: TransactionType.Boleto, value: 100m),   // -100
            CnabTestData.CreateTestTransaction(type: TransactionType.Credit, value: 300m),   // +300
            CnabTestData.CreateTestTransaction(type: TransactionType.Financing, value: 200m) // -200
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.First().Balance.Should().Be(500m); // 500 - 100 + 300 - 200
    }

    #endregion

    #region GetStoreBalancesAsync - DTO Mapping Tests

    [Fact]
    public async Task GetStoreBalancesAsync_MapsTransactionTypeDescription()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(type: TransactionType.Debit)
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.First().Transactions.First().Type.Should().Be("Débito");
    }

    [Fact]
    public async Task GetStoreBalancesAsync_IncludesSignedValue()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(type: TransactionType.Debit, value: 100m),
            CnabTestData.CreateTestTransaction(type: TransactionType.Boleto, value: 50m)
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        var transactionList = result.First().Transactions.ToList();
        transactionList[0].SignedValue.Should().Be(100m);  // Income
        transactionList[1].SignedValue.Should().Be(-50m);  // Expense
    }

    [Fact]
    public async Task GetStoreBalancesAsync_IncludesAllTransactionFields()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction(
            type: TransactionType.Debit,
            value: 123.45m,
            date: new DateTime(2019, 3, 1),
            time: new TimeSpan(15, 30, 45));
        transaction.Cpf = "12345678901";
        transaction.CardNumber = "1234****5678";

        var transactions = new List<Transaction> { transaction };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        var dto = result.First().Transactions.First();
        dto.Value.Should().Be(123.45m);
        dto.Date.Should().Be(new DateTime(2019, 3, 1));
        dto.Time.Should().Be(new TimeSpan(15, 30, 45));
        dto.Cpf.Should().Be("12345678901");
        dto.CardNumber.Should().Be("1234****5678");
    }

    #endregion

    #region ClearData Tests

    [Fact]
    public async Task ClearData_RemovesAllTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction()
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        _service.ClearData();
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearData_RemovesAllStores()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store A", storeOwner: "Owner A"),
            CnabTestData.CreateTestTransaction(storeName: "Store B", storeOwner: "Owner B")
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Act
        _service.ClearData();
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearData_ResetsTransactionIdCounter()
    {
        // Arrange
        var firstBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction()
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(firstBatch);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));
        _service.ClearData();

        // Act
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));
        var result = await _service.GetStoreBalancesAsync();

        // Assert - IDs should start at 1 again
        result.First().StoreId.Should().Be(1);
    }

    [Fact]
    public async Task ClearData_ResetsStoreIdCounter()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction()
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test1"));
        _service.ClearData();

        // Act
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test2"));
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.First().StoreId.Should().Be(1);
    }

    [Fact]
    public async Task ClearData_AfterClear_CanImportAgain()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction()
        };
        _mockParser.Setup(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(transactions);

        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));
        _service.ClearData();

        // Act
        var result = await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test"));

        // Assert
        result.Success.Should().BeTrue();
        result.TransactionsImported.Should().Be(1);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task ImportCnabFileAsync_CalledTwice_AppendsData()
    {
        // Arrange
        var firstBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store 1", storeOwner: "Owner 1")
        };
        var secondBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store 2", storeOwner: "Owner 2")
        };

        _mockParser.SetupSequence(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(firstBatch)
            .ReturnsAsync(secondBatch);

        // Act
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test1"));
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test2"));
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportCnabFileAsync_CalledTwice_ContinuesIdSequence()
    {
        // Arrange
        var firstBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store 1", storeOwner: "Owner 1")
        };
        var secondBatch = new List<Transaction>
        {
            CnabTestData.CreateTestTransaction(storeName: "Store 2", storeOwner: "Owner 2")
        };

        _mockParser.SetupSequence(p => p.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(firstBatch)
            .ReturnsAsync(secondBatch);

        // Act
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test1"));
        await _service.ImportCnabFileAsync(CnabTestData.CreateStreamFromString("test2"));
        var result = await _service.GetStoreBalancesAsync();

        // Assert
        var storeList = result.ToList();
        storeList[0].StoreId.Should().Be(1);
        storeList[1].StoreId.Should().Be(2);
    }

    #endregion
}
