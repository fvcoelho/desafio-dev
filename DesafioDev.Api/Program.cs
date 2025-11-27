using DesafioDev.Api.Configuration;
using DesafioDev.Api.Endpoints;
using DesafioDev.Api.Services;
using DesafioDev.Api.Services.Interfaces;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configure storage options
var storageOptions = builder.Configuration
    .GetSection(StorageOptions.SectionName)
    .Get<StorageOptions>() ?? new StorageOptions();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "CNAB File Parser API",
        Version = "v1",
        Description = "API for parsing and managing CNAB financial transaction files"
    });
});

// Register application services
builder.Services.AddSingleton<ICnabLineParser, CnabLineParser>();
builder.Services.AddSingleton<ICnabParser, CnabParser>();

// Register storage service based on configuration
if (storageOptions.UseInMemory)
{
    builder.Services.AddSingleton<ITransactionService, InMemoryTransactionService>();
    Console.WriteLine("[INFO] Using In-Memory storage");
}
else
{
    // TODO: Register PostgreSQL implementation when available
    // builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    // builder.Services.AddScoped<ITransactionService, PostgresTransactionService>();

    // For now, fall back to in-memory
    builder.Services.AddSingleton<ITransactionService, InMemoryTransactionService>();
    Console.WriteLine("[WARNING] PostgreSQL storage not yet implemented. Falling back to In-Memory storage.");
}

// Add CORS (optional, for frontend integration)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// Map endpoints
app.MapGet("/health", async (IConfiguration configuration) =>
{
    var healthStatus = new
    {
        status = "healthy",
        database = await CheckDatabaseConnection(configuration, storageOptions)
    };
    return Results.Ok(healthStatus);
})
    .WithName("HealthCheck")
    .WithTags("Health")
    .WithDescription("Health check endpoint that verifies API and database connectivity")
    .WithOpenApi();

app.MapCnabEndpoints();
app.MapStoreEndpoints();

app.Run();

// Helper method to check database connection
static async Task<string> CheckDatabaseConnection(IConfiguration configuration, StorageOptions storageOptions)
{
    // If using in-memory storage, skip database check
    if (storageOptions.UseInMemory)
    {
        return "N/A (using in-memory storage)";
    }

    try
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            return "error (no connection string)";
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Test with a simple query
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync();

        return "ok";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Database health check failed: {ex.Message}");
        return $"error ({ex.Message})";
    }
}

// Make the Program class accessible to WebApplicationFactory for integration testing
public partial class Program { }
