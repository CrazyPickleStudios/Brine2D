namespace Brine2D.ECS.Systems;

/// <summary>
/// Marks a system to always execute sequentially (single-threaded).
/// Use this for systems that are not thread-safe or have strict ordering requirements.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SequentialAttribute : Attribute
{
}