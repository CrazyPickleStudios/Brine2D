using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Rendering;

namespace Brine2D.Hosting;

/// <summary>
///     Configuration options for Brine2D game engine.
/// </summary>
public sealed class Brine2DOptions
{
    private volatile bool _validated;

    /// <summary>Audio configuration.</summary>
    public AudioOptions Audio { get; init; } = new();

    /// <summary>ECS configuration.</summary>
    public ECSOptions ECS { get; init; } = new();

    /// <summary>
    ///     Time in seconds to wait for the game thread's finally block to complete after a forced
    ///     shutdown is triggered. Default: 2 seconds.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [Range(0, 30, ErrorMessage = "ForceShutdownGracePeriodSeconds must be between 0 and 30.")]
    public int ForceShutdownGracePeriodSeconds { get; set; } = 2;

    /// <summary>
    ///     Scheduling priority of the dedicated game thread.
    ///     Raise to <see cref="ThreadPriority.AboveNormal"/> on CPU-bound workloads for
    ///     tighter frame-timing. Avoid <see cref="ThreadPriority.Highest"/>; it can starve
    ///     OS threads and cause system instability on some platforms.
    ///     Default: <see cref="ThreadPriority.Normal"/>.
    /// </summary>
    public ThreadPriority GameThreadPriority { get; set; } = ThreadPriority.Normal;

    /// <summary>
    ///     Run in headless mode (no window, input, audio, or rendering).
    ///     Useful for dedicated servers or automated testing.
    /// </summary>
    public bool Headless { get; set; }

    /// <summary>
    ///     Minimum time in milliseconds a loading screen stays visible after the scene is ready.
    ///     Prevents loading screens from flashing on very fast loads. Default: 200. Set to 0 to disable.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "LoadingScreenMinimumDisplayMs must be 0 or greater.")]
    public int LoadingScreenMinimumDisplayMs { get; set; } = 200;
    
    /// <summary>Rendering configuration.</summary>
    public RenderingOptions Rendering { get; init; } = new();

    /// <summary>
    ///     Time in seconds to wait for the game thread to exit gracefully during disposal.
    ///     If the thread does not exit within this window, a forced shutdown is triggered.
    ///     Default: 5 seconds.
    /// </summary>
    [Range(1, 60, ErrorMessage = "ShutdownTimeoutSeconds must be between 1 and 60.")]
    public int ShutdownTimeoutSeconds { get; set; } = 5;

    /// <summary>Gets <see cref="ShutdownTimeoutSeconds"/> as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan ShutdownTimeout => TimeSpan.FromSeconds(ShutdownTimeoutSeconds);

    /// <summary>Window configuration.</summary>
    public WindowOptions Window { get; init; } = new();

    /// <summary>
    ///     Validates all options using DataAnnotations.
    ///     Called automatically by <see cref="GameApplicationBuilder.Build"/> and at host startup.
    ///     Subsequent calls short-circuit if the first validation succeeded.
    /// </summary>
    /// <exception cref="GameConfigurationException">
    ///     Thrown if validation fails with detailed error messages.
    /// </exception>
    public void Validate()
    {
        if (_validated)
            return;

        var allErrors = new List<string>();
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, new ValidationContext(this), errors, true))
            allErrors.AddRange(errors.Select(e => e.ErrorMessage ?? "Unknown validation error"));

        if (!Headless)
            ValidateNested(Window, nameof(Window), allErrors);

        ValidateNested(Rendering, nameof(Rendering), allErrors);
        ValidateNested(ECS, nameof(ECS), allErrors);
        ValidateNested(Audio, nameof(Audio), allErrors);

        if (ForceShutdownGracePeriodSeconds >= ShutdownTimeoutSeconds)
        {
            allErrors.Add(
                $"ForceShutdownGracePeriodSeconds ({ForceShutdownGracePeriodSeconds}s) must be less than " +
                $"ShutdownTimeoutSeconds ({ShutdownTimeoutSeconds}s).");
        }

        if (allErrors.Count > 0)
        {
            throw new GameConfigurationException(
                "Game application configuration is invalid:" + Environment.NewLine +
                string.Join(Environment.NewLine, allErrors.Select(e => $"  • {e}")) + Environment.NewLine +
                Environment.NewLine +
                "Fix: Check your builder.Configure(options => ...) calls in Program.cs");
        }

        _validated = true;
    }

    private static void ValidateNested(object obj, string propertyPath, List<string> allErrors,
        HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!visited.Add(obj)) return;

        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(obj, new ValidationContext(obj), errors, true))
        {
            foreach (var error in errors)
                allErrors.Add($"{propertyPath}: {error.ErrorMessage}");
        }

        foreach (var prop in obj.GetType().GetProperties()
            .Where(p => p.CanRead
                && !p.PropertyType.IsValueType
                && p.PropertyType != typeof(string)
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType)))
        {
            var value = prop.GetValue(obj);
            if (value != null)
                ValidateNested(value, $"{propertyPath}.{prop.Name}", allErrors, visited);
        }
    }
}