using Brine2D.ECS.Systems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.ECS
{
    internal interface IRenderSystemRegistration
    {
        IRenderSystem Resolve(IServiceProvider serviceProvider);
    }
}
