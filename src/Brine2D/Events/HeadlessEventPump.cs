using Brine2D.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Events;

/// <summary>
/// No-op event pump for headless mode (servers, testing).
/// No SDL events are processed - allows game loop to run without SDL window.
/// </summary>
internal sealed class HeadlessEventPump : IEventPump
{
    private readonly ILogger<HeadlessEventPump>? _logger;

    public HeadlessEventPump(ILogger<HeadlessEventPump>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("Headless event pump initialized (no SDL events)");
    }

    public void ProcessEvents()  // ✅ Changed from PumpEvents
    {
        // No SDL events to process in headless mode
    }

    public void Dispose()
    {
        _logger?.LogDebug("Headless event pump disposed");
    }
}