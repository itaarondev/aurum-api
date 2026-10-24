using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Aurum.Api.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = context.Response;

            // Estructura base de la respuesta de error
            object errorResponse;

            switch (exception)
            {
                // 1. Errores de validación (FluentValidation) -> 400 Bad Request
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;

                    var validationErrors = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(e => e.ErrorMessage).ToArray()
                        );

                    errorResponse = new
                    {
                        status = (int)HttpStatusCode.BadRequest,
                        message = "Falló la validación de los datos de entrada.",
                        errors = validationErrors
                    };
                    break;

                // 2. Recurso no encontrado (Ej. NotFoundException personalizada) -> 404 Not Found
                case KeyNotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse = new
                    {
                        status = (int)HttpStatusCode.NotFound,
                        message = notFoundEx.Message,
                        errors = (object)null
                    };
                    break;

                // 3. Conflictos de negocio (Ej. el contenedor ya existe) -> 409 Conflict
                case InvalidOperationException conflictEx when conflictEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase):
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse = new
                    {
                        status = (int)HttpStatusCode.Conflict,
                        message = exception.Message,
                        errors = (object)null
                    };
                    break;

                // 4. No autorizado / Sin credenciales válidas -> 401 Unauthorized
                case UnauthorizedAccessException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse = new
                    {
                        status = (int)HttpStatusCode.Unauthorized,
                        message = string.IsNullOrEmpty(unauthorizedEx.Message) ? "No autorizado." : unauthorizedEx.Message,
                        errors = (object)null
                    };
                    break;

                // 5. Cualquier otro error inesperado -> 500 Internal Server Error
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse = new
                    {
                        status = (int)HttpStatusCode.InternalServerError,
                        message = "Ocurrió un error interno en el servidor.",
                        // Opcional: puedes exponer exception.Message solo en ambiente de desarrollo
                        details = exception.Message
                    };
                    break;
            }

            var result = JsonSerializer.Serialize(errorResponse);
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
