using ECS.API.Middlewares;

namespace ECS.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
            return app;
        }
    }
}
