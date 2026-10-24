namespace Aurum.Api.Dtos
{
    public class UploadFileRequestDto
    {
        public string ContainerName { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
    }
}
