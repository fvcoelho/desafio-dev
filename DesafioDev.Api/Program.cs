using DesafioDev.Api.Endpoints;
using DesafioDev.Api.Services;
using DesafioDev.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<ITransactionService, InMemoryTransactionService>();

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
