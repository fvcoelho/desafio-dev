using System.Net;
using System.Net.Http.Json;
using DesafioDev.Api.Models.DTOs;
using DesafioDev.Api.Tests.Fixtures;
using FluentAssertions;

namespace DesafioDev.Api.Tests.Integration;

public class CnabApiIntegrationTests : IntegrationTestBase
{
    // Response DTOs for deserialization
    private record HealthResponse(string status);
    private record ErrorResponse(string error);
    private record ClearResponse(string message);

    #region Health Check Tests (2)

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        // Act
        var response = await GetJsonAsync<HealthResponse>("/health");

        // Assert
        response.Should().NotBeNull();
        response!.status.Should().Be("healthy");
    }

    #endregion

    #region CNAB Upload - Happy Path (4)

    [Fact]
    public async Task UploadCnab_WithValidFile_ReturnsSuccess()
    {
        // Arrange
        var cnabContent = CnabTestData.ValidDebitLine;
        var formData = CreateFileUploadContent("test.txt", cnabContent);

        // Act
        var response = await Client.PostAsync("/api/cnab/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UploadCnabResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.TransactionsImported.Should().Be(1);
        result.StoresProcessed.Should().Be(1);
    }

    [Fact]
    public async Task UploadCnab_WithMultipleTransactions_ImportsAll()
    {
        // Arrange
        var realCnabContent = GetRealCnabFileContent();
        var formData = CreateFileUploadContent("CNAB.txt", realCnabContent);

        // Act
        var response = await Client.PostAsync("/api/cnab/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UploadCnabResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.TransactionsImported.Should().Be(21); // CNAB.txt has 21 transactions
    }

    [Fact]
    public async Task UploadCnab_MultipleFiles_AccumulatesData()
    {
        // Arrange
        var firstFile = CnabTestData.ValidDebitLine;
        var secondFile = CnabTestData.ValidCreditLine;

        // Act
        var response1 = await Client.PostAsync("/api/cnab/upload",
            CreateFileUploadContent("file1.txt", firstFile));
        var response2 = await Client.PostAsync("/api/cnab/upload",
            CreateFileUploadContent("file2.txt", secondFile));

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balances.Should().NotBeNull();
        balances!.SelectMany(s => s.Transactions).Should().HaveCount(2);
    }

    [Fact]
    public async Task UploadCnab_CorrectlyGroupsByStore()
    {
        // Arrange - Create 2 transactions for the same store
        var line1 = CnabTestData.CreateValidLine(type: 3, value: "0000010000", store: "SAME STORE        ");
        var line2 = CnabTestData.CreateValidLine(type: 2, value: "0000005000", store: "SAME STORE        ");
        var cnabContent = $"{line1}\n{line2}";
        var formData = CreateFileUploadContent("test.txt", cnabContent);

        // Act
        await Client.PostAsync("/api/cnab/upload", formData);

        // Assert
        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balances.Should().NotBeNull();
        balances.Should().HaveCount(1);
        balances![0].Transactions.Should().HaveCount(2);
    }

    #endregion

    #region CNAB Upload - Validation (5)

    [Fact]
    public async Task UploadCnab_WithNoFile_ReturnsBadRequest()
    {
        // Arrange - Multipart form with wrong field name (not "file")
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("test"));
        formData.Add(fileContent, "wrongFieldName", "test.txt");

        // Act
        var response = await Client.PostAsync("/api/cnab/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadCnab_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var formData = CreateFileUploadContent("empty.txt", "");

        // Act
        var response = await Client.PostAsync("/api/cnab/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadCnab_WithoutMultipartContentType_ReturnsBadRequest()
    {
        // Arrange - Send as plain text instead of multipart
        var content = new StringContent(CnabTestData.ValidDebitLine);

        // Act
        var response = await Client.PostAsync("/api/cnab/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadCnab_WithInvalidFileExtension_ReturnsBadRequest()
    {
        // Arrange
        var formData = CreateFileUploadContent("test.pdf", CnabTestData.ValidDebitLine);

        // Act
        var response = await Client.PostAsync("/api/cnab/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadCnab_WithInvalidCnabContent_ReturnsBadRequest()
    {
        // Arrange - Line too short
        var formData = CreateFileUploadContent("test.txt", CnabTestData.LineTooShort);

        // Act
        var response = await Client.PostAsync("/api/cnab/upload", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Store Balances Tests (4)

    [Fact]
    public async Task GetStoreBalances_WithNoData_ReturnsEmptyArray()
    {
        // Act
        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");

        // Assert
        balances.Should().NotBeNull();
        balances.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStoreBalances_AfterUpload_ReturnsStoreData()
    {
        // Arrange
        var formData = CreateFileUploadContent("test.txt", CnabTestData.ValidDebitLine);
        await Client.PostAsync("/api/cnab/upload", formData);

        // Act
        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");

        // Assert
        balances.Should().NotBeNull();
        balances.Should().HaveCount(1);
        balances![0].StoreName.Should().Be("BAR DO JOÃO");
        balances[0].OwnerName.Should().Be("JOÃO MACEDO");
        balances[0].Transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStoreBalances_CalculatesBalancesCorrectly()
    {
        // Arrange - Create transactions with known values
        // Type 3 (Debit) = -142.00, Type 2 (Boleto) = -112.00, Type 4 (Credit) = +142.00
        var line1 = CnabTestData.CreateValidLine(type: 3, value: "0000014200"); // -142.00
        var line2 = CnabTestData.CreateValidLine(type: 2, value: "0000011200"); // -112.00
        var line3 = CnabTestData.CreateValidLine(type: 4, value: "0000014200"); // +142.00
        var cnabContent = $"{line1}\n{line2}\n{line3}";
        var formData = CreateFileUploadContent("test.txt", cnabContent);
        await Client.PostAsync("/api/cnab/upload", formData);

        // Act
        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");

        // Assert
        balances.Should().NotBeNull();
        balances.Should().HaveCount(1);
        balances![0].Balance.Should().Be(-112.00m); // -142 - 112 + 142 = -112
    }

    [Fact]
    public async Task GetStoreBalances_OrdersByStoreName()
    {
        // Arrange - Create transactions for different stores
        var storeA = CnabTestData.CreateValidLine(store: "AAA STORE         ");
        var storeC = CnabTestData.CreateValidLine(store: "CCC STORE         ");
        var storeB = CnabTestData.CreateValidLine(store: "BBB STORE         ");
        var cnabContent = $"{storeC}\n{storeA}\n{storeB}"; // Intentionally out of order
        var formData = CreateFileUploadContent("test.txt", cnabContent);
        await Client.PostAsync("/api/cnab/upload", formData);

        // Act
        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");

        // Assert
        balances.Should().NotBeNull();
        balances.Should().HaveCount(3);
        balances![0].StoreName.Should().Be("AAA STORE");
        balances[1].StoreName.Should().Be("BBB STORE");
        balances[2].StoreName.Should().Be("CCC STORE");
    }

    #endregion

    #region Clear Data Tests (2)

    [Fact]
    public async Task ClearData_ReturnsSuccess()
    {
        // Act
        var response = await Client.DeleteAsync("/api/cnab/clear");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ClearResponse>();
        result.Should().NotBeNull();
        result!.message.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ClearData_RemovesAllData()
    {
        // Arrange - Add some data first
        var formData = CreateFileUploadContent("test.txt", CnabTestData.ValidDebitLine);
        await Client.PostAsync("/api/cnab/upload", formData);

        // Verify data exists
        var balancesBeforeClear = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balancesBeforeClear.Should().NotBeEmpty();

        // Act - Clear data
        await Client.DeleteAsync("/api/cnab/clear");

        // Assert - Data should be gone
        var balancesAfterClear = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balancesAfterClear.Should().BeEmpty();
    }

    #endregion

    #region End-to-End Workflow Tests (3)

    [Fact]
    public async Task CompleteWorkflow_HealthUploadBalancesClear_Success()
    {
        // Step 1: Check health
        var healthResponse = await Client.GetAsync("/health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Upload CNAB file
        var formData = CreateFileUploadContent("test.txt", CnabTestData.ValidDebitLine);
        var uploadResponse = await Client.PostAsync("/api/cnab/upload", formData);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Get balances
        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balances.Should().HaveCount(1);

        // Step 4: Clear data
        var clearResponse = await Client.DeleteAsync("/api/cnab/clear");
        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Verify data cleared
        var balancesAfterClear = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balancesAfterClear.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleUploads_ThenClear_ThenUploadAgain_Works()
    {
        // Upload first file
        var formData1 = CreateFileUploadContent("file1.txt", CnabTestData.ValidDebitLine);
        await Client.PostAsync("/api/cnab/upload", formData1);

        // Upload second file
        var formData2 = CreateFileUploadContent("file2.txt", CnabTestData.ValidCreditLine);
        await Client.PostAsync("/api/cnab/upload", formData2);

        // Verify 2 transactions
        var balances1 = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balances1!.SelectMany(s => s.Transactions).Should().HaveCount(2);

        // Clear data
        await Client.DeleteAsync("/api/cnab/clear");

        // Verify empty
        var balances2 = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balances2.Should().BeEmpty();

        // Upload again
        var formData3 = CreateFileUploadContent("file3.txt", CnabTestData.ValidBoletoLine);
        await Client.PostAsync("/api/cnab/upload", formData3);

        // Verify only 1 transaction
        var balances3 = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balances3!.SelectMany(s => s.Transactions).Should().HaveCount(1);
    }

    [Fact]
    public async Task UploadRealCnabFile_VerifyExpectedResults()
    {
        // Arrange
        var realCnabContent = GetRealCnabFileContent();
        var formData = CreateFileUploadContent("CNAB.txt", realCnabContent);

        // Act
        var uploadResponse = await Client.PostAsync("/api/cnab/upload", formData);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadCnabResponse>();

        // Assert upload success
        uploadResult.Should().NotBeNull();
        uploadResult!.Success.Should().BeTrue();
        uploadResult.TransactionsImported.Should().Be(21);

        // Get balances and verify
        var balances = await GetJsonAsync<List<StoreBalanceDto>>("/api/stores/balances");
        balances.Should().NotBeNull();

        // Verify stores are ordered alphabetically
        var storeNames = balances!.Select(s => s.StoreName).ToList();
        storeNames.Should().BeInAscendingOrder();

        // Verify each store has transactions
        balances.Should().OnlyContain(s => s.Transactions.Any());

        // Verify total transactions match import count
        var totalTransactions = balances.SelectMany(s => s.Transactions).Count();
        totalTransactions.Should().Be(21);
    }

    #endregion
}
