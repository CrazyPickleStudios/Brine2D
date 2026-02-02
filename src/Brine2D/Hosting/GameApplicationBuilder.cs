using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
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

        internal GameApplicationBuilder(string[] args)
        {
            _hostBuilder = Host.CreateApplicationBuilder(args);

            // Configure default settings - use ASP.NET conventions
            Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            Configuration.AddJsonFile($"appsettings.{System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true);

            // Add core engine services
            Services.AddBrineEngine();

            // Add default logging
            Logging.AddConsole();
            Logging.SetMinimumLevel(LogLevel.Information);
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
        public GameApplication Build()
        {
            var host = _hostBuilder.Build();
            return new GameApplication(host);
        }
    }
}
