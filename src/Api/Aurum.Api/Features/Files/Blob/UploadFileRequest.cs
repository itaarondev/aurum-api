namespace Aurum.Api.Features.Files.Blob
{
    public record UploadFileRequest(string ContainerName, IFormFile File);
}
