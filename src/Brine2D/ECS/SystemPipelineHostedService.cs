using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.ECS
{
    /// <summary>
    /// Post-build configuration to populate update and render pipelines.
    /// This is registered as a hosted service to run automatically.
    /// </summary>
    internal class SystemPipelineHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public SystemPipelineHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Configure pipelines when the host starts
            SystemPipelineConfigurator.ConfigureUpdatePipeline(_serviceProvider);
            SystemPipelineConfigurator.ConfigureRenderPipeline(_serviceProvider);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
