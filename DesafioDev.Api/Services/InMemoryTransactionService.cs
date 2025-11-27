using DesafioDev.Api.Models.DTOs;
using DesafioDev.Api.Models.Entities;
using DesafioDev.Api.Services.Interfaces;

namespace DesafioDev.Api.Services;

/// <summary>
/// In-memory implementation of ITransactionService for Phase 1 (no database)
/// </summary>
public class InMemoryTransactionService : ITransactionService
{
    private readonly List<Transaction> _transactions = new();
    private readonly List<Store> _stores = new();
    private readonly ICnabParser _parser;
    private readonly ILogger<InMemoryTransactionService> _logger;
    private int _nextTransactionId = 1;
    private int _nextStoreId = 1;

    public InMemoryTransactionService(
        ICnabParser parser,
        ILogger<InMemoryTransactionService> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<UploadCnabResponse> ImportCnabFileAsync(Stream fileStream)
    {
        try
        {
            _logger.LogInformation("Starting CNAB file import");

            var transactions = await _parser.ParseAsync(fileStream);

            foreach (var transaction in transactions)
            {
                // Find or create store
                var store = _stores.FirstOrDefault(s =>
                    s.Name.Equals(transaction.StoreName, StringComparison.OrdinalIgnoreCase) &&
                    s.OwnerName.Equals(transaction.StoreOwner, StringComparison.OrdinalIgnoreCase));

                if (store == null)
                {
                    store = new Store
                    {
                        Id = _nextStoreId++,
                        Name = transaction.StoreName,
                        OwnerName = transaction.StoreOwner
                    };
                    _stores.Add(store);

                    _logger.LogDebug(
                        "Created new store: {StoreId} - {StoreName} ({OwnerName})",
                        store.Id,
                        store.Name,
                        store.OwnerName);
                }

                transaction.StoreId = store.Id;
                transaction.Store = store;
                transaction.Id = _nextTransactionId++;

                _transactions.Add(transaction);
                store.Transactions.Add(transaction);
            }

            var storeCount = transactions.Select(t => t.StoreId).Distinct().Count();

            _logger.LogInformation(
                "Successfully imported {TransactionCount} transactions for {StoreCount} stores",
                transactions.Count,
                storeCount);

            return new UploadCnabResponse
            {
                Success = true,
                TransactionsImported = transactions.Count,
                StoresProcessed = storeCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CNAB file");

            return new UploadCnabResponse
            {
                Success = false,
                TransactionsImported = 0,
                StoresProcessed = 0,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<StoreBalanceDto>> GetStoreBalancesAsync()
    {
        _logger.LogInformation("Retrieving store balances for {StoreCount} stores", _stores.Count);

        var balances = _stores.Select(store => new StoreBalanceDto
        {
            StoreId = store.Id,
            StoreName = store.Name,
            OwnerName = store.OwnerName,
            Transactions = _transactions
                .Where(t => t.StoreId == store.Id)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Time)
                .Select(t => new TransactionDto
                {
                    Type = t.Type.GetDescription(),
                    Date = t.Date,
                    Time = t.Time,
                    Value = t.Value,
                    SignedValue = t.SignedValue,
                    Cpf = t.Cpf,
                    CardNumber = t.CardNumber
                })
                .ToList(),
            Balance = store.CalculateBalance()
        }).OrderBy(s => s.StoreName).AsEnumerable();

        return Task.FromResult(balances);
    }

    /// <inheritdoc/>
    public void ClearData()
    {
        _logger.LogInformation("Clearing all in-memory data");

        _transactions.Clear();
        _stores.Clear();
        _nextTransactionId = 1;
        _nextStoreId = 1;
    }
}
