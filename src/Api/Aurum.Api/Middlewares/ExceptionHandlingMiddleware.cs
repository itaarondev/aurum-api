using FluentValidation;
using Microsoft.ApplicationInsights;
using System.Net;
using System.Text.Json;

namespace Aurum.Api.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // TELEMETRY CLIENT: Inyección nativa en Azure Application Insights (Para Requests)
            //var telemetryClient = context.RequestServices.GetService<TelemetryClient>();
            //if (telemetryClient != null && userId != "Anonymous")
            //{
            //    telemetryClient.Context.User.AuthenticatedUserId = userId;
            //}

            var info = SetModelInfoErrorTelemetry(context);

            // Creamos un ámbito de log (Log Scope) para que toda esta traza viaje junta en los logs
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["UserId"] = info.userId,
                ["UserEmail"] = info.userEmail,
                ["TokenJti"] = info.tokenJti,
                ["UserAgent"] = info.userAgent,
                ["ClientIp"] = info.clientIp,
                ["TraceId"] = context.TraceIdentifier
            }))
            {
                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    // Clasificamos el nivel del log inteligentemente para Application Insights
                    HandleLogByExceptionType(ex, context, info);

                    await HandleExceptionAsync(context, ex);
                }
            }
        }

        private void HandleLogByExceptionType(Exception ex, HttpContext context, ModelInfoError info)
        {   
            switch (ex)
            {
                case ValidationException:
                case KeyNotFoundException:
                case UnauthorizedAccessException:
                    // Errores de cliente o negocio esperados -> Warning (No ensucian las alertas de 500)
                    _logger.LogWarning(ex, "Advertencia de negocio/cliente: {Message}", ex.Message);
                    break;

                case InvalidOperationException conflictEx when conflictEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase):
                    _logger.LogWarning(ex, "Conflicto de recurso existente: {Message}", ex.Message);
                    break;

                default:
                    // Errores internos reales del servidor -> Error Crítico (Va directo a las alertas de Application Insights)
                    _logger.LogError(ex, "Ocurrió un error interno no controlado: {Message}", ex.Message);
                    SetTelemetryPersonalized(context, info, ex);
                    break;
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = context.Response;

            string message = string.Empty;
            List<string> errorList = new();

            switch (exception)
            {
                // 1. Errores de validación (FluentValidation) -> 400 Bad Request
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    message = "Falló la validación de los datos de entrada.";
                    errorList = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
                    break;

                // 2. Recurso no encontrado -> 404 Not Found
                case KeyNotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    message = notFoundEx.Message;
                    errorList.Add(notFoundEx.Message);
                    break;

                // 3. Conflictos de negocio -> 409 Conflict
                case InvalidOperationException conflictEx when conflictEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase):
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    message = conflictEx.Message;
                    errorList.Add(conflictEx.Message);
                    break;

                // 4. No autorizado -> 401 Unauthorized
                case UnauthorizedAccessException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    message = string.IsNullOrEmpty(unauthorizedEx.Message) ? "No autorizado." : unauthorizedEx.Message;
                    errorList.Add(message);
                    break;

                // 5. Error interno inesperado -> 500 Internal Server Error
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    message = "Ocurrió un error interno en el servidor.";

                    var isDevelopment = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
                    errorList.Add(isDevelopment ? exception.Message : message);
                    break;
            }

            var errorResult = new
            {
                succeeded = false,
                message = message,
                errors = errorList
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Serialize(errorResult, options);
            await context.Response.WriteAsync(result, context.RequestAborted);
        }
    

        private ModelInfoError SetModelInfoErrorTelemetry(HttpContext context)
        {
            // Extraemos la información de auditoría del usuario autenticado (si lo está)
            string userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                         ?? context.User?.FindFirst("sub")?.Value
                         ?? "Anonymous";

            string userEmail = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                          ?? context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value
                          ?? context.User?.FindFirst("email")?.Value
                          ?? "N/A";

            string tokenJti = context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value
                           ?? context.User?.FindFirst("jti")?.Value
                           ?? "N/A";

            // Extraemos información del dispositivo y red desde los headers HTTP
            string userAgent = context.Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrEmpty(userAgent)) userAgent = "N/A";

            string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "N/A";

            return new ModelInfoError
            {
                userId = userId,
                userEmail = userEmail,
                tokenJti = tokenJti,
                userAgent = userAgent,
                clientIp = clientIp
            };
        }
        private void SetTelemetryPersonalized(HttpContext context, ModelInfoError info, Exception ex)
        {
            var telemetryClient = context.RequestServices.GetService<TelemetryClient>();
            if (telemetryClient != null)
            {
                var exceptionTelemetry = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(ex)
                {
                    SeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error
                };

                exceptionTelemetry.Context.User.AuthenticatedUserId = info.userId;
                exceptionTelemetry.Properties["UserId"] = info.userId;
                exceptionTelemetry.Properties["UserEmail"] = info.userEmail;
                exceptionTelemetry.Properties["TokenJti"] = info.tokenJti;
                exceptionTelemetry.Properties["UserAgent"] = info.userAgent;
                exceptionTelemetry.Properties["ClientIp"] = info.clientIp;

                telemetryClient.TrackException(exceptionTelemetry);
            }
        }
        private struct ModelInfoError()
        {
            public string userId { get; set; } = string.Empty;
            public string userEmail { get; set; } = string.Empty;
            public string tokenJti { get; set; } = string.Empty;
            public string userAgent { get; set; } = string.Empty;
            public string clientIp { get; set; } = string.Empty;
        }
    }
}