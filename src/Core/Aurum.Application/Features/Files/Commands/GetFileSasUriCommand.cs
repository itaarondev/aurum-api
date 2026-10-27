using Aurum.Application.Common.Interfaces;
using Aurum.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Aurum.Application.Features.Files.Commands
{
    public record GetFileSasUriCommand(string ContainerName, string FileName) : IRequest<Result<string>>;

    public class GetFileSasUriCommandValidator : AbstractValidator<GetFileSasUriCommand>
    {
        public GetFileSasUriCommandValidator()
        {
            RuleFor(x => x.ContainerName)
                .NotEmpty().WithMessage("El nombre del contenedor es obligatorio.")
                .MaximumLength(30).WithMessage("El nombre del contenedor no puede superar los 30 caracteres.");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre del archivo no puede superar los 50 caracteres.");
        }
    }

    public class GetFileSasUriCommandHandler : IRequestHandler<GetFileSasUriCommand, Result<string>>
    {
        private readonly IBlobStorageService _blobStorageService;

        public GetFileSasUriCommandHandler(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<Result<string>> Handle(GetFileSasUriCommand request, CancellationToken cancellationToken)
        {
            var uri = await _blobStorageService.GetFileSasUriAsync(request.ContainerName, request.FileName, TimeSpan.FromMinutes(1));
            return Result<string>.Success(uri);
        }
    }

}
