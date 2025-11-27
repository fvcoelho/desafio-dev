using DesafioDev.Api.Models.Entities;
using DesafioDev.Api.Services.Interfaces;
using System.Globalization;

namespace DesafioDev.Api.Services;

/// <summary>
/// Parses individual lines from CNAB fixed-width format files
/// </summary>
public class CnabLineParser : ICnabLineParser
{
    private const int ExpectedLineLength = 80;

    /// <inheritdoc/>
    public Transaction ParseLine(string line, int lineNumber)
    {
        ValidateLineLength(line, lineNumber);

        try
        {
            return new Transaction
            {
                Type = ParseType(line[0..1], lineNumber),
                Date = ParseDate(line[1..9], lineNumber),
                Value = ParseValue(line[9..19], lineNumber),
                Cpf = line[19..30].Trim(),
                CardNumber = line[30..42].Trim(),
                Time = ParseTime(line[42..48], lineNumber),
                StoreOwner = line[48..62].Trim(),
                StoreName = line[62..80].Trim()
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Error parsing CNAB line - {ex.Message}", ex);
        }
    }

    private void ValidateLineLength(string line, int lineNumber)
    {
        if (string.IsNullOrEmpty(line))
        {
            throw new InvalidOperationException($"Line {lineNumber}: Line is empty");
        }

        if (line.Length != ExpectedLineLength)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Expected {ExpectedLineLength} characters, got {line.Length}");
        }
    }

    private TransactionType ParseType(string typeStr, int lineNumber)
    {
        if (!int.TryParse(typeStr, out int typeValue))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid transaction type '{typeStr}'");
        }

        if (!Enum.IsDefined(typeof(TransactionType), typeValue))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Transaction type {typeValue} is not valid (must be 1-9)");
        }

        return (TransactionType)typeValue;
    }

    private DateTime ParseDate(string dateStr, int lineNumber)
    {
        // Format: YYYYMMDD
        if (!DateTime.TryParseExact(
            dateStr,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid date format '{dateStr}' (expected YYYYMMDD)");
        }

        return date;
    }

    private TimeSpan ParseTime(string timeStr, int lineNumber)
    {
        // Format: HHMMSS
        if (timeStr.Length != 6)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid time length '{timeStr}' (expected 6 characters)");
        }

        if (!int.TryParse(timeStr[0..2], out int hours) ||
            !int.TryParse(timeStr[2..4], out int minutes) ||
            !int.TryParse(timeStr[4..6], out int seconds))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid time format '{timeStr}' (expected HHMMSS)");
        }

        if (hours < 0 || hours > 23)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid hour value {hours} (must be 0-23)");
        }

        if (minutes < 0 || minutes > 59)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid minute value {minutes} (must be 0-59)");
        }

        if (seconds < 0 || seconds > 59)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid second value {seconds} (must be 0-59)");
        }

        return new TimeSpan(hours, minutes, seconds);
    }

    private decimal ParseValue(string valueStr, int lineNumber)
    {
        if (!long.TryParse(valueStr, out long rawValue))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Invalid value '{valueStr}' (expected numeric)");
        }

        if (rawValue < 0)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Value cannot be negative ({rawValue})");
        }

        // Normalize: divide by 100 to convert from cents
        return rawValue / 100.00m;
    }
}
