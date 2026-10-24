using Aurum.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Aurum.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Registramos MediatR escaneando el ensamblado (Assembly) actual de la capa de Aplicación.
            // Esto le permite a MediatR encontrar automáticamente todos tus Commands y Handlers.
            // Registramos el Pipeline Behavior de validación de forma global
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Registramos automáticamente todos los validadores que hereden de AbstractValidator
            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
