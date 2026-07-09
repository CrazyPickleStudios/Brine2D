using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Brine2D.ECS.Serialization;

/// <summary>
/// Registry that maps component types to their serialization delegates for use with
/// <see cref="AotEntitySerializer"/>. Register each component type once at startup;
/// <see cref="AotEntitySerializer"/> will use the registered delegates for every
/// serialize/deserialize/attach operation.
/// </summary>
/// <remarks>
/// <para>
/// Three registration tiers, in order of AOT-safety:
/// </para>
/// <list type="number">
/// <item>
/// <b>Fully AOT-safe:</b> <see cref="Register{T}(JsonTypeInfo{T})"/>.
/// Supply a source-generated <see cref="JsonTypeInfo{T}"/> from your own
/// <c>[JsonSerializable]</c>-annotated <c>JsonSerializerContext</c>.
/// Zero runtime reflection; compatible with NativeAOT and IL trimming.
/// Best when you publish with <c>&lt;PublishTrimmed&gt;true&lt;/PublishTrimmed&gt;</c>
/// and only need to cover a handful of custom component types.
/// </item>
/// <item>
/// <b>Reflection, explicit:</b> <see cref="Register{T}()"/>.
/// No <c>[JsonSerializable]</c> needed; the registry derives JSON metadata at
/// runtime. Annotated with <c>[RequiresDynamicCode]</c> and
/// <c>[RequiresUnreferencedCode]</c> so the trimmer surfaces any trimmed-publish
/// usages during analysis. Suitable for non-trimmed publishing.
/// </item>
/// <item>
/// <b>Reflection, automatic:</b> <see cref="RegisterAllComponents"/> and
/// <see cref="RegisterBrineComponents"/>.
/// Scans one or more <see cref="Assembly"/> instances and registers every
/// concrete (non-abstract, non-generic) <see cref="Component"/> subclass it finds.
/// One call covers all components in the assembly with zero per-type boilerplate.
/// Same AOT restrictions as tier 2; not suitable for trimmed publishing.
/// </item>
/// </list>
/// <para>
/// <b>Typical setup (non-trimmed):</b>
/// <code>
/// var registry = new ComponentTypeRegistry();
/// registry.RegisterBrineComponents();          // all built-in engine components
/// registry.RegisterAllComponents(GetType().Assembly); // all your game's components
///
/// var serializer = new AotEntitySerializer(registry);
/// </code>
/// </para>
/// </remarks>
public sealed class ComponentTypeRegistry
{
    private sealed class Registration
    {
        public required Func<JsonElement, Component?> Deserialize { get; init; }
        public required Func<Component, JsonElement> Serialize { get; init; }
        public required Action<Entity, Component> Attach { get; init; }
    }

    private readonly Dictionary<string, Registration> _byFullName = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a component type using a source-generated <see cref="JsonTypeInfo{T}"/>.
    /// This is the fully AOT-safe registration path.
    /// </summary>
    /// <typeparam name="T">The concrete component type to register.</typeparam>
    /// <param name="typeInfo">
    /// The source-generated type info for <typeparamref name="T"/>, typically obtained from a
    /// <c>[JsonSerializable(typeof(T))]</c>-attributed <c>JsonSerializerContext</c> subclass.
    /// </param>
    /// <example>
    /// <code>
    /// // [JsonSerializable(typeof(HealthComponent))]
    /// // internal partial class MyGameContext : JsonSerializerContext { }
    ///
    /// var registry = new ComponentTypeRegistry();
    /// registry.RegisterBrineComponents();
    /// registry.Register(MyGameContext.Default.HealthComponent);
    /// </code>
    /// </example>
    public void Register<T>(JsonTypeInfo<T> typeInfo) where T : Component
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        var key = typeof(T).FullName ?? typeof(T).Name;

