using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Aurum.Application.Features.Files.Commands
{    
    public record CreateContainerCommand(string ContainerName) : IRequest<Result<string>>;

    public class CreateContainerCommandValidator : AbstractValidator<CreateContainerCommand>
    {
        public CreateContainerCommandValidator()
        {
            RuleFor(v => v.ContainerName)
                .NotEmpty().WithMessage("El nombre del contenedor es obligatorio.")
                .MinimumLength(3).WithMessage("El nombre del contenedor debe tener al menos 3 caracteres.")
                .MaximumLength(30).WithMessage("El nombre del contenedor no puede superar los 30 caracteres.")
                .Matches("^[a-z0-9-]+$").WithMessage("El nombre del contenedor solo puede contener letras minúsculas, números y guiones.");
        }
    }

    public class CreateContainerCommandHandler : IRequestHandler<CreateContainerCommand, Result<string>>
    {
        private readonly IBlobStorageService _blobStorageService;

        public CreateContainerCommandHandler(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<Result<string>> Handle(CreateContainerCommand request, CancellationToken cancellationToken)
        {
            // Llamamos al servicio de infraestructura para crear el contenedor
            await _blobStorageService.CreateContainerAsync(request.ContainerName);

            return Result<string>.Success($"Contenedor '{request.ContainerName}' creado exitosamente.");
        }
    }
}
