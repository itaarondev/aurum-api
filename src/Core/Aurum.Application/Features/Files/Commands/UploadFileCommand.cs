using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Aurum.Application.Features.Files.Commands
{
    public record UploadFileCommand(Stream FileStream, string FileName, string ContainerName) : IRequest<Result<string>>;

    public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
    {
        public UploadFileCommandValidator()
        {
            RuleFor(v => v.FileName)
                .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre del archivo no puede superar los 50 caracteres.");
            RuleFor(v => v.ContainerName)
                .NotEmpty().WithMessage("El nombre del contenedor es obligatorio.")
                .MinimumLength(3).WithMessage("El nombre del contenedor debe tener al menos 3 caracteres.")
                .MaximumLength(30).WithMessage("El nombre del contenedor no puede superar los 30 caracteres.")
                .Matches("^[a-z0-9-]+$").WithMessage("El nombre del contenedor solo puede contener letras minúsculas, números y guiones.");
            RuleFor(v => v.FileStream)
                .NotNull().WithMessage("El contenido del archivo no puede estar vacío o ser nulo.");
        }
    }

    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<string>>
    {
        private readonly IBlobStorageService _blobStorageService;

        public UploadFileCommandHandler(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<Result<string>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            // Generamos un nombre único para evitar colisiones en Azure
            // var uniqueFileName = $"{Guid.NewGuid()}_{request.FileName}";

            var uri = await _blobStorageService.UploadFileAsync(request.FileStream, request.ContainerName, request.FileName);
            return Result<string>.Success(uri);
        }
    }
}
