using Brine2D.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Events;

/// <summary>
/// No-op event pump for headless mode (servers, testing).
/// </summary>
internal sealed class HeadlessEventPump : IEventPump, IDisposable
{
    private readonly ILogger<HeadlessEventPump> _logger;

    public HeadlessEventPump(ILogger<HeadlessEventPump> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("Headless event pump initialized");
    }

    public void ProcessEvents() { }

    public void Dispose() => _logger.LogDebug("Headless event pump disposed");
}