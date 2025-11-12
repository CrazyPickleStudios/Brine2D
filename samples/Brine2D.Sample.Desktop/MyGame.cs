using Brine2D.Core.Graphics;
using Brine2D.Core.Hosting;
using Brine2D.Core.Input;
using Brine2D.Core.Runtime;
using Brine2D.Core.Timing;
using Brine2D.SDL.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Brine2D.Core.Math;

namespace Brine2D.Sample.Desktop
{
    public sealed class MyGame : IGame
    {
        private IEngineContext _ctx = default!;
        private ITexture2D _tex = default!;

        public void Initialize(IEngineContext context)
        {
            _ctx = context;
            _ctx.Window.Title = "Overlay Demo";
            _tex = _ctx.Content.Load<ITexture2D>("images/brine2d_logo.png");
        }

        public void Update(GameTime time)
        {
            // Normal update logic. Overlay metrics are handled by SdlHost.
        }

        public void Draw(GameTime time)
        {
            _ctx.Renderer.Clear(Color.CornflowerBlue);
            _ctx.Sprites.Begin();
            _ctx.Sprites.Draw(_tex, null, new Rectangle(64, 64, 128, 128), Color.White);
            _ctx.Sprites.End();
            // No need to call overlay.Draw(); SdlHost already did.
        }
    }
}
