using DesafioDev.Api.Models.Entities;

namespace DesafioDev.Api.Services.Interfaces;

/// <summary>
/// Interface for parsing individual CNAB lines (81-character fixed-width format)
/// </summary>
public interface ICnabLineParser
{
    /// <summary>
    /// Parses a single line from the CNAB file
    /// </summary>
    /// <param name="line">The 81-character line to parse</param>
    /// <param name="lineNumber">Line number for error reporting</param>
    /// <returns>A Transaction object with parsed data</returns>
    /// <exception cref="InvalidOperationException">Thrown when line format is invalid</exception>
    Transaction ParseLine(string line, int lineNumber);
}