        _byFullName[key] = new Registration
        {
            Deserialize = element =>
                JsonSerializer.Deserialize(element.GetRawText(), typeInfo),
            Serialize = component =>
                JsonSerializer.SerializeToElement((T)component, typeInfo),
            Attach = static (entity, component) => entity.AddComponent((T)component)
        };
    }

    /// <summary>
    /// Registers a single component type using the default <see cref="JsonSerializerOptions"/>
    /// (camelCase, enums as strings, <see cref="System.Numerics.Vector2"/> converter).
    /// </summary>
    /// <remarks>
    /// This overload resolves serialization metadata at runtime and is not compatible with
    /// NativeAOT or IL trimming. Prefer <see cref="Register{T}(JsonTypeInfo{T})"/> when
    /// publishing trimmed, or <see cref="RegisterAllComponents"/> to bulk-register without
    /// per-type calls.
    /// </remarks>
    [RequiresDynamicCode("Uses runtime JSON reflection for component type T. Use Register<T>(JsonTypeInfo<T>) for AOT compatibility.")]
    [RequiresUnreferencedCode("Uses runtime JSON reflection for component type T. Use Register<T>(JsonTypeInfo<T>) for AOT compatibility.")]
    public void Register<T>() where T : Component
    {
        var options = EntitySerializer.CreateDefaultOptions();
        var key = typeof(T).FullName ?? typeof(T).Name;

        _byFullName[key] = new Registration
        {
            Deserialize = element =>
                JsonSerializer.Deserialize<T>(element.GetRawText(), options),
            Serialize = component =>
                JsonSerializer.SerializeToElement((T)component, options),
            Attach = static (entity, component) => entity.AddComponent((T)component)
        };
    }

    /// <summary>
    /// Scans the provided assemblies and automatically registers every concrete
    /// (non-abstract, non-generic, publicly constructible) <see cref="Component"/> subclass found.
    /// Abstract base classes and open generic types are skipped.
    /// </summary>
    /// <remarks>
    /// This is the lowest-boilerplate registration path for non-trimmed publishing.
    /// Call once per assembly at startup; duplicate registrations for a type that is already
    /// registered are silently overwritten (last call wins).
    /// <para>
    /// Not compatible with NativeAOT or IL trimming — use <see cref="Register{T}(JsonTypeInfo{T})"/>
    /// for each type instead when publishing with <c>&lt;PublishTrimmed&gt;true&lt;/PublishTrimmed&gt;</c>.
    /// </para>
    /// </remarks>
    /// <param name="assemblies">
    /// One or more assemblies to scan. Pass <c>GetType().Assembly</c> to register all
    /// components in your game project.
    /// </param>
    /// <returns>The number of component types registered.</returns>
    [RequiresDynamicCode("Scans assembly types at runtime and uses reflection-based JSON serialization. Not AOT-compatible.")]
    [RequiresUnreferencedCode("Scans assembly types at runtime. Types may be trimmed away. Not compatible with IL trimming.")]
    public int RegisterAllComponents(params Assembly[] assemblies)
    {
        var options = EntitySerializer.CreateDefaultOptions();
        var componentBase = typeof(Component);
        var count = 0;

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types!; }

            foreach (var type in types)
            {
                if (type == null) continue;
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                if (!componentBase.IsAssignableFrom(type)) continue;

                RegisterByType(type, options);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Registers all concrete <see cref="Component"/> subclasses built into the Brine2D engine.
    /// Equivalent to <c>RegisterAllComponents(typeof(Component).Assembly)</c>.
    /// </summary>
    /// <remarks>
    /// Call this once at startup before registering your own game components.
    /// Combine with <see cref="RegisterAllComponents"/> for your game assembly:
    /// <code>
    /// registry.RegisterBrineComponents();
    /// registry.RegisterAllComponents(GetType().Assembly);
    /// </code>
    /// Not compatible with NativeAOT or IL trimming.
    /// </remarks>
    /// <returns>The number of engine component types registered.</returns>
    [RequiresDynamicCode("Delegates to RegisterAllComponents which uses runtime reflection. Not AOT-compatible.")]
    [RequiresUnreferencedCode("Delegates to RegisterAllComponents which scans assembly types at runtime. Not compatible with IL trimming.")]
    public int RegisterBrineComponents()
        => RegisterAllComponents(typeof(Component).Assembly);

    /// <summary>Returns whether a component type has been registered under its full type name.</summary>
    public bool IsRegistered<T>() where T : Component
        => _byFullName.ContainsKey(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>Returns whether the given type name has a registration.</summary>
    public bool IsRegistered(string fullTypeName)
        => _byFullName.ContainsKey(fullTypeName);

    /// <summary>The number of component types currently registered.</summary>
    public int Count => _byFullName.Count;

    internal bool TryDeserializeAndAttach(string typeName, JsonElement data, Entity entity)
    {
        if (!_byFullName.TryGetValue(typeName, out var reg))
            return false;

        var component = reg.Deserialize(data);
        if (component == null)
            return false;

        reg.Attach(entity, component);
        return true;
    }

    internal bool TrySerialize(Component component, out JsonElement element)
    {
        var key = component.GetType().FullName ?? component.GetType().Name;
        if (!_byFullName.TryGetValue(key, out var reg))
        {
            element = default;
            return false;
        }

        element = reg.Serialize(component);
        return true;
    }

    [RequiresDynamicCode("Uses MakeGenericMethod and reflection-based JSON serialization.")]
    [RequiresUnreferencedCode("Uses reflection-based JSON serialization metadata.")]
    private void RegisterByType(Type type, JsonSerializerOptions options)
    {
        var key = type.FullName ?? type.Name;
        var attachMethod = EntitySerializer.AddComponentMethod.MakeGenericMethod(type);

        _byFullName[key] = new Registration
        {
            Deserialize = element =>
                JsonSerializer.Deserialize(element.GetRawText(), type, options) as Component,
            Serialize = component =>
                JsonSerializer.SerializeToElement(component, type, options),
            Attach = (entity, component) =>
                attachMethod.Invoke(entity, [component])
        };
    }
}
