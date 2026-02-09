using Microsoft.Extensions.DependencyInjection;
using NewsRoom.Core.Interfaces;
using NewsRoom.Infrastructure.Mocks;
using NewsRoom.Infrastructure.Persistence;
using NewsRoom.Infrastructure.Services;

namespace NewsRoom.Infrastructure.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddNewsRoomServices(this IServiceCollection services)
    {
        // Repository (in-memory for now, PostgreSQL later)
        services.AddSingleton<IBroadcastRepository, InMemoryBroadcastRepository>();

        // Orchestrator
        services.AddScoped<IBroadcastOrchestrator, BroadcastOrchestratorService>();

        // Register mock providers as defaults
        // These will be replaced by real providers via configuration later
        services.AddScoped<INewsSource, MockNewsSource>();
        services.AddScoped<IScriptGenerator, MockScriptGenerator>();
        services.AddScoped<ITtsProvider, MockTtsProvider>();
        services.AddScoped<IAvatarGenerator, MockAvatarGenerator>();
        services.AddScoped<IBRollProvider, MockBRollProvider>();
        services.AddScoped<IBRollOrchestrator, MockBRollOrchestrator>();
        services.AddScoped<IEditorialImageExtractor, MockEditorialImageExtractor>();
        services.AddScoped<IMapGenerator, MockMapGenerator>();
        services.AddScoped<IDataGraphicGenerator, MockDataGraphicGenerator>();
        services.AddScoped<IVideoComposer, MockVideoComposer>();
        services.AddScoped<IStorageProvider, MockStorageProvider>();
        services.AddScoped<IMessagePublisher, MockMessagePublisher>();

        return services;
    }
}
