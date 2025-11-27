using DesafioDev.Api.Services.Interfaces;

namespace DesafioDev.Api.Endpoints;

/// <summary>
/// Endpoints for store and balance operations
/// </summary>
public static class StoreEndpoints
{
    /// <summary>
    /// Maps store-related endpoints
    /// </summary>
    public static void MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stores")
            .WithTags("Stores")
            .WithOpenApi();

        group.MapGet("/balances", GetStoreBalances)
            .WithName("GetStoreBalances")
            .WithSummary("Get balances for all stores")
            .WithDescription("Returns a list of all stores with their transactions and calculated balances")
            .Produces(200);
    }

    private static async Task<IResult> GetStoreBalances(
        ITransactionService transactionService,
        ILogger<ITransactionService> logger)
    {
        logger.LogInformation("Retrieving store balances");

        try
        {
            var balances = await transactionService.GetStoreBalancesAsync();

            var balanceList = balances.ToList();

            logger.LogInformation("Retrieved balances for {StoreCount} stores", balanceList.Count);

            return Results.Ok(balanceList);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving store balances");
            return Results.Problem(
                title: "Error retrieving store balances",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}
