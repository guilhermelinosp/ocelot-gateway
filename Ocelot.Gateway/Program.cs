using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var host = builder.Host;
var services = builder.Services;

// SERVICES
services.AddHealthChecks(); // Add other specific health checks if needed (e.g., for databases, external services)

// Ocelot with Polly support
services
    .AddOcelot()
    .AddPolly();

// CONFIGURATION
configuration.AddJsonFile("gateway.json", optional: false, reloadOnChange: true);

// HOST (Serilog setup with better configuration)
host.UseSerilog((host, services, logging) =>
{
    logging
        .MinimumLevel.Warning()
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Information)

        .WriteTo.Async(write =>
        {
            write.Console(
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp}  {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}");
            write.File("logs/.log", rollingInterval: RollingInterval.Day,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp}  {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}");
        })
        .ReadFrom.Configuration(host.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});
// 

// MIDDLEWARE
var app = builder.Build();

app.UseSerilogRequestLogging(); // Log all HTTP requests

app.UseRouting(); // Ensure routing is enabled before using middleware

// Health Check endpoint at `/health`
app.UseEndpoints(endpoints =>
{
    // Health Check endpoint
    endpoints.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Other endpoints...
});

// Use Ocelot as middleware
await app.UseOcelot();

// Run the application
await app.RunAsync();
