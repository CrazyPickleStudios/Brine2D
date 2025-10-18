namespace Brine2D;

public abstract class Module : Object
{
    private static readonly Dictionary<Type, Module> Registry = new();

    protected Module()
    {
        RegisterInstance(this);
    }

    public static T? GetInstance<T>()
        where T : Module
    {
        Registry.TryGetValue(typeof(T), out var module);
        return module as T;
    }

    protected internal virtual void Dispose()
    {
        var type = GetType();
        if (Registry.TryGetValue(type, out var instance) && ReferenceEquals(instance, this))
            Registry.Remove(type);
    }

    private static void RegisterInstance(Module instance)
    {
        var type = instance.GetType();

        if (!Registry.TryAdd(type, instance))
            throw new InvalidOperationException($"Module of type {type.Name} already registered.");
    }
}