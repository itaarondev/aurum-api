using Aurum.Application.Features.Files.Commands;
using MediatR;

namespace Aurum.Api.Features.Files.Blob
{
    public static class GetFileSasUriEndpoint
    {
        public static IEndpointRouteBuilder MapGetFileSasUriEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/files/{containerName}/{fileName}", async (string containerName, string fileName, ISender mediator) =>
            {
                var command = new GetFileSasUriCommand(containerName, fileName);

                var result = await mediator.Send(command);

                return Results.Ok(result);
            })
            .WithName("GetFileSasUri")
            .WithTags("Files");

            return endpoints;
        }
    }
}
