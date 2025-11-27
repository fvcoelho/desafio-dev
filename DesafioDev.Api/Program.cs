using DesafioDev.Api.Configuration;
using DesafioDev.Api.Endpoints;
using DesafioDev.Api.Services;
using DesafioDev.Api.Services.Interfaces;

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
    Console.WriteLine($"[INFO] Using storage: {storageOptions.Type} (In-Memory)");
}
else
{
    // TODO: Register PostgreSQL implementation when available
    // builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    // builder.Services.AddScoped<ITransactionService, PostgresTransactionService>();

    // For now, fall back to in-memory
    builder.Services.AddSingleton<ITransactionService, InMemoryTransactionService>();
    Console.WriteLine($"[WARNING] PostgreSQL storage not yet implemented. Falling back to In-Memory storage.");
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .WithTags("Health")
    .WithOpenApi();

app.MapCnabEndpoints();
app.MapStoreEndpoints();

app.Run();

// Make the Program class accessible to WebApplicationFactory for integration testing
public partial class Program { }
