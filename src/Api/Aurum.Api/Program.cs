using Aurum.Api;
using Aurum.Api.Middlewares;
using Aurum.Application;
using Aurum.External;
using Aurum.Persistence;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Key Vault en producción
if (!builder.Environment.IsDevelopment())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        builder.Configuration.AddAzureKeyVault(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
    }
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);

// Registro de Application Insights
if (builder.Environment.IsProduction())
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Aplica migraciones automáticamente al arrancar de forma limpia
// Solo corre las migraciones automáticamente si estás en desarrollo local
if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync();
}

// Add middlewares global
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapApiEndpoints();

app.Run();
