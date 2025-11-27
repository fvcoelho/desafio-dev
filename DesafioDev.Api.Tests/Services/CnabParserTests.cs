using DesafioDev.Api.Models.Entities;
using DesafioDev.Api.Services;
using DesafioDev.Api.Services.Interfaces;
using DesafioDev.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesafioDev.Api.Tests.Services;

public class CnabParserTests
{
    private readonly Mock<ICnabLineParser> _mockLineParser;
    private readonly Mock<ILogger<CnabParser>> _mockLogger;
    private readonly CnabParser _parser;

    public CnabParserTests()
    {
        _mockLineParser = new Mock<ICnabLineParser>();
        _mockLogger = new Mock<ILogger<CnabParser>>();
        _parser = new CnabParser(_mockLineParser.Object, _mockLogger.Object);
    }

    #region Valid Stream Parsing Tests

    [Fact]
    public async Task ParseAsync_WithValidSingleLine_ReturnsOneTransaction()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction);

        var stream = CnabTestData.CreateStreamFromLines(CnabTestData.ValidDebitLine);

        // Act
        var result = await _parser.ParseAsync(stream);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(transaction);
    }

    [Fact]
    public async Task ParseAsync_WithValidMultipleLines_ReturnsAllTransactions()
    {
        // Arrange
        var transaction1 = CnabTestData.CreateTestTransaction(TransactionType.Debit);
        var transaction2 = CnabTestData.CreateTestTransaction(TransactionType.Credit);
        var transaction3 = CnabTestData.CreateTestTransaction(TransactionType.Boleto);

        _mockLineParser.SetupSequence(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction1)
            .Returns(transaction2)
            .Returns(transaction3);

        var stream = CnabTestData.CreateStreamFromLines(
            CnabTestData.ValidDebitLine,
            CnabTestData.ValidCreditLine,
            CnabTestData.ValidBoletoLine);

        // Act
        var result = await _parser.ParseAsync(stream);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region Stream Validation Tests

    [Fact]
    public async Task ParseAsync_WithNullStream_ThrowsArgumentException()
    {
        // Arrange
        Stream? stream = null;

        // Act
        Func<Task> act = async () => await _parser.ParseAsync(stream!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*stream*");
    }

    [Fact]
    public async Task ParseAsync_WithUnreadableStream_ThrowsArgumentException()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.CanRead).Returns(false);

        // Act
        Func<Task> act = async () => await _parser.ParseAsync(mockStream.Object);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*stream*");
    }

    [Fact]
    public async Task ParseAsync_WithEmptyStream_ThrowsInvalidOperationException()
    {
        // Arrange
        var stream = CnabTestData.CreateStreamFromString("");

        // Act
        Func<Task> act = async () => await _parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No valid transactions found*");
    }

    #endregion

    #region Line Validation and Error Handling Tests

    [Fact]
    public async Task ParseAsync_WithEmptyLines_SkipsThemAndContinues()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction);

        var stream = CnabTestData.CreateStreamFromLines(
            CnabTestData.ValidDebitLine,
            "",
            CnabTestData.ValidCreditLine);

        // Act
        var result = await _parser.ParseAsync(stream);

        // Assert
        result.Should().HaveCount(2);
        _mockLineParser.Verify(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceLines_SkipsThemAndContinues()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction);

        var stream = CnabTestData.CreateStreamFromLines(
            CnabTestData.ValidDebitLine,
            "   ",
            CnabTestData.ValidCreditLine);

        // Act
        var result = await _parser.ParseAsync(stream);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_WithInvalidLine_ThrowsWithLineNumber()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), 1))
            .Returns(transaction);
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), 2))
            .Throws(new InvalidOperationException("Line 2: Invalid format"));

        var stream = CnabTestData.CreateStreamFromLines(
            CnabTestData.ValidDebitLine,
            "INVALID LINE");

        // Act
        Func<Task> act = async () => await _parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to parse CNAB file*");
    }

    [Fact]
    public async Task ParseAsync_WithMultipleInvalidLines_AggregatesErrors()
    {
        // Arrange
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Throws<InvalidOperationException>();

        var stream = CnabTestData.CreateStreamFromLines(
            "INVALID LINE 1",
            "INVALID LINE 2",
            "INVALID LINE 3");

        // Act
        Func<Task> act = async () => await _parser.ParseAsync(stream);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to parse CNAB file. 3 error(s)*");
    }

    #endregion

    #region Success Logging Tests

    [Fact]
    public async Task ParseAsync_WithValidLines_LogsSuccessMessage()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction);

        var stream = CnabTestData.CreateStreamFromLines(CnabTestData.ValidDebitLine);

        // Act
        await _parser.ParseAsync(stream);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed successfully")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ParseAsync_WithSkippedLines_LogsWarning()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction);

        var stream = CnabTestData.CreateStreamFromLines(
            CnabTestData.ValidDebitLine,
            "",
            CnabTestData.ValidCreditLine);

        // Act
        await _parser.ParseAsync(stream);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping empty line")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    #endregion

    #region Dependency Integration Tests

    [Fact]
    public async Task ParseAsync_CallsCnabLineParserForEachLine()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction);

        var stream = CnabTestData.CreateStreamFromLines(
            CnabTestData.ValidDebitLine,
            CnabTestData.ValidCreditLine,
            CnabTestData.ValidBoletoLine);

        // Act
        await _parser.ParseAsync(stream);

        // Assert
        _mockLineParser.Verify(
            p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ParseAsync_PassesCorrectLineNumberToCnabLineParser()
    {
        // Arrange
        var transaction = CnabTestData.CreateTestTransaction();
        _mockLineParser.Setup(p => p.ParseLine(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(transaction);

        var stream = CnabTestData.CreateStreamFromLines(
            CnabTestData.ValidDebitLine,
            CnabTestData.ValidCreditLine);

        // Act
        await _parser.ParseAsync(stream);

        // Assert
        _mockLineParser.Verify(p => p.ParseLine(It.IsAny<string>(), 1), Times.Once);
        _mockLineParser.Verify(p => p.ParseLine(It.IsAny<string>(), 2), Times.Once);
    }

    #endregion
}
