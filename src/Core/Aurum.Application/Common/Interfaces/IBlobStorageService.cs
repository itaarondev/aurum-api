namespace Aurum.Application.Common.Interfaces
{
    public interface IBlobStorageService
    {
        Task CreateContainerAsync(string containerName);
        Task DeleteContainerAsync(string containerName);
        Task<string> UploadFileAsync(Stream fileStream, string containerName, string fileName);
        Task<bool> DeleteFileAsync(string containerName, string fileName);
        Task<string> GetFileSasUriAsync(string containerName, string fileName, TimeSpan expirationTime);
    }
}
