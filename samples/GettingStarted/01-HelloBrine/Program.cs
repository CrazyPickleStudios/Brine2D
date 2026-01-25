using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Rendering;
using Brine2D.SDL;
using HelloBrine;

// Create the game application builder
// Similar to WebApplicationBuilder in ASP.NET Core
var builder = GameApplication.CreateBuilder(args);

// Add Brine2D with sensible defaults (SDL3 backend, GPU rendering, input)
// This is equivalent to calling:
//   - AddBrineCore()
//   - AddBrineEngine()
//   - AddSDL3ApplicationLifetime()
//   - AddSDL3Input()
//   - AddSDL3Rendering()
builder.Services.AddBrine2D(options =>
{
    options.WindowTitle = "01 - Hello Brine";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
    // Defaults: GraphicsBackend.GPU with VSync enabled
});

// Register your game scene (like a Controller in ASP.NET)
builder.Services.AddScene<GameScene>();

// Build the game application
var game = builder.Build();

// Run the game starting with GameScene
await game.RunAsync<GameScene>();