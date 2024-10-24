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
host.UseSerilog((hostContext, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.ControlledBy(new Serilog.Core.LoggingLevelSwitch(LogEventLevel.Warning))
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Information)

        .WriteTo.Async(a => a.Console(
            outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp} {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}")
        )
        .WriteTo.Async(a => a.File("logs/logs-.log", rollingInterval: RollingInterval.Day,
            outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {ClientIp} {ThreadId} {Message:lj} <p:{SourceContext}>{NewLine}{Exception}")
        )
        .Enrich.FromLogContext()
        .Enrich.WithClientIp()    // Adds client IP information
        .Enrich.WithThreadId()    // Adds ThreadId for better tracking
        .Enrich.WithMachineName() // Adds MachineName
        .ReadFrom.Configuration(hostContext.Configuration)
        .ReadFrom.Services((IServiceProvider)services);
});

// MIDDLEWARE
var app = builder.Build();

app.UseSerilogRequestLogging(); // Log all HTTP requests

app.UseRouting(); // Ensure routing is enabled before using middleware

// Health Check endpoint at `/health`
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Use Ocelot as middleware
await app.UseOcelot();

// Run the application
await app.RunAsync();
