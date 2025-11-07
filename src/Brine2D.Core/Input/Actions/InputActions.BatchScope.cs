namespace Brine2D.Core.Input.Actions;

public sealed partial class InputActions<TAction, TAxis>
{
    /// <summary>
    ///     Scoped helper to coalesce multiple binding changes into a single notification.
    /// </summary>
    /// <remarks>
    ///     Usage:
    ///     <code>
    ///     using (inputActions.BeginBindingsBatch())
    ///     {
    ///         // Multiple Bind*/Unbind*/Set* calls here
    ///     } // Single consolidated change notification on dispose.
    ///     </code>
    ///     Construction calls <see cref="BeginBindingsUpdate" /> to increment an internal batch depth counter.
    ///     Disposal calls <see cref="EndBindingsUpdate" /> to decrement the counter and, if this was the outermost
    ///     scope and changes occurred, emit a single bindings-changed notification.
    /// </remarks>
    private readonly struct BatchScope : IDisposable
    {
        // Owning InputActions instance that tracks batch depth and notifications.
        private readonly InputActions<TAction, TAxis> _owner;

        /// <summary>
        ///     Creates a new scope and begins a batched update on the owning <see cref="InputActions{TAction, TAxis}" />.
        /// </summary>
        /// <param name="owner">The owning input map that manages batch depth and change notifications.</param>
        public BatchScope(InputActions<TAction, TAxis> owner)
        {
            _owner = owner;
            // Enter batch mode (increments depth; suppresses intermediate notifications).
            _owner.BeginBindingsUpdate();
        }

        /// <summary>
        ///     Ends the batched update on the owning <see cref="InputActions{TAction, TAxis}" />.
        /// </summary>
        /// <remarks>
        ///     If this was the outermost scope and any changes occurred during the batch, a single
        ///     bindings-changed notification is emitted by the owner.
        /// </remarks>
        public void Dispose()
        {
            // Leave batch mode (decrements depth; may emit a consolidated notification).
            _owner.EndBindingsUpdate();
        }
    }
}