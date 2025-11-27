namespace DesafioDev.Api.Configuration;

/// <summary>
/// Configuration options for storage backend
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Whether to use in-memory storage (true) or database storage (false)
    /// </summary>
    public bool UseInMemory { get; set; } = true;
}
