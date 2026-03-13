using System.Diagnostics.CodeAnalysis;
using Brine2D.Events;
using Brine2D.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SDL3;

namespace Brine2D;

/// <summary>
///     Central SDL3 event processing service.
///     Polls SDL events and routes them to appropriate event buses.
///     Similar to ASP.NET's event pipeline; single source of truth for platform events.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires a live SDL3 event loop; covered by manual/hardware testing.")]
internal class SDL3EventPump : IEventPump
{
    // The internal bus is the concrete EventBus because it is an SDL3-private channel,
    // registered as a keyed singleton and never surfaced through IEventBus.
    private readonly IEventBus _internalEventBus;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SDL3EventPump> _logger;
    private readonly IEventBus _publicEventBus;

    public SDL3EventPump
    (
        ILogger<SDL3EventPump> logger,
        IEventBus publicEventBus,
        IEventBus internalEventBus,
        IHostApplicationLifetime lifetime
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publicEventBus = publicEventBus ?? throw new ArgumentNullException(nameof(publicEventBus));
        _internalEventBus = internalEventBus ?? throw new ArgumentNullException(nameof(internalEventBus));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    /// <summary>
    ///     Processes all pending SDL events for this frame.
    ///     Should be called once per frame, early in the game loop.
    /// </summary>
    public void ProcessEvents()
    {
        while (SDL.PollEvent(out var evt))
        {
            RouteEvent(evt);
        }
    }

    private void HandleQuit()
    {
        _lifetime.StopApplication();
        _publicEventBus.Publish(new ApplicationQuitRequestedEvent());
        _logger.LogInformation("Application quit requested");
    }

    private void RouteEvent(SDL.Event evt)
    {
        switch ((SDL.EventType)evt.Type)
        {
            case SDL.EventType.Quit:
                HandleQuit();
                break;

            case SDL.EventType.WindowResized:
                _publicEventBus.Publish(new WindowResizedEvent(evt.Window.Data1, evt.Window.Data2));
                _logger.LogDebug("Window resized to {Width}x{Height}", evt.Window.Data1, evt.Window.Data2);
                break;

            case SDL.EventType.WindowMinimized:
                _publicEventBus.Publish(new WindowMinimizedEvent());
                _logger.LogDebug("Window minimized");
                break;

            case SDL.EventType.WindowRestored:
                _publicEventBus.Publish(new WindowRestoredEvent());
                _logger.LogDebug("Window restored");
                break;

            case SDL.EventType.WindowFocusGained:
                _publicEventBus.Publish(new WindowFocusGainedEvent());
                _logger.LogDebug("Window focus gained");
                break;

            case SDL.EventType.WindowFocusLost:
                _publicEventBus.Publish(new WindowFocusLostEvent());
                _logger.LogDebug("Window focus lost");
                break;

            case SDL.EventType.WindowHidden:
                _publicEventBus.Publish(new WindowHiddenEvent());
                _logger.LogDebug("Window hidden");
                break;

            case SDL.EventType.WindowShown:
                _publicEventBus.Publish(new WindowShownEvent());
                _logger.LogDebug("Window shown");
                break;

            case SDL.EventType.KeyDown:
                _internalEventBus.Publish(new SDL3KeyDownEvent(evt.Key));
                break;

            case SDL.EventType.KeyUp:
                _internalEventBus.Publish(new SDL3KeyUpEvent(evt.Key));
                break;

            case SDL.EventType.MouseButtonDown:
                _internalEventBus.Publish(new SDL3MouseButtonDownEvent(evt.Button));
                break;

            case SDL.EventType.MouseButtonUp:
                _internalEventBus.Publish(new SDL3MouseButtonUpEvent(evt.Button));
                break;

            case SDL.EventType.MouseWheel:
                _internalEventBus.Publish(new SDL3MouseWheelEvent(evt.Wheel));
                break;

            case SDL.EventType.MouseMotion:
                _internalEventBus.Publish(new SDL3MouseMotionEvent(evt.Motion));
                break;

            case SDL.EventType.TextInput:
                _internalEventBus.Publish(new SDL3TextInputEvent(evt.Text));
                break;

            case SDL.EventType.GamepadButtonDown:
                _internalEventBus.Publish(new SDL3GamepadButtonDownEvent(evt.GButton));
                break;

            case SDL.EventType.GamepadButtonUp:
                _internalEventBus.Publish(new SDL3GamepadButtonUpEvent(evt.GButton));
                break;

            case SDL.EventType.GamepadAdded:
                _internalEventBus.Publish(new SDL3GamepadAddedEvent(evt.GDevice));
                break;

            case SDL.EventType.GamepadRemoved:
                _internalEventBus.Publish(new SDL3GamepadRemovedEvent(evt.GDevice));
                break;
        }
    }
}