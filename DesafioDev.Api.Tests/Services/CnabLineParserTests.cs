using DesafioDev.Api.Models.Entities;
using DesafioDev.Api.Services;
using DesafioDev.Api.Tests.Fixtures;
using FluentAssertions;

namespace DesafioDev.Api.Tests.Services;

public class CnabLineParserTests
{
    private readonly CnabLineParser _parser;

    public CnabLineParserTests()
    {
        _parser = new CnabLineParser();
    }

    #region Valid Line Parsing Tests

    [Fact]
    public void ParseLine_WithValidDebitLine_ReturnsCorrectTransaction()
    {
        // Arrange
        var line = CnabTestData.ValidDebitLine;

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Financing);
        result.Date.Should().Be(new DateTime(2019, 3, 1));
        result.Value.Should().Be(1234.00m);
        result.Cpf.Should().Be("09620676017");
        result.CardNumber.Should().Be("123456789012");
        result.Time.Should().Be(new TimeSpan(15, 34, 53));
        result.StoreOwner.Should().Be("JOÃO MACEDO");
        result.StoreName.Should().Be("BAR DO JOÃO");
    }

    [Fact]
    public void ParseLine_WithValidCreditLine_ReturnsCorrectTransaction()
    {
        // Arrange
        var line = CnabTestData.ValidCreditLine;

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Credit);
        result.Date.Should().Be(new DateTime(2019, 3, 1));
        result.Value.Should().Be(1234.00m);
        result.Cpf.Should().Be("55641815063");
        result.CardNumber.Should().Be("123456789012");
        result.Time.Should().Be(new TimeSpan(10, 0, 0));
        result.StoreOwner.Should().Be("MARIA JOSEFINA");
        result.StoreName.Should().Be("LOJA DO Ó - FILIAL");
    }

    [Theory]
    [InlineData(1, TransactionType.Debit)]
    [InlineData(2, TransactionType.Boleto)]
    [InlineData(3, TransactionType.Financing)]
    [InlineData(4, TransactionType.Credit)]
    [InlineData(5, TransactionType.LoanReceipt)]
    [InlineData(6, TransactionType.Sales)]
    [InlineData(7, TransactionType.TedReceipt)]
    [InlineData(8, TransactionType.DocReceipt)]
    [InlineData(9, TransactionType.Rent)]
    public void ParseLine_WithAllTransactionTypes_ParsesCorrectly(int typeValue, TransactionType expectedType)
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(type: typeValue);

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Type.Should().Be(expectedType);
    }

    #endregion

    #region Transaction Type Validation Tests

    [Fact]
    public void ParseLine_WithInvalidTypeCharacter_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.InvalidTypeCharacter;

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid transaction type*");
    }

    [Fact]
    public void ParseLine_WithTypeZero_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.InvalidTypeLine;

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Transaction type 0 is not valid*");
    }

    [Fact]
    public void ParseLine_WithTypeTen_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(type: 10);

        // Act
        Action act = () => _parser.ParseLine(line, 2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 2*");
    }

    #endregion

    #region Date Validation Tests

    [Fact]
    public void ParseLine_WithValidDate_ParsesCorrectly()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(date: "20230615");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Date.Should().Be(new DateTime(2023, 6, 15));
    }

    [Fact]
    public void ParseLine_WithInvalidDateFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(date: "2023-615");

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid date format*");
    }

    [Fact]
    public void ParseLine_WithInvalidDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(date: "20231399");

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid date format*");
    }

    #endregion

    #region Value Parsing Tests

    [Fact]
    public void ParseLine_WithZeroValue_ReturnsZeroDecimal()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(value: "0000000000");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Value.Should().Be(0.00m);
    }

    [Fact]
    public void ParseLine_WithPositiveValue_DividesBy100()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(value: "0000014200");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Value.Should().Be(142.00m);
    }

    [Fact]
    public void ParseLine_WithMaxValue_ParsesCorrectly()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(value: "9999999999");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Value.Should().Be(99999999.99m);
    }

    [Fact]
    public void ParseLine_WithNonNumericValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(value: "ABCDEFGHIJ");

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid value*");
    }

    #endregion

    #region Time Validation Tests

    [Fact]
    public void ParseLine_WithValidTime_ParsesCorrectly()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(time: "153453");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Time.Should().Be(new TimeSpan(15, 34, 53));
    }

    [Fact]
    public void ParseLine_WithMidnight_ParsesAsZero()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(time: "000000");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Time.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ParseLine_WithInvalidHour_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(time: "243000");

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid hour value*");
    }

    [Fact]
    public void ParseLine_WithInvalidMinute_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(time: "156000");

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid minute value*");
    }

    [Fact]
    public void ParseLine_WithInvalidSecond_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(time: "150060");

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid second value*");
    }

    [Fact]
    public void ParseLine_WithNonNumericTime_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(time: "ABC DEF");

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Invalid time format*");
    }

    #endregion

    #region Line Length Validation Tests

    [Fact]
    public void ParseLine_WithCorrectLength_Succeeds()
    {
        // Arrange
        var line = CnabTestData.ValidDebitLine;

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Should().NotBeNull();
        line.Length.Should().Be(80);
    }

    [Fact]
    public void ParseLine_WithTooShortLine_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.LineTooShort;

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Expected 80 characters*");
    }

    [Fact]
    public void ParseLine_WithTooLongLine_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = CnabTestData.LineTooLong;

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*Expected 80 characters, got 81*");
    }

    [Fact]
    public void ParseLine_WithEmptyLine_ThrowsInvalidOperationException()
    {
        // Arrange
        var line = string.Empty;

        // Act
        Action act = () => _parser.ParseLine(line, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*empty*");
    }

    [Fact]
    public void ParseLine_WithNullLine_ThrowsInvalidOperationException()
    {
        // Arrange
        string? line = null;

        // Act
        Action act = () => _parser.ParseLine(line!, 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Line 1*empty*");
    }

    #endregion

    #region String Field Extraction Tests

    [Fact]
    public void ParseLine_WithPaddedStrings_TrimsWhitespace()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(
            owner: "JOÃO MACEDO   ",
            store: "BAR DO JOÃO         ");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.StoreOwner.Should().Be("JOÃO MACEDO");
        result.StoreName.Should().Be("BAR DO JOÃO");
    }

    [Fact]
    public void ParseLine_WithValidCpf_ExtractsCorrectly()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(cpf: "12345678901");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.Cpf.Should().Be("12345678901");
    }

    [Fact]
    public void ParseLine_WithValidCardNumber_ExtractsCorrectly()
    {
        // Arrange
        var line = CnabTestData.CreateValidLine(card: "987654321098");

        // Act
        var result = _parser.ParseLine(line, 1);

        // Assert
        result.CardNumber.Should().Be("987654321098");
    }

    #endregion
}
