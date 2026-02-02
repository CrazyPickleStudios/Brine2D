using Brine2D.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.ECS
{
    /// <summary>
    /// Registration for update systems.
    /// </summary>
    internal class UpdateSystemRegistration<T> : IUpdateSystemRegistration
        where T : IUpdateSystem
    {
        public IUpdateSystem Resolve(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<T>();
        }
    }

}
