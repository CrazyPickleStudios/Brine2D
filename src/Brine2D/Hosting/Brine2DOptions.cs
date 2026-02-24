using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Input;
using Brine2D.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Brine2D.Hosting;

/// <summary>
/// Configuration options for Brine2D game engine.
/// </summary>
public sealed class Brine2DOptions
{
    /// <summary>Window configuration.</summary>
    [Required]
    public WindowOptions Window { get; set; } = new();

    /// <summary>Rendering configuration.</summary>
    [Required]
    public RenderingOptions Rendering { get; set; } = new();

    /// <summary>ECS configuration.</summary>
    [Required]
    public ECSOptions ECS { get; set; } = new();

    /// <summary>
    /// Run in headless mode (no window, input, audio, or rendering).
    /// Useful for dedicated servers or automated testing.
    /// </summary>
    public bool Headless { get; set; } = false;

    /// <summary>
    /// Minimum time in milliseconds a loading screen stays visible after the scene is ready.
    /// Prevents loading screens from flashing imperceptibly for very fast loads. Default: 200.
    /// Set to 0 to disable.
    /// </summary>
    public int LoadingScreenMinimumDisplayMs { get; set; } = 200;

    /// <summary>
    /// Validates all options using DataAnnotations (ASP.NET Core pattern).
    /// Called automatically by GameApplicationBuilder.Build().
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if validation fails with detailed error messages.
    /// </exception>
    public void Validate()
    {
        var allErrors = new List<string>();

        var errors = new List<ValidationResult>();
        var context = new ValidationContext(this, serviceProvider: null, items: null);

        if (!Validator.TryValidateObject(this, context, errors, validateAllProperties: true))
            allErrors.AddRange(errors.Select(e => e.ErrorMessage ?? "Unknown validation error"));

        ValidateNested(Window, "Window", allErrors);
        ValidateNested(Rendering, "Rendering", allErrors);
        ValidateNested(ECS, "ECS", allErrors);

        if (allErrors.Any())
        {
            throw new InvalidOperationException(
                "Game application configuration is invalid:" + Environment.NewLine +
                string.Join(Environment.NewLine, allErrors.Select(e => $"  • {e}")) + Environment.NewLine +
                Environment.NewLine +
                "Fix: Check your builder.Configure(options => ...) calls in Program.cs");
        }
    }

    private static void ValidateNested(object obj, string propertyName, List<string> allErrors)
    {
        var errors = new List<ValidationResult>();
        var context = new ValidationContext(obj, serviceProvider: null, items: null);

        if (!Validator.TryValidateObject(obj, context, errors, validateAllProperties: true))
        {
            foreach (var error in errors)
                allErrors.Add($"{propertyName}: {error.ErrorMessage}");
        }
    }
}