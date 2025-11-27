namespace DesafioDev.Api.Models.Entities;

/// <summary>
/// Represents a store that processes transactions
/// </summary>
public class Store
{
    /// <summary>
    /// Unique identifier for the store
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Store name (e.g., "BAR DO JOÃO")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Name of the store owner/representative
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Collection of transactions for this store
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>
    /// Calculates the total balance for this store based on all transactions
    /// </summary>
    public decimal CalculateBalance()
    {
        return Transactions.Sum(t => t.SignedValue);
    }
}
