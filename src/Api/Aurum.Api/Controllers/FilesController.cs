using Aurum.Api.Dtos;
using Aurum.Application.Features.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aurum.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly ISender _mediator;
        public FilesController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("container")]
        public async Task<IActionResult> CreateContainer([FromBody] CreateContainerCommand command)
        {
            await _mediator.Send(command);

            return Ok(new { Message = $"Contenedor '{command.ContainerName}' creado o verificado exitosamente." });
        }

        [HttpDelete("container/{containerName}")]
        public async Task<IActionResult> DeleteContainer(string containerName)
        {
            var command = new DeleteContainerCommand(containerName);

            await _mediator.Send(command);

            return Ok(new { Message = $"Contenedor '{containerName}' eliminado exitosamente." });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(UploadFileRequestDto requestDto)
        {
            using var stream = requestDto.File.OpenReadStream();

            var command = new UploadFileCommand(stream, requestDto.File.FileName, requestDto.ContainerName);            

            var fileUrl = await _mediator.Send(command);

            return Ok(new { Message = "Archivo subido exitosamente", Url = fileUrl });
        }

        [HttpDelete("{containerName}/{fileName}")]
        public async Task<IActionResult> DeleteFile(string containerName, string fileName)
        {
            var command = new DeleteFileCommand(containerName, fileName);

            var deleted = await _mediator.Send(command);

            if (!deleted)
                return NotFound("El archivo no fue encontrado en el contenedor especificado.");

            return Ok(new { Message = "Archivo eliminado exitosamente" });
        }

        [HttpGet("{containerName}/{fileName}")]
        public async Task<ActionResult<string>> GetUriFile(string containerName, string fileName)
        {
            var command = new GetFileSasUriCommand(containerName, fileName);

            var sasUri = await _mediator.Send(command);
            
            return Ok(new { SasUri = sasUri });
        }


    }
}
