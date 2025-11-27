namespace DesafioDev.Api.Models.DTOs;

/// <summary>
/// Data transfer object for store balance information
/// </summary>
public class StoreBalanceDto
{
    /// <summary>
    /// Store identifier
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Store name
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Store owner name
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// List of transactions for this store
    /// </summary>
    public List<TransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// Total balance (sum of signed values)
    /// </summary>
    public decimal Balance { get; set; }
}
