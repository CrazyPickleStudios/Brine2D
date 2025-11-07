namespace Brine2D.Core.Content;

/// <summary>
///     Watches one or more content directories for file changes and invalidates cached assets
///     in the associated <see cref="ContentManager" />. Consumers can subscribe to <see cref="Changed" />
///     to react (e.g., hot-reload) when files are updated, created, renamed, or deleted.
/// </summary>
public sealed class ContentWatcher : IDisposable
{
    /// <summary>
    ///     Content manager to invalidate when file system changes occur.
    /// </summary>
    private readonly ContentManager _content;

    /// <summary>
    ///     Active watchers, one per root directory. Disposed in <see cref="Dispose" />.
    /// </summary>
    private readonly List<FileSystemWatcher> _watchers = new();

    /// <summary>
    ///     Initializes a new instance that observes the given content roots.
    ///     Non-existent roots are skipped.
    /// </summary>
    /// <param name="content">The content manager that will be invalidated upon changes.</param>
    /// <param name="roots">One or more root directories to watch recursively.</param>
    public ContentWatcher(ContentManager content, params string[] roots)
    {
        _content = content;

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            // Configure watcher to include subdirectories and start immediately.
            var w = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };

            // Wire up change notifications we care about.
            w.Changed += OnChanged;
            w.Created += OnChanged;
            w.Renamed += OnRenamed;
            w.Deleted += OnDeleted;

            _watchers.Add(w);
        }
    }

    /// <summary>
    ///     Raised after a change is observed. The argument is the path of the affected file
    ///     (for rename events, this is the new path).
    /// </summary>
    public event Action<string>? Changed;

    /// <summary>
    ///     Disposes all underlying watchers and clears the internal list.
    /// </summary>
    public void Dispose()
    {
        foreach (var w in _watchers)
        {
            w.Dispose();
        }

        _watchers.Clear();
    }

    /// <summary>
    ///     Handles Created and Changed notifications by invalidating the path and
    ///     notifying subscribers.
    /// </summary>
    private void OnChanged(object? s, FileSystemEventArgs e)
    {
        _content.Invalidate(e.FullPath);
        Changed?.Invoke(e.FullPath);
    }

    /// <summary>
    ///     Handles Deleted notifications by invalidating the path and notifying subscribers.
    /// </summary>
    private void OnDeleted(object? s, FileSystemEventArgs e)
    {
        _content.Invalidate(e.FullPath);
        Changed?.Invoke(e.FullPath);
    }

    /// <summary>
    ///     Handles Renamed notifications by invalidating both old and new paths and
    ///     notifying subscribers with the new path.
    /// </summary>
    private void OnRenamed(object? s, RenamedEventArgs e)
    {
        _content.Invalidate(e.OldFullPath);
        _content.Invalidate(e.FullPath);
        Changed?.Invoke(e.FullPath);
    }
}