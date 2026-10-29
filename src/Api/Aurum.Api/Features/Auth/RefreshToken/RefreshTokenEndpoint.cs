using Aurum.Application.Features.Auth.RefreshToken;
using MediatR;

namespace Aurum.Api.Features.Auth.RefreshToken
{
    public static class RefreshTokenEndpoint
    {
        public static IEndpointRouteBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/refresh-token", async (ISender mediator, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                // 1. Extraemos el Refresh Token de la cookie HttpOnly que manda el navegador
                if (!httpContext.Request.Cookies.TryGetValue("Aurum.RefreshToken", out var refreshTokenCookie) || string.IsNullOrEmpty(refreshTokenCookie))
                {
                    return Results.BadRequest(new { succeeded = false, errors = new[] { "Refresh token no encontrado en las cookies." } });
                }

                var command = new RefreshTokenCommand(refreshTokenCookie);
                var result = await mediator.Send(command, cancellationToken);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(result);
                }

                // 2. INYECTAMOS LOS NUEVOS TOKENS EN LAS COOKIES (Igual que en el Login)
                httpContext.Response.Cookies.Append("Aurum.AccessToken", result.Data.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(1) // O lo que tengas configurado en tus settings
                });

                httpContext.Response.Cookies.Append("Aurum.RefreshToken", result.Data.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return Results.Ok(new
                {
                    succeeded = true,
                    message = "Tokens refrescados exitosamente"
                });
            })
            .WithName("RefreshToken")
            .WithTags("Auth")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            return app;
        }
    }
}