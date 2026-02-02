using Brine2D.ECS.Systems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.ECS
{
    // Registration helpers for deferred system resolution
    internal interface IUpdateSystemRegistration
    {
        IUpdateSystem Resolve(IServiceProvider serviceProvider);
    }
}
