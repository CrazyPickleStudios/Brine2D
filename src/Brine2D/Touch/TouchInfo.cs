using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL;

namespace Brine2D
{
    internal struct TouchInfo
    {
        public SDL_FingerID id;  // Identifier. Only unique for the duration of the touch-press.
        public double x;  // Position in pixels (for touchscreens) or normalized [0, 1] position (for touchpads) along the x-axis.
        public double y;  // Position in pixels (for touchscreens) or normalized [0, 1] position (for touchpads) along the y-axis.
        public double dx; // Amount moved along the x-axis.
        public double dy; // Amount moved along the y-axis.
        public double pressure;
        public DeviceType deviceType;
        public bool mouse;
    };
}
