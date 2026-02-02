using Brine2D.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.ECS
{
    /// <summary>
    /// Registration for render systems.
    /// </summary>
    internal class RenderSystemRegistration<T> : IRenderSystemRegistration
        where T : IRenderSystem
    {
        public IRenderSystem Resolve(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<T>();
        }
    }
}
