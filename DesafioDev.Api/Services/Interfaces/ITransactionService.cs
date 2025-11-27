using DesafioDev.Api.Models.DTOs;

namespace DesafioDev.Api.Services.Interfaces;

/// <summary>
/// Service for managing transactions and stores
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Imports transactions from a CNAB file stream
    /// </summary>
    /// <param name="fileStream">The CNAB file stream</param>
    /// <returns>Number of transactions imported</returns>
    Task<UploadCnabResponse> ImportCnabFileAsync(Stream fileStream);

    /// <summary>
    /// Gets balance information for all stores
    /// </summary>
    /// <returns>List of store balances with transactions</returns>
    Task<IEnumerable<StoreBalanceDto>> GetStoreBalancesAsync();

    /// <summary>
    /// Clears all data (for testing purposes)
    /// </summary>
    void ClearData();
}
