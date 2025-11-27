using DesafioDev.Api.Models.DTOs;
using DesafioDev.Api.Models.Entities;
using DesafioDev.Api.Services.Interfaces;
using Npgsql;

namespace DesafioDev.Api.Services;

/// <summary>
/// PostgreSQL implementation of ITransactionService using direct Npgsql
/// </summary>
public class PostgresTransactionService : ITransactionService
{
    private readonly string _connectionString;
    private readonly ICnabParser _parser;
    private readonly ILogger<PostgresTransactionService> _logger;

    public PostgresTransactionService(
        IConfiguration configuration,
        ICnabParser parser,
        ILogger<PostgresTransactionService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string not configured");
        _parser = parser;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<UploadCnabResponse> ImportCnabFileAsync(Stream fileStream)
    {
        try
        {
            _logger.LogInformation("Starting CNAB file import to PostgreSQL");

            var transactions = await _parser.ParseAsync(fileStream);
            var storeCount = 0;
            var transactionCount = 0;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Use transaction for atomicity
            await using var dbTransaction = await connection.BeginTransactionAsync();

            try
            {
                var processedStores = new HashSet<string>();

                foreach (var transaction in transactions)
                {
                    // Get or create store
                    var storeKey = $"{transaction.StoreName}|{transaction.StoreOwner}";
                    var storeId = await GetOrCreateStoreAsync(
                        connection,
                        transaction.StoreName,
                        transaction.StoreOwner);

                    if (!processedStores.Contains(storeKey))
                    {
                        processedStores.Add(storeKey);
                        storeCount++;
                    }

                    // Insert transaction
                    await InsertTransactionAsync(connection, transaction, storeId);
                    transactionCount++;
                }

                await dbTransaction.CommitAsync();

                _logger.LogInformation(
                    "Successfully imported {TransactionCount} transactions for {StoreCount} stores",
                    transactionCount,
                    storeCount);

                return new UploadCnabResponse
                {
                    Success = true,
                    TransactionsImported = transactionCount,
                    StoresProcessed = storeCount
                };
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CNAB file to PostgreSQL");

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
    public async Task<IEnumerable<StoreBalanceDto>> GetStoreBalancesAsync()
    {
        _logger.LogInformation("Retrieving store balances from PostgreSQL");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Query to get all stores
        var stores = await GetAllStoresAsync(connection);
        var balances = new List<StoreBalanceDto>();

        foreach (var store in stores)
        {
            // Get transactions for this store
            var transactions = await GetTransactionsByStoreIdAsync(connection, store.Id);

            var balance = new StoreBalanceDto
            {
                StoreId = store.Id,
                StoreName = store.Name,
                OwnerName = store.OwnerName,
                Transactions = transactions
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
                Balance = transactions.Sum(t => t.SignedValue)
            };

            balances.Add(balance);
        }

        return balances.OrderBy(b => b.StoreName);
    }

    /// <inheritdoc/>
    public void ClearData()
    {
        _logger.LogInformation("Clearing all data from PostgreSQL");

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        // Delete transactions first (FK constraint)
        using (var cmd = new NpgsqlCommand("TRUNCATE TABLE transactions, stores RESTART IDENTITY CASCADE", connection))
        {
            cmd.ExecuteNonQuery();
        }

        _logger.LogInformation("All data cleared successfully");
    }

    /// <summary>
    /// Gets or creates a store and returns its ID
    /// </summary>
    private async Task<int> GetOrCreateStoreAsync(
        NpgsqlConnection connection,
        string storeName,
        string ownerName)
    {
        // Try to insert, on conflict do nothing
        var insertSql = @"
            INSERT INTO stores (name, owner_name, created_at, updated_at)
            VALUES (@name, @owner_name, NOW(), NOW())
            ON CONFLICT (name, owner_name) DO NOTHING";

        await using (var cmd = new NpgsqlCommand(insertSql, connection))
        {
            cmd.Parameters.AddWithValue("@name", storeName);
            cmd.Parameters.AddWithValue("@owner_name", ownerName);
            await cmd.ExecuteNonQueryAsync();
        }

        // Now retrieve the store ID
        var selectSql = "SELECT id FROM stores WHERE name = @name AND owner_name = @owner_name";

        await using (var cmd = new NpgsqlCommand(selectSql, connection))
        {
            cmd.Parameters.AddWithValue("@name", storeName);
            cmd.Parameters.AddWithValue("@owner_name", ownerName);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }

    /// <summary>
    /// Inserts a transaction into the database
    /// </summary>
    private async Task InsertTransactionAsync(
        NpgsqlConnection connection,
        Transaction transaction,
        int storeId)
    {
        var sql = @"
            INSERT INTO transactions
            (type, date, time, value, cpf, card_number, store_name, store_owner, store_id, created_at)
            VALUES (@type, @date, @time, @value, @cpf, @card_number, @store_name, @store_owner, @store_id, NOW())";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@type", (int)transaction.Type);
        cmd.Parameters.AddWithValue("@date", transaction.Date);
        cmd.Parameters.AddWithValue("@time", transaction.Time);
        cmd.Parameters.AddWithValue("@value", transaction.Value);
        cmd.Parameters.AddWithValue("@cpf", transaction.Cpf);
        cmd.Parameters.AddWithValue("@card_number", transaction.CardNumber);
        cmd.Parameters.AddWithValue("@store_name", transaction.StoreName);
        cmd.Parameters.AddWithValue("@store_owner", transaction.StoreOwner);
        cmd.Parameters.AddWithValue("@store_id", storeId);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets all stores from the database
    /// </summary>
    private async Task<List<Store>> GetAllStoresAsync(NpgsqlConnection connection)
    {
        var stores = new List<Store>();
        var sql = "SELECT id, name, owner_name FROM stores ORDER BY name";

        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            stores.Add(new Store
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                OwnerName = reader.GetString(2)
            });
        }

        return stores;
    }

    /// <summary>
    /// Gets all transactions for a specific store
    /// </summary>
    private async Task<List<Transaction>> GetTransactionsByStoreIdAsync(
        NpgsqlConnection connection,
        int storeId)
    {
        var transactions = new List<Transaction>();
        var sql = @"
            SELECT id, type, date, time, value, cpf, card_number, store_name, store_owner
            FROM transactions
            WHERE store_id = @store_id
            ORDER BY date, time";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@store_id", storeId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            transactions.Add(new Transaction
            {
                Id = reader.GetInt32(0),
                Type = (TransactionType)reader.GetInt32(1),
                Date = reader.GetDateTime(2),
                Time = reader.GetTimeSpan(3),
                Value = reader.GetDecimal(4),
                Cpf = reader.GetString(5),
                CardNumber = reader.GetString(6),
                StoreName = reader.GetString(7),
                StoreOwner = reader.GetString(8),
                StoreId = storeId
            });
        }

        return transactions;
    }
}
