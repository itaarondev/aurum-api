using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aurum.Application.Features.Files.Commands
{
    public record DeleteFileCommand(string ContainerName, string FileName) : IRequest<Result<string>>;

    public class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
    {
        public DeleteFileCommandValidator()
        {
            RuleFor(v => v.ContainerName)
                .NotEmpty().WithMessage("El nombre del contenedor es obligatorio.")
                .MinimumLength(3).WithMessage("El nombre del contenedor debe tener al menos 3 caracteres.")
                .MaximumLength(30).WithMessage("El nombre del contenedor no puede superar los 63 caracteres.")
                .Matches("^[a-z0-9-]+$").WithMessage("El nombre del contenedor solo puede contener letras minúsculas, números y guiones.");

            RuleFor(v => v.FileName)
                .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre del archivo no puede superar los 50 caracteres.");
        }
    }

    public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result<string>>
    {
        private readonly IBlobStorageService _blobStorageService;

        public DeleteFileCommandHandler(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<Result<string>> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
        {
            var delete = await _blobStorageService.DeleteFileAsync(request.ContainerName, request.FileName);
            if (!delete)
            {
                return Result<string>.Failure(new[] { "El archivo no fue encontrado en el contenedor especificado." });
            }
            return Result<string>.Success("Archivo eliminado exitosamente");
        }
    }
}
