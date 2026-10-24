using Aurum.Application.Common.Interfaces;
using Aurum.External.BlobStorage;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aurum.External
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Registro inteligente del BlobServiceClient (Desarrollo vs Producción)
            services.AddSingleton(sp =>
            {
                var connectionString = configuration.GetConnectionString("AzureStorage");

                // Si existe la cadena de conexión (Desarrollo local / User Secrets)
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return new BlobServiceClient(connectionString);
                }

                // Si no hay cadena, asumimos Producción (Identidad Administrada)
                var accountName = configuration["StorageAccountName"];
                if (string.IsNullOrEmpty(accountName))
                {
                    throw new InvalidOperationException("No se encontró la cadena de conexión 'AzureStorage' ni el nombre de la cuenta para Identidad Administrada.");
                }

                var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");
                return new BlobServiceClient(blobUri, new DefaultAzureCredential());
            });

            // 2. Registramos el servicio de aplicación vinculándolo con su implementación
            services.AddScoped<IBlobStorageService, BlobStorageService>();

            return services;
        }
    }
}

/*
// 1. Leemos la cadena de conexión desde la configuración
var connectionString = configuration.GetConnectionString("AzureBlobStorage");

// 2. Registramos el BlobServiceClient como Singleton (se crea una sola vez)
services.AddSingleton(new BlobServiceClient(connectionString));

// 3. Registramos el servicio de aplicación vinculándolo con su implementación
services.AddScoped<IBlobStorageService, BlobStorageService>();

return services; 
 */
