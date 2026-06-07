namespace ECS.API.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring Cross-Origin Resource Sharing (CORS).
    /// </summary>
    public static class CorsExtensions
    {
        /// <summary>
        /// The name of the default CORS policy used across the application.
        /// </summary>
        private const string AppCorsPolicy = "AppCorsPolicy";

        /// <summary>
        /// Adds custom CORS services and policies to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddCustomCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(AppCorsPolicy, builder =>
                {
                    builder.WithOrigins("http://localhost:3000")
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            return services;
        }

        /// <summary>
        /// Adds the custom CORS middleware to the application's request pipeline.
        /// </summary>
        /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
        /// <returns>The original <see cref="WebApplication"/> for chaining.</returns>
        public static WebApplication UseCustomCors(this WebApplication app)
        {
            app.UseCors(AppCorsPolicy);
            return app;
        }
    }
}