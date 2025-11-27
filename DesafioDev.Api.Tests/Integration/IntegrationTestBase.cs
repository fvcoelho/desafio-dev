using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace DesafioDev.Api.Tests.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly ApiWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase()
    {
        Factory = new ApiWebApplicationFactory();
        Client = Factory.CreateClient();
    }

    // Clear data before each test
    public async Task InitializeAsync()
    {
        await Client.DeleteAsync("/api/cnab/clear");
    }

    // Cleanup after each test
    public async Task DisposeAsync()
    {
        await Client.DeleteAsync("/api/cnab/clear");
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    // Helper: Create multipart form data for file upload
    protected MultipartFormDataContent CreateFileUploadContent(string fileName, string content)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        formData.Add(fileContent, "file", fileName);
        return formData;
    }

    // Helper: Get JSON response
    protected async Task<T?> GetJsonAsync<T>(string url)
    {
        return await Client.GetFromJsonAsync<T>(url);
    }

    // Helper: Read real CNAB.txt file
    protected string GetRealCnabFileContent()
    {
        // Binary is at: DesafioDev.Api.Tests/bin/Debug/net8.0/
        // Need to go up 4 levels to reach solution root
        var projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(projectRoot, "CNAB.txt"));
    }
}
