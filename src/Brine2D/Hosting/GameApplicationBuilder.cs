using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Brine2D.Engine;

namespace Brine2D.Hosting
{
    /// <summary>
    /// Builder for configuring and creating a game application.
    /// Similar to WebApplicationBuilder in ASP.NET.
    /// </summary>
    public class GameApplicationBuilder
    {
        private readonly HostApplicationBuilder _hostBuilder;

        internal GameApplicationBuilder(
            string[] args, 
            HostApplicationBuilderSettings? settings = null,
            bool isSlim = false)
        {
            _hostBuilder = settings != null 
                ? Host.CreateApplicationBuilder(settings)
                : Host.CreateApplicationBuilder(args);

            // Add core engine services (always needed)
            Services.AddBrineEngine();

            // Only add defaults if NOT slim
            if (!isSlim)
            {
                // Configure default settings - use ASP.NET conventions
                Configuration.AddJsonFile("gamesettings.json", optional: true, reloadOnChange: true);
                Configuration.AddJsonFile($"gamesettings.{System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true);

                // Add default logging
                Logging.AddConsole();
                Logging.SetMinimumLevel(LogLevel.Information);
            }
        }

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        public IServiceCollection Services => _hostBuilder.Services;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public ConfigurationManager Configuration => _hostBuilder.Configuration;

        /// <summary>
        /// Gets the logging builder.
        /// </summary>
        public ILoggingBuilder Logging => _hostBuilder.Logging;

        /// <summary>
        /// Gets the host environment.
        /// </summary>
        public IHostEnvironment Environment => _hostBuilder.Environment;

        /// <summary>
        /// Builds the game application.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when required services are not registered.
        /// </exception>
        public GameApplication Build()
        {
            ValidateConfiguration();
            
            var host = _hostBuilder.Build();
            return new GameApplication(host);
        }

        /// <summary>
        /// Validates that all required services are registered.
        /// </summary>
        private void ValidateConfiguration()
        {
            using var serviceProvider = Services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = false,
                ValidateOnBuild = false
            });

            var errors = new List<string>();

            // Check for core engine services
            if (serviceProvider.GetService<GameEngine>() == null)
            {
                errors.Add("IGameEngine is not registered. Did you forget to call AddBrine2D()?");
            }

            if (serviceProvider.GetService<GameLoop>() == null)
            {
                errors.Add("IGameLoop is not registered. Did you forget to call AddBrine2D()?");
            }

            if (serviceProvider.GetService<ISceneManager>() == null)
            {
                errors.Add("ISceneManager is not registered. Did you forget to call AddBrine2D()?");
            }

            if (serviceProvider.GetService<IGameContext>() == null)
            {
                errors.Add("IGameContext is not registered. Did you forget to call AddBrine2D()?");
            }

            // Note: We don't check for IRenderer, IInputService, etc. because
            // headless mode is valid (no SDL backend)

            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Game application configuration is invalid:{System.Environment.NewLine}" +
                    $"{string.Join(System.Environment.NewLine, errors.Select(e => $"  - {e}"))}");
            }
        }
    }
}
