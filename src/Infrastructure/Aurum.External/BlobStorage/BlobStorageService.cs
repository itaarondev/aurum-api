using Aurum.Application.Common.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.StaticFiles;

namespace Aurum.External.BlobStorage
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }
        public async Task CreateContainerAsync(string containerName)
        {
            // 1. Obtener una referencia al contenedor que deseas crear
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // 2. Crear el contenedor de forma asíncrona si es que aún no existe
            // Debemos especificar el nivel de acceso, por ejemplo: 
            // - PublicAccessType.None (para que sea 100% privado)
            // - PublicAccessType.Blob (si solo quieres que el archivo individual sea accesible por su enlace directo, sin listar la carpeta)
            // - PublicAccessType.BlobContainer (cualquier persona pueda listar y ver todos los archivos del contenedor en internet de forma pública)
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        }

        public async Task DeleteContainerAsync(string containerName)
        {
            // 1. Obtener una referencia al contenedor que deseas crear
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // 2. Eliminar el contenedor de forma asíncrona si es que existe
            await containerClient.DeleteIfExistsAsync();
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string containerName, string fileName)
        {
            // Obtener una referencia al contenedor que deseas crear
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Obtener la referencia del archivo
            var blobClient = containerClient.GetBlobClient(fileName);

            // Sube el archivo (overwrite: true sobrescribe si ya existe uno con el mismo nombre)
            await blobClient.UploadAsync(fileStream, overwrite: true);

            /* opcion para subir con metadatos
             
                var metadata = new Dictionary<string, string>
                {
                    { "title", title },
                    { "description", description }
                };

                var uploadOptions = new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    Metadata = metadata
                };

                await blobClient.UploadAsync(fileStream, uploadOptions);             
             */

            // Retorna la URI pública o interna del archivo almacenado
            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteFileAsync(string containerName, string fileName)
        {
            // Obtener una referencia al contenedor que deseas crear
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Obtener la referencia del archivo
            var blobClient = containerClient.GetBlobClient(fileName);

            // Borra el archivo si existe y retorna true; si no existía, retorna false
            var response = await blobClient.DeleteIfExistsAsync();

            return response.Value;
        }

        public async Task<string> GetFileSasUriAsync(string containerName, string fileName, TimeSpan expirationTime)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            // 1. Verificar si el blob realmente existe antes de generar la SAS
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"El archivo '{fileName}' no fue encontrado en el contenedor '{containerName}'.");
            }

            // 2. Verificar que la cuenta tenga soporte para generar SAS por clave compartida
            // (Esto requiere que tu BlobServiceClient se haya inicializado con la Connection String completa que incluye AccountKey)
            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("La cuenta de almacenamiento no está configurada con credenciales de clave compartida para generar tokens SAS.");
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream"; // Valor por defecto si no lo reconoce
            }

            // 3. Configurar los permisos del token SAS (en este caso, solo lectura)
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b", // "b" significa Blob individual
                ExpiresOn = DateTimeOffset.UtcNow.Add(expirationTime)
            };

            // Otorgar permiso de lectura
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // 👉 AGREGAR ESTA LÍNEA PARA FORZAR VISUALIZACIÓN EN VEZ DE DESCARGA:
            sasBuilder.ContentType = contentType;
            sasBuilder.ContentDisposition = "inline; filename=" + fileName;

            // 4. Generar la URI firmada utilizando las credenciales de la cuenta
            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            return sasUri.ToString();
        }

    }
}
