using Aurum.Application.Features.Auth.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aurum.Api.Features.Auth.Login
{
    public static class LoginEndpoint
    {
        public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/auth/login", async ([FromBody] LoginRequest request, ISender mediator, HttpContext httpContext) =>
            {
                var command = new LoginCommand(request.Email, request.Password);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(result);
                }

                // 1. Extraemos los tokens que tu JwtTokenService y MediatR ya generaron exitosamente
                var accessToken = result.Data.AccessToken;
                var refreshToken = result.Data.RefreshToken;

                // 2. Inyectamos el Access Token (JWT con roles y claims) en una cookie HttpOnly segura
                httpContext.Response.Cookies.Append("Aurum.AccessToken", accessToken, new CookieOptions
                {
                    HttpOnly = true,                  // Bloquea el acceso por JavaScript (Protección contra XSS)
                    Secure = true,                    // Obliga a que viaje únicamente por HTTPS (Requerido en Azure)
                    SameSite = SameSiteMode.None,     // Crucial si tu Front y Back están en dominios/App Services distintos
                    Expires = DateTimeOffset.UtcNow.AddMinutes(1) // Coincide con la vida de tu Access Token
                });

                // 3. Hacemos lo mismo con el Refresh Token para protegerlo bajo la misma regla
                httpContext.Response.Cookies.Append("Aurum.RefreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(1) // La duración de tu refresh token
                });

                return Results.Ok(new
                {
                    succeeded = true,
                    message = "Autenticación exitosa",
                    email = result.Data.Email
                });
            })
            .WithName("LoginUser")
            .WithTags("Auth");

            return endpoints;
        }
    }
}
