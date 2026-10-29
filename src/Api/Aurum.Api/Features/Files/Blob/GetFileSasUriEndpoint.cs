using Aurum.Application.Features.Files.GetFileSasUri;
using MediatR;

namespace Aurum.Api.Features.Files.Blob
{
    public static class GetFileSasUriEndpoint
    {
        public static IEndpointRouteBuilder MapGetFileSasUriEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/files/{containerName}/{fileName}", async (string containerName, string fileName, ISender mediator) =>
            {
                var command = new GetFileSasUriQuery(containerName, fileName);

                var result = await mediator.Send(command);

                return Results.Ok(result);
            })
            .WithName("GetFileSasUri")
            .WithTags("Files")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            return endpoints;
        }
    }
}
