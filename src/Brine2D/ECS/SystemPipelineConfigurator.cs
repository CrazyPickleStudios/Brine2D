using Brine2D.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.ECS
{
    /// <summary>
    /// Post-build configuration to populate update and render pipelines.
    /// Called after the service provider is built.
    /// </summary>
    public static class SystemPipelineConfigurator
    {
        public static void ConfigureUpdatePipeline(IServiceProvider serviceProvider)
        {
            var pipeline = serviceProvider.GetRequiredService<UpdatePipeline>();
            var registrations = serviceProvider.GetServices<IUpdateSystemRegistration>();

            foreach (var registration in registrations)
            {
                var system = registration.Resolve(serviceProvider);
                pipeline.AddSystem(system);
            }
        }

        public static void ConfigureRenderPipeline(IServiceProvider serviceProvider)
        {
            var pipeline = serviceProvider.GetRequiredService<RenderPipeline>();
            var registrations = serviceProvider.GetServices<IRenderSystemRegistration>();

            foreach (var registration in registrations)
            {
                var system = registration.Resolve(serviceProvider);
                pipeline.AddSystem(system);
            }
        }
    }
}
