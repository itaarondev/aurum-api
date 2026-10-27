using FluentValidation;
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
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Clasificamos el nivel del log inteligentemente para Application Insights
                HandleLogByExceptionType(ex);

                await HandleExceptionAsync(context, ex);
            }
        }

        private void HandleLogByExceptionType(Exception ex)
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
    }
}

// 6. Excepciones de Base de Datos (Entity Framework / SQL)
//case DbUpdateException dbEx:
//    response.StatusCode = (int)HttpStatusCode.Conflict; // Por defecto asumimos conflicto (ej. duplicados)

//    string dbMessage = "Ocurrió un error al guardar los datos en la base de datos.";

//    // Opcional: Puedes inspeccionar la excepción interna (InnerException) para detectar códigos específicos de SQL Server
//    if (dbEx.InnerException != null)
//    {
//        var innerMessage = dbEx.InnerException.Message;

//        // Ejemplo para SQL Server: Violación de índice único / llave duplicada (Error 2627 o 2601)
//        if (innerMessage.Contains("2627") || innerMessage.Contains("2601") || innerMessage.Contains("IX_") || innerMessage.Contains("UNIQUE"))
//        {
//            dbMessage = "Ya existe un registro con estos datos únicos en el sistema.";
//        }
//        // Ejemplo para Llaves Foráneas rotas (Error 547 en SQL Server)
//        else if (innerMessage.Contains("547") || innerMessage.Contains("REFERENCE constraint"))
//        {
//            response.StatusCode = (int)HttpStatusCode.BadRequest;
//            dbMessage = "La operación viola una regla de relación con otros datos existentes.";
//        }
//    }

//    errorResponse = new
//    {
//        status = response.StatusCode,
//        message = dbMessage,
//        errors = (object)null
//    };
//    break;
