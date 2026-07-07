namespace Brine2D.ECS.Systems;

/// <summary>
/// Marks a system to always execute sequentially (single-threaded),
/// regardless of the global <see cref="ECSOptions.EnableMultiThreading"/> setting.
/// Use this for systems that are not thread-safe or have strict ordering requirements.
/// </summary>
/// <remarks>
/// This attribute is declared with <c>Inherited = false</c>, meaning it is not
/// automatically applied to subclasses. If you derive from a
/// <c>[Sequential]</c>-marked system, the derived class will run in parallel
/// unless you explicitly apply <c>[Sequential]</c> to it as well.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SequentialAttribute : Attribute
{
}