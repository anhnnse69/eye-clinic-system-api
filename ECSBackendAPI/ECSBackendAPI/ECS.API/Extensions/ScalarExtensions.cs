using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace ECS.API.Extensions
{
    public static class ScalarExtensions
    {
        public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, _) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "ECS Backend API",
                        Version = "v1",
                        Description = "Medical Appointment & Clinic Management System API",
                        Contact = new OpenApiContact
                        {
                            Name = "ECS Team",
                            Email = "support@ECS.vn"
                        }
                    };
                    return Task.CompletedTask;
                });

                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            return services;
        }

        public static WebApplication UseApiDocumentation(this WebApplication app)
        {
            app.MapOpenApi();

            app.MapScalarApiReference(options =>
            {
                options.Title = "ECS Backend API";
                options.Theme = ScalarTheme.Purple;
                options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
                options.Authentication = new ScalarAuthenticationOptions
                {
                    PreferredSecuritySchemes = ["Bearer"]
                };
            });

            return app;
        }
    }

}