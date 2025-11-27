using DesafioDev.Api.Models.Entities;

namespace DesafioDev.Api.Services.Interfaces;

/// <summary>
/// Interface for parsing complete CNAB files
/// </summary>
public interface ICnabParser
{
    /// <summary>
    /// Parses a CNAB file stream and returns a list of transactions
    /// </summary>
    /// <param name="fileStream">The CNAB file stream</param>
    /// <returns>List of parsed transactions</returns>
    /// <exception cref="InvalidOperationException">Thrown when file format is invalid</exception>
    Task<List<Transaction>> ParseAsync(Stream fileStream);
}
