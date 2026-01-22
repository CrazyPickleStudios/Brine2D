using Brine2D.Events;
using Brine2D.Hosting;
using Brine2D.SDL.Common.Events;
using Brine2D.SDL.Events;
using Microsoft.Extensions.Logging;

namespace Brine2D.SDL.Common;

/// <summary>
/// Central SDL3 event processing service.
/// Polls SDL events and routes them to appropriate event buses.
/// Similar to ASP.NET's event pipeline - single source of truth for platform events.
/// </summary>
public class SDL3EventPump : IEventPump, IHostApplicationLifetime
{
    private readonly ILogger<SDL3EventPump> _logger;
    private readonly EventBus _publicEventBus;
    private readonly EventBus _internalEventBus;

    public bool IsExitRequested { get; private set; }

    public SDL3EventPump(
        ILogger<SDL3EventPump> logger,
        EventBus publicEventBus,
        EventBus internalEventBus)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publicEventBus = publicEventBus ?? throw new ArgumentNullException(nameof(publicEventBus));
        _internalEventBus = internalEventBus ?? throw new ArgumentNullException(nameof(internalEventBus));
    }

    /// <summary>
    /// Processes all pending SDL events for this frame.
    /// Should be called once per frame, early in the game loop.
    /// </summary>
    public void ProcessEvents()
    {
        while (SDL3.SDL.PollEvent(out var evt))
        {
            RouteEvent(evt);
        }
    }

    private void RouteEvent(SDL3.SDL.Event evt)
    {
        switch ((SDL3.SDL.EventType)evt.Type)
        {
            // ===== LIFECYCLE EVENTS (Public) =====
            case SDL3.SDL.EventType.Quit:
                HandleQuit();
                break;

            case SDL3.SDL.EventType.WindowResized:
                _publicEventBus.Publish(new WindowResizedEvent(evt.Window.Data1, evt.Window.Data2));
                _logger.LogDebug("Window resized to {Width}x{Height}", evt.Window.Data1, evt.Window.Data2);
                break;

            case SDL3.SDL.EventType.WindowMinimized:
                _publicEventBus.Publish(new WindowMinimizedEvent());
                _logger.LogDebug("Window minimized");
                break;

            case SDL3.SDL.EventType.WindowRestored:
                _publicEventBus.Publish(new WindowRestoredEvent());
                _logger.LogDebug("Window restored");
                break;

            case SDL3.SDL.EventType.WindowFocusGained:
                _publicEventBus.Publish(new WindowFocusGainedEvent());
                _logger.LogDebug("Window focus gained");
                break;

            case SDL3.SDL.EventType.WindowFocusLost:
                _publicEventBus.Publish(new WindowFocusLostEvent());
                _logger.LogDebug("Window focus lost");
                break;

            // ===== INPUT EVENTS (Internal SDL events only) =====
            case SDL3.SDL.EventType.KeyDown:
                _internalEventBus.Publish(new SDL3KeyDownEvent(evt.Key));
                break;

            case SDL3.SDL.EventType.KeyUp:
                _internalEventBus.Publish(new SDL3KeyUpEvent(evt.Key));
                break;

            case SDL3.SDL.EventType.MouseButtonDown:
                _internalEventBus.Publish(new SDL3MouseButtonDownEvent(evt.Button));
                break;

            case SDL3.SDL.EventType.MouseButtonUp:
                _internalEventBus.Publish(new SDL3MouseButtonUpEvent(evt.Button));
                break;

            case SDL3.SDL.EventType.MouseWheel:
                _internalEventBus.Publish(new SDL3MouseWheelEvent(evt.Wheel));
                break;

            case SDL3.SDL.EventType.MouseMotion:
                _internalEventBus.Publish(new SDL3MouseMotionEvent(evt.Motion));
                break;

            case SDL3.SDL.EventType.TextInput:
                _internalEventBus.Publish(new SDL3TextInputEvent(evt.Text));
                break;

            case SDL3.SDL.EventType.GamepadButtonDown:
                _internalEventBus.Publish(new SDL3GamepadButtonDownEvent(evt.GButton));
                break;

            case SDL3.SDL.EventType.GamepadButtonUp:
                _internalEventBus.Publish(new SDL3GamepadButtonUpEvent(evt.GButton));
                break;

            case SDL3.SDL.EventType.GamepadAdded:
                _internalEventBus.Publish(new SDL3GamepadAddedEvent(evt.GDevice));
                break;

            case SDL3.SDL.EventType.GamepadRemoved:
                _internalEventBus.Publish(new SDL3GamepadRemovedEvent(evt.GDevice));
                break;
        }
    }

    private void HandleQuit()
    {
        IsExitRequested = true;
        _publicEventBus.Publish(new ApplicationQuitRequestedEvent());
        _logger.LogInformation("Application quit requested");
    }

    public void RequestExit()
    {
        HandleQuit();
    }
}