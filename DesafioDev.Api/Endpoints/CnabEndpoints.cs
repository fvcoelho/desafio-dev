using DesafioDev.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DesafioDev.Api.Endpoints;

/// <summary>
/// Endpoints for CNAB file operations
/// </summary>
public static class CnabEndpoints
{
    /// <summary>
    /// Maps CNAB-related endpoints
    /// </summary>
    public static void MapCnabEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cnab")
            .WithTags("CNAB")
            .WithOpenApi();

        group.MapPost("/upload", UploadCnabFile)
            .WithName("UploadCnabFile")
            .WithSummary("Upload and process a CNAB file")
            .WithDescription("Uploads a CNAB file, parses its contents, and stores the transactions in memory")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(200)
            .Produces(400)
            .DisableAntiforgery();

        group.MapDelete("/clear", ClearData)
            .WithName("ClearData")
            .WithSummary("Clear all data")
            .WithDescription("Clears all stored transactions and stores (for testing)")
            .Produces(200);
    }

    private static async Task<IResult> UploadCnabFile(
        [FromForm] IFormFile file,
        ITransactionService transactionService,
        ILogger<ITransactionService> logger)
    {
        if (file == null || file.Length == 0)
        {
            logger.LogWarning("Upload attempted with no file or empty file");
            return Results.BadRequest(new { error = "No file uploaded or file is empty" });
        }

        // Validate file extension (optional - CNAB files might have .txt or .cnab extension)
        var allowedExtensions = new[] { ".txt", ".cnab", ".dat" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!string.IsNullOrEmpty(fileExtension) && !allowedExtensions.Contains(fileExtension))
        {
            logger.LogWarning("Invalid file extension: {Extension}", fileExtension);
            return Results.BadRequest(new { error = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });
        }

        logger.LogInformation(
            "Processing CNAB file upload: {FileName} ({Size} bytes)",
            file.FileName,
            file.Length);

        try
        {
            using var stream = file.OpenReadStream();
            var result = await transactionService.ImportCnabFileAsync(stream);

            if (!result.Success)
            {
                logger.LogError("CNAB file import failed: {ErrorMessage}", result.ErrorMessage);
                return Results.BadRequest(result);
            }

            logger.LogInformation(
                "CNAB file imported successfully: {TransactionCount} transactions, {StoreCount} stores",
                result.TransactionsImported,
                result.StoresProcessed);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during CNAB file upload");
            return Results.Problem(
                title: "Error processing CNAB file",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static IResult ClearData(
        ITransactionService transactionService,
        ILogger<ITransactionService> logger)
    {
        logger.LogInformation("Clearing all data via API endpoint");

        transactionService.ClearData();

        return Results.Ok(new { message = "All data cleared successfully" });
    }
}
