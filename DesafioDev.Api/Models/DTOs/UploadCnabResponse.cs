namespace DesafioDev.Api.Models.DTOs;

/// <summary>
/// Response returned after uploading a CNAB file
/// </summary>
public class UploadCnabResponse
{
    /// <summary>
    /// Indicates if the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of transactions imported
    /// </summary>
    public int TransactionsImported { get; set; }

    /// <summary>
    /// Number of unique stores processed
    /// </summary>
    public int StoresProcessed { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
