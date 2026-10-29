using Aurum.Api.Features.Auth.Google;
using Aurum.Api.Features.Auth.Login;
using Aurum.Api.Features.Auth.RefreshToken;
using Aurum.Api.Features.Auth.Register;
using Aurum.Api.Features.Files.Blob;
using Aurum.Api.Features.Files.Container;

namespace Aurum.Api.Extensions
{
    public static class EndpointRoutingExtensions
    {
        public static WebApplication MapApiEndpoints(this WebApplication app)
        {
            // Agrupamos y mapeamos todas las rebanadas verticales aquí
            app.MapRegisterEndpoint();
            app.MapLoginEndpoint();
            app.MapCreateContainerEndpoint();
            app.MapDeleteContainerEndpoint();
            app.MapUploadFileEndpoint();
            app.MapDeleteFileEndpoint();
            app.MapGetFileSasUriEndpoint();
            app.MapRefreshTokenEndpoint();
            app.MapGoogleAuthEndpoints();

            return app;
        }
    }
}
