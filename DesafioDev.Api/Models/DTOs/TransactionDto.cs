namespace DesafioDev.Api.Models.DTOs;

/// <summary>
/// Data transfer object for a transaction
/// </summary>
public class TransactionDto
{
    /// <summary>
    /// Type of transaction as string
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Transaction date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Transaction time
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Transaction value (normalized)
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Signed value (positive for income, negative for expenses)
    /// </summary>
    public decimal SignedValue { get; set; }

    /// <summary>
    /// CPF of the beneficiary
    /// </summary>
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// Card number (masked)
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;
}
