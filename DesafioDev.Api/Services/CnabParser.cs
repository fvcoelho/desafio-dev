using DesafioDev.Api.Models.Entities;
using DesafioDev.Api.Services.Interfaces;

namespace DesafioDev.Api.Services;

/// <summary>
/// Orchestrates parsing of complete CNAB files
/// </summary>
public class CnabParser : ICnabParser
{
    private readonly ICnabLineParser _lineParser;
    private readonly ILogger<CnabParser> _logger;

    public CnabParser(ICnabLineParser lineParser, ILogger<CnabParser> logger)
    {
        _lineParser = lineParser;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<Transaction>> ParseAsync(Stream fileStream)
    {
        if (fileStream == null || !fileStream.CanRead)
        {
            throw new ArgumentException("File stream is null or not readable", nameof(fileStream));
        }

        var transactions = new List<Transaction>();
        var lineNumber = 0;
        var errors = new List<string>();

        using var reader = new StreamReader(fileStream);

        _logger.LogInformation("Starting CNAB file parsing");

        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
            {
                _logger.LogWarning("Line {LineNumber}: Skipping empty line", lineNumber);
                continue;
            }

            try
            {
                var transaction = _lineParser.ParseLine(line, lineNumber);
                transactions.Add(transaction);

                _logger.LogDebug(
                    "Line {LineNumber}: Parsed {TransactionType} transaction for {StoreName} - Value: {Value}",
                    lineNumber,
                    transaction.Type,
                    transaction.StoreName,
                    transaction.Value);
            }
            catch (InvalidOperationException ex)
            {
                var errorMsg = $"Line {lineNumber}: {ex.Message}";
                errors.Add(errorMsg);
                _logger.LogError(ex, "Error parsing line {LineNumber}", lineNumber);
            }
        }

        if (errors.Any())
        {
            var errorSummary = string.Join(Environment.NewLine, errors);
            throw new InvalidOperationException(
                $"Failed to parse CNAB file. {errors.Count} error(s) found:{Environment.NewLine}{errorSummary}");
        }

        if (transactions.Count == 0)
        {
            throw new InvalidOperationException("No valid transactions found in the CNAB file");
        }

        _logger.LogInformation(
            "CNAB file parsing completed successfully. Parsed {Count} transactions",
            transactions.Count);

        return transactions;
    }
}
