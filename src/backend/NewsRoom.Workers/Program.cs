using NewsRoom.Infrastructure.Configuration;
using NewsRoom.Workers.Infrastructure;
using NewsRoom.Workers.Workers;
using Serilog;

// Configure Serilog early so all startup messages are captured
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "NewsRoom.Workers")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/newsroom-workers-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting NewsRoom Workers host");

    var builder = Host.CreateApplicationBuilder(args);

    // Use Serilog as the logging provider
    builder.Services.AddSerilog();

    // Register the shared RabbitMQ connection as a singleton
    builder.Services.AddSingleton<RabbitMqConnection>();

    // Register all NewsRoom domain services (mocks by default, real providers via config)
    builder.Services.AddNewsRoomServices(builder.Configuration);

    // Register all worker background services
    builder.Services.AddHostedService<TtsWorker>();
    builder.Services.AddHostedService<AvatarWorker>();
    builder.Services.AddHostedService<BRollWorker>();
    builder.Services.AddHostedService<CompositionWorker>();

    var host = builder.Build();

    Log.Information("NewsRoom Workers host built successfully. Starting workers...");

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NewsRoom Workers host terminated unexpectedly");
}
finally
{
    Log.Information("NewsRoom Workers host shutting down");
    Log.CloseAndFlush();
}
