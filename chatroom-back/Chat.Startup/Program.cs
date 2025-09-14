using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using Chat.Api.Hubs;
using Chat.Api.Infrastructure.Authorization;
using Chat.Api.Mapping;
using Chat.Api.Services;
using Chat.Business.Messaging;
using Chat.Business.Persistance;
using Chat.Repository;
using Chat.Repository.Extensions;
using Chat.Repository.Repositories;
using Chat.Startup.Infrastructure.Azure;
using Chat.Startup.Infrastructure.Json;
using MapsterMapper;
using Chat.Startup.Infrastructure.Authentication;
using Chat.Startup.Infrastructure.Resilience;
using Chat.Startup.Infrastructure.Swagger;
using Chat.Startup.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Npgsql;
using Quartz;
using Serilog;
using Serilog.Events;
using Throw;

namespace Chat.Startup;

/// <summary>
/// Represents the entry point of the application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Represents the entry point of the application.
    /// </summary>
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(applyThemeToRedirectedOutput: true)
            .CreateBootstrapLogger();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        // Logging & Telemetry
        builder.Services.AddSerilog(static (services, lc) => lc
            .ReadFrom.Configuration(services.GetRequiredService<IConfiguration>())
            .ReadFrom.Services(services)
        );

        // API & Swagger
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());

                // required for at least one api offer/{guid}/confirmed, offer/{guid}/ordered
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

                if (!builder.Environment.IsDevelopment())
                {
                    // Trim requests properties with null values to reduce payload size
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                }
            });

        builder.Services.AddRouting(options => options.LowercaseUrls = true);

        builder.Services.AddResponseCaching();

        builder.Services.AddRequestDecompression();
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddApplicationSwaggerGen();

        builder.Services
            .AddSignalR(hubOptions => { hubOptions.EnableDetailedErrors = true; })
            // required to push offer where cyclic references are present
            .AddJsonProtocol(options => { options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; });

        builder.AddHttpProtocolRewriteMiddleware();

        // Authentication & Authorization
        builder.AddOpenIddictAuthentication();
        builder.AddApplicationAuthorization();

        // EF Core: Postgres
        builder.Services.AddDbContextPool<PlatformDbContext>(options =>
        {
            string connectionString = builder.Configuration.GetConnectionString("Database").ThrowIfNull();
            NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionString);

            if (!builder.Environment.IsDevelopment())
            {
                // Use Azure service connector during production
                AzureDatabaseTokenRetriever tokenRetriever = new(new());

                dataSourceBuilder.UsePasswordProvider(
                    _ => tokenRetriever.GetToken().Token,
                    async (_, ct) => (await tokenRetriever.GetTokenAsync(ct)).Token
                );
            }

            options.ConfigureDbContext(dataSourceBuilder);
        });

        // Scheduling: Quartz.NET
        builder.Services.AddQuartz(options =>
        {
            options.UseMicrosoftDependencyInjectionJobFactory();
            options.UseSimpleTypeLoader();
            options.UseInMemoryStore();
        });

        // Resiliency
        builder.Services.AddApplicationResilience();

        // Mapper: Mapster
        builder.Services.AddSingleton<IMapper, Mapper>();
        MappingExtensions.ConfigureStaticMappings();

        // Application code
        builder.Services.AddScoped<MessagingService>();
        builder.Services.AddScoped<IMessagingNotificationHandler, MessagingHubNotificationHandler>();

        // le service de chatrooms
        builder.Services.AddScoped<ChatRoomsService>();

        // le repository de chatrooms
        builder.Services.AddScoped<IChatRoomsPersistance, ChatRoomsRepository>();

        // Application respositories
        builder.Services.AddScoped<IMessagingPersistance, MessagingRepository>();

        WebApplication app = builder.Build();

        app.UseHttpProtocolRewrite();
        app.UseRequestDecompression();
        app.UseResponseCompression();

        // Request Logging
        app.UseSerilogRequestLogging(options =>
        {
            // Customize the message template
            options.MessageTemplate =
                "{RequestScheme} {RequestMethod} {RequestPath} by {RequestClient} responded {StatusCode} in {Elapsed:0.0000} ms";

            // Attach additional properties to the request completion event
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestClient", httpContext.Connection.RemoteIpAddress);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme.ToUpperInvariant());
            };
        });

        app.UseResponseCaching();
        
        app.UseAppSwagger();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        ForwardedHeadersOptions forwardedHeadersOptions = new() { ForwardedHeaders = ForwardedHeaders.All };

        if (app.Configuration.GetSection("ForwardProxies").Get<string[]>()?.Select(IPAddress.Parse).ToArray() is { Length: not 0 } allowedProxies)
        {
            forwardedHeadersOptions.KnownProxies.Clear();
            foreach (IPAddress address in allowedProxies)
                forwardedHeadersOptions.KnownProxies.Add(address);
        }

        // we add CORS but it should not be used because all connections go through the proxy that change the origin
        // of http requests and signalr connections
        if (app.Environment.IsDevelopment())
        {
            // Allow CORS (permissive)
            app.UseCors(static builder =>
            {
                builder.SetIsOriginAllowed(_ => true); // Allow all origins
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
            });
        }

        // app.UseForwardedHeaders(forwardedHeadersOptions);
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHub<MessagingHub>(MessagingHub.HubPath);

        app.MapDefaultControllerRoute();

        // Run application.
        app.Logger.LogInformation("{ApplicationName} v{Version}",
            app.Environment.ApplicationName,
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion
        );

        await app.StartAsync();

        // Migrate database on startup
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await using PlatformDbContext context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await context.MigrateDbAsync();

        await app.WaitForShutdownAsync();
    }
}