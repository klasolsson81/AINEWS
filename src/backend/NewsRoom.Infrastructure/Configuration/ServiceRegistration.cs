using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsRoom.Core.Interfaces;
using NewsRoom.Infrastructure.Images;
using NewsRoom.Infrastructure.Mocks;
using NewsRoom.Infrastructure.News;
using NewsRoom.Infrastructure.Persistence;
using NewsRoom.Infrastructure.ScriptGeneration;
using NewsRoom.Infrastructure.Services;

namespace NewsRoom.Infrastructure.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddNewsRoomServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Repository (in-memory for now, PostgreSQL later)
        services.AddSingleton<IBroadcastRepository, InMemoryBroadcastRepository>();

        // Orchestrator
        services.AddScoped<IBroadcastOrchestrator, BroadcastOrchestratorService>();

        // HTTP client factory
        services.AddHttpClient();

        // Editorial Image Extractor — real OG extraction or mock
        var editorialEnabled = GetConfigValue(configuration, "EDITORIAL_IMAGE_ENABLED") ?? "true";
        if (editorialEnabled == "true")
            services.AddScoped<IEditorialImageExtractor, OgImageExtractor>();
        else
            services.AddScoped<IEditorialImageExtractor, MockEditorialImageExtractor>();

        // News Source — switch between mock and RSS
        var newsProvider = GetConfigValue(configuration, "NEWS_PROVIDER") ?? "mock";
        if (newsProvider == "rss")
            services.AddScoped<INewsSource, RssNewsSource>();
        else
            services.AddScoped<INewsSource, MockNewsSource>();

        // Script Generator — switch between mock and OpenAI
        var llmProvider = GetConfigValue(configuration, "LLM_PROVIDER") ?? "mock";
        if (llmProvider == "openai")
            services.AddScoped<IScriptGenerator, OpenAiScriptGenerator>();
        else
            services.AddScoped<IScriptGenerator, MockScriptGenerator>();

        // TTS — mock by default
        services.AddScoped<ITtsProvider, MockTtsProvider>();

        // Avatar — mock by default
        services.AddScoped<IAvatarGenerator, MockAvatarGenerator>();

        // B-Roll — mock by default
        services.AddScoped<IBRollProvider, MockBRollProvider>();
        services.AddScoped<IBRollOrchestrator, MockBRollOrchestrator>();
        services.AddScoped<IMapGenerator, MockMapGenerator>();
        services.AddScoped<IDataGraphicGenerator, MockDataGraphicGenerator>();

        // Video composer — mock by default
        services.AddScoped<IVideoComposer, MockVideoComposer>();

        // Storage — mock by default
        services.AddScoped<IStorageProvider, MockStorageProvider>();

        // Messaging — mock by default
        services.AddScoped<IMessagePublisher, MockMessagePublisher>();

        return services;
    }

    private static string? GetConfigValue(IConfiguration? configuration, string key)
    {
        return configuration?[key] ?? Environment.GetEnvironmentVariable(key);
    }
}
