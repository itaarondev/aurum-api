using Aurum.Api.Extensions;
using Aurum.Api.Middlewares;
using Aurum.Application;
using Aurum.External;
using Aurum.Persistence;
using Aurum.Persistence.Context;
using Azure.Identity;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Key Vault en producción
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (string.IsNullOrEmpty(keyVaultName))
        throw new InvalidOperationException("La variable de entorno 'KeyVaultName' no está configurada.");
    builder.Configuration.AddAzureKeyVault(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
}

// Registro de servicios organizado por capas
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddExternalServices(builder.Configuration);

// Registro de Application Insights
if (builder.Environment.IsProduction())
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Configuración obligatoria para Azure App Service
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

// Limpiamos las restricciones usando la propiedad moderna
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedOptions);

// Aplica migraciones automáticamente al arrancar de forma limpia (solo desarrollo)
if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync();
    app.MapOpenApi();
}

// Seeder del usuario admin puede correr en cualquier entorno
await ApplicationDbContextSeed.SeedDefaultUserAsync(app.Services);

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Para que el API sirva el HTML localmente de forma nativa
app.UseStaticFiles();

app.UseCors("AllowLocalClient");

app.UseAuthentication();
app.UseAuthorization();

// Add middlewares global
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Mapeo de todas las rebanadas verticales
app.MapApiEndpoints();

app.Run();
