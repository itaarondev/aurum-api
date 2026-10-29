
using Aurum.Application.Features.Files.DeleteContainer;
using MediatR;

namespace Aurum.Api.Features.Files.Container
{
    public static class DeleteContainerEndpoint
    {
        public static IEndpointRouteBuilder MapDeleteContainerEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapDelete("/api/files/container/{containerName}", async (string containerName, ISender mediator) =>
            {
                var command = new DeleteContainerCommand(containerName);

                var result = await mediator.Send(command);

                return Results.Ok(result);
            })
            .WithName("DeleteContainer")
            .WithTags("Files");

            return endpoints;
        }
    }
}
