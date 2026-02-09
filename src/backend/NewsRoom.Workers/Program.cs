using NewsRoom.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Register all NewsRoom services (mock providers by default)
builder.Services.AddNewsRoomServices();

var host = builder.Build();
host.Run();
