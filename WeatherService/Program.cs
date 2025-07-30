using WeatherService.Services;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddGrpc();

        /* SERVICE LIFETIME 
         * 
         * Scoped Services (created once per gRPC request/call) - DEFAULT/RECOMMENDED
         * Perfect for: Database contexts, request-specific services, user context
         * Each gRPC call gets its own fresh instance
         * - builder.Services.AddScoped<WeatherForecastService>();
         * 
         * Singleton Services (created once for the entire application lifetime)
         * Perfect for: Configuration, caches, thread-safe services
         * WARNING: Same instance shared across ALL clients and calls!
         * - builder.Services.AddSingleton<WeatherForecastService>();
         *
         * Transient Services (created every time they're requested)
         * Perfect for: Lightweight services, stateless operations, external API calls
         * New instance created each time the service is injected (overkill for gRPC services)
         * - builder.Services.AddTransient<WeatherForecastService>();
         */

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<WeatherForecastService>();

        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. " +
                             "To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        app.Run();
    }
}
