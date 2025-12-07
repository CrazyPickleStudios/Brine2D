using Brine2D.Engine;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBrine2DCore(this IServiceCollection services)
        {
            return services;
        }
    }
}
