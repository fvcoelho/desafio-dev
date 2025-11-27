namespace DesafioDev.Api.Models.Entities;

/// <summary>
/// Represents a financial transaction from a CNAB file
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier for the transaction
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of transaction (Debit, Credit, etc.)
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Date when the transaction occurred
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Time when the transaction occurred (UTC-3)
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Transaction value (normalized - divided by 100)
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// CPF of the beneficiary
    /// </summary>
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// Card number used in the transaction (may be masked)
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Store name from CNAB file (used for matching)
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Store owner name from CNAB file (used for matching)
    /// </summary>
    public string StoreOwner { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the Store
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Navigation property to the Store
    /// </summary>
    public Store? Store { get; set; }

    /// <summary>
    /// Gets the signed value of the transaction (positive for income, negative for expenses)
    /// </summary>
    public decimal SignedValue => Type.IsIncome() ? Value : -Value;
}
