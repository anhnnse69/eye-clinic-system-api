namespace ECS.API.Extensions
{
    using System.Text.Json.Serialization;

    public static class ControllerExtensions
    {
        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddRouting(options => options.LowercaseUrls = true);
            return services;
        }
    }

}