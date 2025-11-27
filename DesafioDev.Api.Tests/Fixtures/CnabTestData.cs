using DesafioDev.Api.Models.Entities;

namespace DesafioDev.Api.Tests.Fixtures;

/// <summary>
/// Test data and helper methods for CNAB tests
/// </summary>
public static class CnabTestData
{
    // Valid CNAB line examples (80 characters each)
    public const string ValidDebitLine =
        "3201903011234567890096206760171234567890123153453JOÃO MACEDO   BAR DO JOÃO         ";

    public const string ValidCreditLine =
        "4201903011234567890556418150631234567890123100000MARIA JOSEFINA LOJA DO Ó - FILIAL ";

    public const string ValidBoletoLine =
        "2201903010000011200096206760171234567890123153000JOÃO MACEDO   BAR DO JOÃO         ";

    // Invalid line examples
    public const string LineTooShort = "12345";
    public static readonly string LineTooLong = new string('0', 81);
    public static readonly string InvalidTypeCharacter = "X" + new string('0', 79);
    public static readonly string InvalidTypeLine = "0" + new string('0', 79);

    /// <summary>
    /// Creates a test transaction with default or custom values
    /// </summary>
    public static Transaction CreateTestTransaction(
        TransactionType type = TransactionType.Debit,
        decimal value = 100.00m,
        string storeName = "Test Store",
        string storeOwner = "Test Owner",
        DateTime? date = null,
        TimeSpan? time = null)
    {
        return new Transaction
        {
            Type = type,
            Date = date ?? new DateTime(2019, 3, 1),
            Time = time ?? new TimeSpan(15, 34, 53),
            Value = value,
            Cpf = "09620676017",
            CardNumber = "1234****7890",
            StoreName = storeName,
            StoreOwner = storeOwner
        };
    }

    /// <summary>
    /// Creates a test stream from a string
    /// </summary>
    public static Stream CreateStreamFromString(string content)
    {
        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream);
        writer.Write(content);
        writer.Flush();
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Creates a test stream from multiple lines
    /// </summary>
    public static Stream CreateStreamFromLines(params string[] lines)
    {
        return CreateStreamFromString(string.Join(Environment.NewLine, lines));
    }

    /// <summary>
    /// Creates a valid CNAB line with specified values
    /// </summary>
    public static string CreateValidLine(
        int type = 3,
        string date = "20190301",
        string value = "0000014200",
        string cpf = "09620676017",
        string card = "123456789012",
        string time = "153453",
        string owner = "JOÃO MACEDO   ",
        string store = "BAR DO JOÃO         ")
    {
        return $"{type}{date}{value}{cpf}{card}{time}{owner}{store}";
    }
}
