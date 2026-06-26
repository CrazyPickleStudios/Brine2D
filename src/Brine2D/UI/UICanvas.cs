using System.Numerics;
using Brine2D.Core;
using Brine2D.Events;
using Brine2D.Input;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Canvas that holds and manages UI components, routing input and rendering.
/// </summary>
public class UICanvas : IInputLayer, IDisposable
{
    private readonly List<IUIComponent> _components = new();
    private readonly List<InputDispatchEntry> _inputDispatchBuffer = new();
    private readonly IInputContext _input;

    private UIButton? _hoveredButton;
    private UIButton? _pressedButton;
    private UIButton? _dialogPressedButton;
    private UISlider? _activeSlider;
    private Vector2 _activeSliderInputOffset;
    private UITextInput? _focusedTextInput;
    private UITextArea? _focusedTextArea;
    private UICheckbox? _hoveredCheckbox;
    private UIDropdown? _activeDropdown;
    private Vector2 _activeDropdownInputOffset;
    private UIRadioButton? _hoveredRadioButton;
    private UITabContainer? _hoveredTabContainer;
    private Vector2 _hoveredTabContainerInputOffset;
    private UITabContainer? _focusedTabContainer;
    private UIScrollView? _activeScrollView;
    private UIScrollView? _scrollbarDragView;
    private bool _scrollbarDragIsVertical;
    private UIScrollView? _dialogScrollbarDragView;
    private UIDialog? _activeDialog;
    private UIDialog? _draggingDialog;
    private Vector2 _dialogDragOffset;
    private UIDropdown? _activeDialogDropdown;

    private UISpinBox? _hoveredSpinBox;

    private UIVirtualListBase? _hoveredVirtualList;
    private Vector2 _hoveredVirtualListInputOffset;
    private UIVirtualListBase? _thumbDragVirtualList;
    private Vector2 _thumbDragVirtualListInputOffset;

    private UITreeView? _hoveredTreeView;
    private Vector2 _hoveredTreeViewInputOffset;
    private UITreeView? _thumbDragTreeView;
    private Vector2 _thumbDragTreeViewInputOffset;

    // World-space UI overlay state
    private readonly List<IUIWorldComponent> _worldComponents = new();

    // Tween state
    private readonly List<UITween> _activeTweens = new();
    private readonly List<UITweenSequence> _activeTweenSequences = new();

    // Drag-and-drop state
    private readonly Dictionary<IUIComponent, IDragPayload> _draggableRegistry = new();
    private readonly List<UIDropTarget> _dropTargets = new();
    private IUIComponent? _dragSource;
    private IDragPayload? _dragPayload;
    private Vector2 _dragStartMousePos;
    private Vector2 _dragGhostOffset;
    private bool _dragActive;
    private UIDropTarget? _hoveredDropTarget;
    private const float DragThreshold = 6f;

    /// <summary>
    /// Fired when a drag operation begins.
    /// </summary>
    public event Action<IUIComponent, IDragPayload>? OnDragStarted;

    /// <summary>
    /// Fired when a drag is cancelled (released over no valid target).
    /// </summary>
    public event Action<IUIComponent, IDragPayload>? OnDragCancelled;

    private UITooltip? _activeTooltip;
    private IUIComponent? _tooltipOwner;
    private UIContextMenu? _activeContextMenu;
    private int _contextMenuHoveredIndex = -1;
    private readonly List<UIToast> _toasts = new();
    private readonly List<UIMenuBar> _menuBars = new();
    private UIMenuBar? _activeMenuBar;

    private IUIComponent? _focusedWidget;
    private IDisposable? _resizeSubscription;

    // When the active dropdown lives inside a UIScrollView its expanded list is drawn
    // as a top-level overlay. This field holds the resolved screen position of the dropdown
    // header so RenderListOverlay knows where to draw.
    private Vector2 _activeDropdownOverlayPosition;
    private bool _activeDropdownNeedsOverlay;

    private Vector2 _screenSize = new Vector2(1280, 720);

    /// <summary>
    /// UI has high priority so it intercepts input first.
    /// </summary>
    public int Priority => 1000;

    /// <summary>
    /// Optional camera used to project <see cref="IUIWorldComponent.WorldPosition"/> values
    /// to screen space during <see cref="Render"/>. When <c>null</c>, world components are
    /// not projected and are rendered at their last assigned <see cref="IUIComponent.Position"/>.
    /// </summary>
    public ICamera? WorldCamera { get; set; }

    /// <summary>
    /// Current screen dimensions used to resolve <see cref="UIAnchor"/> positions and
    /// to center any <see cref="UIDialog"/> components. Update this whenever the window
    /// is resized. Defaults to 1280×720.
    /// </summary>
    public Vector2 ScreenSize
    {
        get => _screenSize;
        set
        {
            _screenSize = value;
            foreach (var component in _components)
            {
                if (component is UIDialog dialog)
                    dialog.CenterOnScreen(value);

                PropagateScreenSize(component, value);
            }
        }
    }

    /// <summary>
    /// Recursively pushes <paramref name="screenSize"/> into nested <see cref="UIDropdown"/>,
    /// <see cref="UITabContainer"/>, and container children.
    /// Called from the <see cref="ScreenSize"/> setter, <see cref="Add"/>, and container
    /// <c>AddChild</c> methods.
    /// </summary>
    internal static void PropagateScreenSize(IUIComponent component, Vector2 screenSize)
    {
        switch (component)
        {
            case UIDropdown dd:
                dd.ScreenHeight = screenSize.Y;
                return;

            case UIMenuBar mb:
                mb.ScreenHeight = screenSize.Y;
                return;

            case UITabContainer tab:
                tab.ScreenSize = screenSize;
                for (int t = 0; t < tab.TabCount; t++)
                    foreach (var child in tab.GetTabComponents(t))
                        PropagateScreenSize(child, screenSize);
                return;

            case UIPanel panel:
                panel.LastKnownScreenSize = screenSize;
                foreach (var child in panel.GetChildren())
                    PropagateScreenSize(child, screenSize);
                return;

            case UIScrollView sv:
                sv.LastKnownScreenSize = screenSize;
                foreach (var child in sv.GetChildren())
                    PropagateScreenSize(child, screenSize);
                return;

            case UIStackPanel sp:
                sp.LastKnownScreenSize = screenSize;
                foreach (var child in sp.GetChildren())
                    PropagateScreenSize(child, screenSize);
                return;

            case UIGrid grid:
                grid.LastKnownScreenSize = screenSize;
                foreach (var child in grid.GetChildren())
                    PropagateScreenSize(child, screenSize);
                return;

            case UIDialog dialog:
                foreach (var child in dialog.GetChildren())
                    PropagateScreenSize(child, screenSize);
                return;
        }
    }

    /// <summary>
    /// Ordered list of components on this canvas (front = last, back = first).
    /// Exposed internally for testing.
    /// </summary>
    internal IReadOnlyList<IUIComponent> Components => _components.AsReadOnly();

    /// <summary>
    /// Stable ZOrder-sorted view of <see cref="_components"/>.
    /// Equal <see cref="IUIComponent.ZOrder"/> values preserve insertion order.
    /// </summary>
    private IEnumerable<IUIComponent> InRenderOrder() =>
        _components.OrderBy(c => c.ZOrder);

    /// <summary>
    /// The component that currently holds keyboard focus, or null when nothing is focused.
    /// </summary>
    public IUIComponent? FocusedWidget => _focusedWidget;

    public UICanvas(IInputContext input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    /// <summary>
    /// Creates a canvas subscribed to <see cref="WindowResizedEvent"/> on
    /// <paramref name="eventBus"/> so <see cref="ScreenSize"/> tracks window resizes.
    /// The subscription is released on <see cref="Dispose"/>.
    /// </summary>
    public UICanvas(IInputContext input, IEventBus eventBus)
        : this(input)
    {
        _resizeSubscription = eventBus.Subscribe<WindowResizedEvent>(e =>
            ScreenSize = new Vector2(e.Width, e.Height));
    }

    /// <summary>
    /// Adds a component to the canvas. <see cref="UIDialog"/> is auto-centered;
    /// <see cref="UIDropdown.ScreenHeight"/> and <see cref="UIMenuBar"/> state are
    /// initialised from the current <see cref="ScreenSize"/>.
    /// </summary>
    public void Add(IUIComponent component)
    {
        if (component is UIDialog dialog)
            dialog.CenterOnScreen(_screenSize);

        if (component is UIMenuBar mb)
        {
            _menuBars.Add(mb);
            _activeMenuBar = mb;
        }

        PropagateScreenSize(component, _screenSize);

        _components.Add(component);
    }

    /// <summary>
    /// Searches all components (including nested children) for the first one whose
    /// <see cref="IUIComponent.Name"/> matches <paramref name="name"/>.
    /// Returns <c>null</c> if not found.
    /// </summary>
    public IUIComponent? FindByName(string name)
    {
        foreach (var component in _components)
        {
            var found = FindByNameIn(component, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Typed overload — returns the first component with the given <paramref name="name"/>
    /// that is assignable to <typeparamref name="T"/>, or <c>null</c>.
    /// </summary>
    public T? FindByName<T>(string name) where T : class, IUIComponent =>
        FindByName(name) as T;

    private static IUIComponent? FindByNameIn(IUIComponent component, string name)
    {
        if (component.Name == name) return component;

        IEnumerable<IUIComponent>? children = component switch
        {
            UIPanel p        => p.GetChildren(),
            UIScrollView sv  => sv.GetChildren(),
            UIStackPanel sp  => sp.GetChildren(),
            UIGrid g         => g.GetChildren(),
            UIDialog d       => d.GetChildren().Concat<IUIComponent>(d.GetButtons()),
            UITabContainer t => Enumerable.Range(0, t.TabCount).SelectMany(i => t.GetTabComponents(i)),
            _                => null
        };

        if (children == null) return null;
        foreach (var child in children)
        {
            var found = FindByNameIn(child, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Adds a world-space overlay component. Its <see cref="IUIWorldComponent.WorldPosition"/>
    /// is projected to screen coordinates via <see cref="WorldCamera"/> on every
    /// <see cref="Render"/> call.
    /// </summary>
    public void AddWorldComponent(IUIWorldComponent component)
    {
        _worldComponents.Add(component);
    }

    /// <summary>Removes a world-space overlay component.</summary>
    public void RemoveWorldComponent(IUIWorldComponent component)
    {
        _worldComponents.Remove(component);
    }

    public void Remove(IUIComponent component)
    {
        _components.Remove(component);

        if (_hoveredButton == component)
        {
            _hoveredButton.SetHovered(false);
            _hoveredButton = null;
        }

        if (_pressedButton == component)
        {
            _pressedButton.SetPressed(false);
            _pressedButton = null;
        }

        if (_dialogPressedButton == component)
        {
            _dialogPressedButton.SetPressed(false);
            _dialogPressedButton = null;
        }

        if (_activeSlider == component)
        {
            _activeSlider.EndDrag();
            _activeSlider = null;
            _activeSliderInputOffset = Vector2.Zero;
        }

        if (_focusedTextInput == component)
        {
            _focusedTextInput.SetFocused(false, _input);
            _focusedTextInput = null;
        }

        if (_focusedTextArea == component)
        {
            _focusedTextArea.SetFocused(false, _input);
            _focusedTextArea = null;
        }

        if (_hoveredCheckbox == component)
            _hoveredCheckbox = null;

        if (_activeDropdown == component)
        {
            _activeDropdown.Close();
            _activeDropdown = null;
            _activeDropdownInputOffset = Vector2.Zero;
        }

        if (_hoveredRadioButton == component)
        {
            _hoveredRadioButton.SetHovered(false);
            _hoveredRadioButton = null;
        }

        if (_hoveredTabContainer == component)
        {
            _hoveredTabContainer = null;
            _hoveredTabContainerInputOffset = Vector2.Zero;
        }

        if (_focusedTabContainer == component)
        {
            _focusedTabContainer.SetFocused(false);
            _focusedTabContainer = null;
        }

        if (_activeScrollView == component)
            _activeScrollView = null;

        if (_scrollbarDragView == component)
        {
            _scrollbarDragView.EndScrollbarDrag();
            _scrollbarDragView = null;
        }

        if (_activeDialog == component)
            _activeDialog = null;

        if (_draggingDialog == component)
        {
            _draggingDialog = null;
            _dialogDragOffset = Vector2.Zero;
        }

        if (_hoveredSpinBox == component)
            _hoveredSpinBox = null;

        if (_hoveredVirtualList == component)
        {
            (_hoveredVirtualList as UIVirtualListBase)?.ClearHover();
            _hoveredVirtualList = null;
            _hoveredVirtualListInputOffset = Vector2.Zero;
        }

        if (_thumbDragVirtualList == component)
        {
            (_thumbDragVirtualList as UIVirtualListBase)?.EndThumbDrag();
            _thumbDragVirtualList = null;
            _thumbDragVirtualListInputOffset = Vector2.Zero;
        }

        if (_hoveredTreeView == component)
        {
            _hoveredTreeView.ClearHover();
            _hoveredTreeView = null;
            _hoveredTreeViewInputOffset = Vector2.Zero;
        }

        if (_thumbDragTreeView == component)
        {
            _thumbDragTreeView.EndThumbDrag();
            _thumbDragTreeView = null;
            _thumbDragTreeViewInputOffset = Vector2.Zero;
        }

        if (_dragSource == component)
            CancelDrag();

        if (component is UIMenuBar removedMb)
        {
            removedMb.CloseMenu();
            _menuBars.Remove(removedMb);
            if (_activeMenuBar == removedMb)
                _activeMenuBar = _menuBars.Count > 0 ? _menuBars[^1] : null;
        }

        if (component is UIDropTarget dt)
        {
            _dropTargets.Remove(dt);
            if (_hoveredDropTarget == dt)
                _hoveredDropTarget = null;
        }

        _draggableRegistry.Remove(component);

        if (_tooltipOwner == component)
        {
            _activeTooltip?.OnHoverEnd();
            _activeTooltip = null;
            _tooltipOwner = null;
        }

        if (_focusedWidget == component)
            ClearWidgetFocus();

        // Clear focus on any descendant still tracked as the focused widget.
        if (_focusedWidget != null && ContainsDescendant(component, _focusedWidget))
            ClearWidgetFocus();

        // Release any UIRadioButton registrations so the group does not keep stale references.
        UnregisterRadioButtons(component);
    }

    /// <summary>
    /// Recursively unregisters all <see cref="UIRadioButton"/> instances in
    /// <paramref name="component"/> from their <see cref="UIRadioButtonGroup"/>.
    /// Called from <see cref="Remove"/> to avoid stale group references.
    /// </summary>
    private static void UnregisterRadioButtons(IUIComponent component)
    {
        if (component is UIRadioButton rb)
        {
            rb.Group.UnregisterButton(rb);
            return;
        }

        switch (component)
        {
            case UIPanel panel:
                foreach (var child in panel.GetChildren())
                    UnregisterRadioButtons(child);
                break;
            case UIScrollView sv:
                foreach (var child in sv.GetChildren())
                    UnregisterRadioButtons(child);
                break;
            case UIStackPanel sp:
                foreach (var child in sp.GetChildren())
                    UnregisterRadioButtons(child);
                break;
            case UIGrid grid:
                foreach (var child in grid.GetChildren())
                    UnregisterRadioButtons(child);
                break;
            case UITabContainer tab:
                for (int t = 0; t < tab.TabCount; t++)
                    foreach (var child in tab.GetTabComponents(t))
                        UnregisterRadioButtons(child);
                break;
            case UIDialog dialog:
                foreach (var child in dialog.GetChildren())
                    UnregisterRadioButtons(child);
                break;
        }
    }

    /// <summary>
    /// Moves a component to the top of the draw/input order. If it is not a direct child,
    /// the owning top-level container is brought to front instead.
    /// No-op if the component is not on this canvas or is already at the front.
    /// </summary>
    public void BringToFront(IUIComponent component)
    {
        // Direct child — fast path.
        int index = _components.IndexOf(component);
        if (index >= 0)
        {
            if (index != _components.Count - 1)
            {
                _components.RemoveAt(index);
                _components.Add(component);
            }
            return;
        }

        // Not a direct child — find the top-level container that owns it.
        for (int i = 0; i < _components.Count; i++)
        {
            if (ContainsDescendant(_components[i], component))
            {
                if (i != _components.Count - 1)
                {
                    var owner = _components[i];
                    _components.RemoveAt(i);
                    _components.Add(owner);
                }
                return;
            }
        }
    }

    private static bool ContainsDescendant(IUIComponent container, IUIComponent target)
    {
        switch (container)
        {
            case UIPanel panel:
                foreach (var child in panel.GetChildren())
                {
                    if (child == target || ContainsDescendant(child, target)) return true;
                }
                break;
            case UIScrollView sv:
                foreach (var child in sv.GetChildren())
                {
                    if (child == target || ContainsDescendant(child, target)) return true;
                }
                break;
            case UITabContainer tab:
                for (int t = 0; t < tab.TabCount; t++)
                {
                    foreach (var child in tab.GetTabComponents(t))
                    {
                        if (child == target || ContainsDescendant(child, target)) return true;
                    }
                }
                break;
            case UIDialog dialog:
                foreach (var btn in dialog.GetButtons())
                {
                    if (btn == target || ContainsDescendant(btn, target)) return true;
                }
                foreach (var child in dialog.GetChildren())
                {
                    if (child == target || ContainsDescendant(child, target)) return true;
                }
                break;
            case UIStackPanel sp:
                foreach (var child in sp.GetChildren())
                {
                    if (child == target || ContainsDescendant(child, target)) return true;
                }
                break;
            case UIGrid grid:
                foreach (var child in grid.GetChildren())
                {
                    if (child == target || ContainsDescendant(child, target)) return true;
                }
                break;
        }
        return false;
    }

    /// <summary>
    /// Moves a component to the start of the draw/input order so it renders behind all others.
    /// No-op if the component is not on this canvas or is already at the back.
    /// </summary>
    public void SendToBack(IUIComponent component)
    {
        int index = _components.IndexOf(component);
        if (index > 0)
        {
            _components.RemoveAt(index);
            _components.Insert(0, component);
        }
    }

    /// <summary>
    /// Opens <paramref name="menu"/> as a top-level overlay at <paramref name="position"/>,
    /// clamped to <see cref="ScreenSize"/>. Closes any previously open context menu first.
    /// </summary>
    public void ShowContextMenu(UIContextMenu menu, Vector2 position)
    {
        CloseContextMenu();

        float x = Math.Clamp(position.X, 0f, Math.Max(0f, _screenSize.X - menu.Width));
        float y = Math.Clamp(position.Y, 0f, Math.Max(0f, _screenSize.Y - menu.Height));
        menu.Position = new Vector2(x, y);

        _activeContextMenu = menu;
        _contextMenuHoveredIndex = -1;
    }

    /// <summary>
    /// Closes the active context menu, if any, and fires <see cref="UIContextMenu.OnClosed"/>.
    /// </summary>
    public void CloseContextMenu()
    {
        if (_activeContextMenu == null) return;
        _activeContextMenu.FireClosed();
        _activeContextMenu = null;
        _contextMenuHoveredIndex = -1;
    }

    /// <summary>
    /// The context menu currently shown as an overlay, or <c>null</c> when none is open.
    /// </summary>
    public UIContextMenu? ActiveContextMenu => _activeContextMenu;

    /// <summary>
    /// The most recently added or clicked menu bar on this canvas, or <c>null</c> if none has been added.
    /// </summary>
    public UIMenuBar? ActiveMenuBar => _activeMenuBar;

    /// <summary>
    /// Corner of the screen where toast notifications are stacked.
    /// Defaults to <see cref="ToastAnchor.BottomRight"/>.
    /// </summary>
    public ToastAnchor ToastAnchor { get; set; } = ToastAnchor.BottomRight;

    /// <summary>
    /// Gap in pixels between the screen edge and the toast stack, and between individual toasts.
    /// </summary>
    public float ToastPadding { get; set; } = 12f;

    /// <summary>
    /// Maximum number of toasts shown simultaneously. Older toasts are dismissed first when
    /// the limit is exceeded. 0 = unlimited.
    /// </summary>
    public int MaxVisibleToasts { get; set; } = 5;

    /// <summary>
    /// Enqueues a toast. When <see cref="MaxVisibleToasts"/> would be exceeded,
    /// the oldest toast is dismissed immediately to make room.
    /// </summary>
    public void ShowToast(UIToast toast)
    {
        if (MaxVisibleToasts > 0)
        {
            while (_toasts.Count >= MaxVisibleToasts && _toasts.Count > 0)
            {
                var oldest = _toasts[0];
                _toasts.RemoveAt(0);
                oldest.FireDismissed();
            }
        }
        _toasts.Add(toast);
    }

    /// <summary>
    /// Starts early dismissal of <paramref name="toast"/>. No-op if not active.
    /// </summary>
    public void DismissToast(UIToast toast)
    {
        if (_toasts.Contains(toast))
            toast.RequestDismiss();
    }

    /// <summary>
    /// Read-only view of currently active toasts (oldest first).
    /// </summary>
    public IReadOnlyList<UIToast> ActiveToasts => _toasts.AsReadOnly();

    /// <summary>
    /// Registers <paramref name="component"/> as a drag source carrying <paramref name="payload"/>.
    /// Once the user drags past the threshold the ghost overlay appears.
    /// </summary>
    public void RegisterDraggable(IUIComponent component, IDragPayload payload)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(payload);
        _draggableRegistry[component] = payload;
    }

    /// <summary>
    /// Removes a previously registered draggable component. No-op if not registered.
    /// </summary>
    public void UnregisterDraggable(IUIComponent component) =>
        _draggableRegistry.Remove(component);

    /// <summary>
    /// Registers <paramref name="target"/> as a drop zone. It must already be on the canvas.
    /// </summary>
    public void RegisterDropTarget(UIDropTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!_dropTargets.Contains(target))
            _dropTargets.Add(target);
    }

    /// <summary>
    /// Removes a previously registered drop target. No-op if not registered.
    /// </summary>
    public void UnregisterDropTarget(UIDropTarget target) =>
        _dropTargets.Remove(target);

    /// <summary>
    /// Registers a <see cref="UITween"/> for automatic advancement each <see cref="Update"/>.
    /// Removed automatically on completion (looping tweens run until <see cref="StopTween"/> is called).
    /// </summary>
    public void StartTween(UITween tween)
    {
        ArgumentNullException.ThrowIfNull(tween);
        if (!_activeTweens.Contains(tween))
            _activeTweens.Add(tween);
    }

    /// <summary>
    /// Registers a <see cref="UITweenSequence"/> for automatic advancement each <see cref="Update"/>.
    /// Removed automatically on completion.
    /// </summary>
    public void StartTween(UITweenSequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        if (!_activeTweenSequences.Contains(sequence))
            _activeTweenSequences.Add(sequence);
    }

    /// <summary>
    /// Removes a tween from the active list. No-op if not registered.
    /// Does not call <see cref="UITween.Complete"/>.
    /// </summary>
    public void StopTween(UITween tween) => _activeTweens.Remove(tween);

    /// <summary>
    /// Removes a sequence from the active list. No-op if it is not registered.
    /// </summary>
    public void StopTween(UITweenSequence sequence) => _activeTweenSequences.Remove(sequence);

    /// <summary>
    /// Stops and removes all active tweens and sequences.
    /// </summary>
    public void StopAllTweens()
    {
        _activeTweens.Clear();
        _activeTweenSequences.Clear();
    }

    /// <summary>
    /// Read-only view of currently running tweens.
    /// </summary>
    public IReadOnlyList<UITween> ActiveTweens => _activeTweens.AsReadOnly();

    /// <summary>
    /// Read-only view of currently running tween sequences.
    /// </summary>
    public IReadOnlyList<UITweenSequence> ActiveTweenSequences => _activeTweenSequences.AsReadOnly();

    /// <summary>
    /// Whether a drag operation is currently in progress.
    /// </summary>
    public bool IsDragging => _dragActive;

    /// <summary>
    /// The component currently being dragged, or <c>null</c>.
    /// </summary>
    public IUIComponent? DragSource => _dragSource;

    public void Clear()
    {
        _components.Clear();
        _inputDispatchBuffer.Clear();
        _hoveredButton = null;
        _pressedButton = null;
        _dialogPressedButton = null;
        _activeSlider = null;
        _activeSliderInputOffset = Vector2.Zero;
        if (_focusedTextInput != null)
        {
            _focusedTextInput.SetFocused(false, _input);
            _focusedTextInput = null;
        }
        if (_focusedTextArea != null)
        {
            _focusedTextArea.SetFocused(false, _input);
            _focusedTextArea = null;
        }
        _hoveredCheckbox = null;
        _hoveredSpinBox = null;
        _hoveredVirtualList?.ClearHover();
        _hoveredVirtualList = null;
        _hoveredVirtualListInputOffset = Vector2.Zero;
        _thumbDragVirtualList?.EndThumbDrag();
        _thumbDragVirtualList = null;
        _thumbDragVirtualListInputOffset = Vector2.Zero;
        _hoveredTreeView?.ClearHover();
        _hoveredTreeView = null;
        _hoveredTreeViewInputOffset = Vector2.Zero;
        _thumbDragTreeView?.EndThumbDrag();
        _thumbDragTreeView = null;
        _thumbDragTreeViewInputOffset = Vector2.Zero;
        _activeDropdown?.Close();
        _activeDropdown = null;
        _activeDropdownInputOffset = Vector2.Zero;
        _hoveredRadioButton = null;
        _hoveredTabContainer = null;
        _hoveredTabContainerInputOffset = Vector2.Zero;
        _focusedTabContainer?.SetFocused(false);
        _focusedTabContainer = null;
        _activeScrollView = null;
        _scrollbarDragView?.EndScrollbarDrag();
        _scrollbarDragView = null;
        _dialogScrollbarDragView?.EndScrollbarDrag();
        _dialogScrollbarDragView = null;
        _activeDialog = null;
        _draggingDialog = null;
        _dialogDragOffset = Vector2.Zero;
        _activeDialogDropdown?.Close();
        _activeDialogDropdown = null;
        _activeTooltip = null;
        ClearWidgetFocus();
        _tooltipOwner = null;
        CloseContextMenu();
        foreach (var t in _toasts) t.FireDismissed();
        _toasts.Clear();
        CancelDrag();
        _draggableRegistry.Clear();
        _dropTargets.Clear();
        _activeTweens.Clear();
        _activeTweenSequences.Clear();
        _worldComponents.Clear();
        foreach (var mb in _menuBars) mb.CloseMenu();
        _menuBars.Clear();
        _activeMenuBar = null;
    }

    public void Update(float deltaTime)
    {
        foreach (var component in InRenderOrder())
        {
            if (component.Enabled)
                component.Update(deltaTime);
        }

        _activeTooltip?.Update(deltaTime);

        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            _toasts[i].Update(deltaTime);
            if (_toasts[i].IsExpired)
            {
                _toasts[i].FireDismissed();
                _toasts.RemoveAt(i);
            }
        }

        for (int i = _activeTweens.Count - 1; i >= 0; i--)
        {
            _activeTweens[i].Update(deltaTime);
            if (_activeTweens[i].IsComplete)
                _activeTweens.RemoveAt(i);
        }

        for (int i = _activeTweenSequences.Count - 1; i >= 0; i--)
        {
            _activeTweenSequences[i].Update(deltaTime);
            if (_activeTweenSequences[i].IsComplete)
                _activeTweenSequences.RemoveAt(i);
        }

        foreach (var wc in _worldComponents)
        {
            if (wc.Enabled)
                wc.Update(deltaTime);
        }
    }

    public void Render(IRenderer renderer)
    {
        var previousCamera = renderer.Camera;
        renderer.Camera = null;

        var ordered = InRenderOrder().ToList();

        // Ensure only the topmost visible dialog draws its overlay.
        UIDialog? topmostDialog = null;
        for (int i = ordered.Count - 1; i >= 0; i--)
        {
            if (ordered[i] is UIDialog d && d.Visible)
            {
                topmostDialog = d;
                break;
            }
        }
        foreach (var component in ordered)
        {
            if (component is UIDialog dlg)
                dlg.SuppressOverlay = dlg != topmostDialog;
        }

        foreach (var component in ordered)
        {
            if (!component.Visible) continue;

            if (component is IAnchoredUIComponent anchored &&
                (anchored.Anchor != UIAnchor.TopLeft || anchored.AnchorOffset != Vector2.Zero))
            {
                var anchorOrigin = UIAnchorResolver.Resolve(anchored.Anchor, _screenSize.X, _screenSize.Y);
                var resolvedPosition = anchorOrigin + anchored.AnchorOffset;

                var saved = anchored.Position;
                anchored.Position = resolvedPosition;
                component.Render(renderer);
                anchored.Position = saved;
            }
            else
            {
                component.Render(renderer);
            }
        }

        if (_activeTooltip != null && _activeTooltip.Visible)
            _activeTooltip.Render(renderer);

        RenderToasts(renderer);

        _activeContextMenu?.Render(renderer, _contextMenuHoveredIndex);

        foreach (var mb in _menuBars) mb.RenderSubmenuOverlay(renderer);

        if (_dragActive)
            RenderDragGhost(renderer);

        RenderWorldComponents(renderer);

        // Render any active dropdown that was suppressed
        if (_activeDropdown != null && _activeDropdown.IsExpanded && _activeDropdownNeedsOverlay)
        {
            // Position is content-relative; subtract InputOffset to get screen-space position.
            var screenPos = _activeDropdown.Position - _activeDropdownInputOffset;
            _activeDropdown.RenderListOverlay(renderer, screenPos);
        }

        renderer.Camera = previousCamera;
    }

    /// <summary>
    /// Processes gamepad input for UI navigation. Returns <c>true</c> if input was consumed.
    /// D-Pad/left-stick up/down cycles focus; left/right nudges sliders or switches tabs.
    /// A activates the focused widget; B dismisses the active modal dialog.
    /// Navigation is scoped to the dialog when one is active.
    /// </summary>
    public bool ProcessGamepadInput(IInputContext input, bool consumed, int gamepadIndex = 0)
    {
        if (consumed || !input.IsGamepadConnected(gamepadIndex))
            return false;

        bool down = input.IsGamepadButtonPressed(GamepadButton.DPadDown, gamepadIndex) ||
                    (input.IsGamepadAxisPressed(GamepadAxis.LeftY, gamepadIndex) && input.GetGamepadAxis(GamepadAxis.LeftY, gamepadIndex) > 0);
        bool up = input.IsGamepadButtonPressed(GamepadButton.DPadUp, gamepadIndex) ||
                  (input.IsGamepadAxisPressed(GamepadAxis.LeftY, gamepadIndex) && input.GetGamepadAxis(GamepadAxis.LeftY, gamepadIndex) < 0);
        bool right = input.IsGamepadButtonPressed(GamepadButton.DPadRight, gamepadIndex) ||
                     (input.IsGamepadAxisPressed(GamepadAxis.LeftX, gamepadIndex) && input.GetGamepadAxis(GamepadAxis.LeftX, gamepadIndex) > 0);
        bool left = input.IsGamepadButtonPressed(GamepadButton.DPadLeft, gamepadIndex) ||
                    (input.IsGamepadAxisPressed(GamepadAxis.LeftX, gamepadIndex) && input.GetGamepadAxis(GamepadAxis.LeftX, gamepadIndex) < 0);
        bool activate = input.IsGamepadButtonPressed(GamepadButton.A, gamepadIndex);
        bool cancel = input.IsGamepadButtonPressed(GamepadButton.B, gamepadIndex);

        bool anyNav = down || up || right || left || activate || cancel;
        if (!anyNav) return false;

        var activeDialog = FindActiveDialog();
        if (activeDialog != null)
        {
            if (cancel)
                activeDialog.EscapeDismiss();

            if (down)
                CycleFocusableWidgetsInDialog(activeDialog, isReverse: false, input);
            else if (up)
                CycleFocusableWidgetsInDialog(activeDialog, isReverse: true, input);

            if (_focusedWidget != null && (activate || left || right))
                ActivateFocusedWidget(input, activate, left, right);

            return true;
        }

        if (down)
            CycleFocusableWidgets(isReverse: false, input);
        else if (up)
            CycleFocusableWidgets(isReverse: true, input);

        if (_focusedWidget != null && (activate || left || right))
            ActivateFocusedWidget(input, activate, left, right);

        if (_focusedTabContainer != null)
        {
            if (right) { _focusedTabContainer.SelectNextTab(); return true; }
            if (left) { _focusedTabContainer.SelectPreviousTab(); return true; }
        }

        return anyNav;
    }

    /// <summary>
    /// Processes keyboard input for UI. Returns <c>true</c> if input was consumed.
    /// When <paramref name="consumed"/> is true, unfocuses any active text input.
    /// Tab/Shift+Tab cycles focus; Enter/Space activates; arrow keys nudge sliders.
    /// All input is blocked while a modal dialog is active.
    /// </summary>
    public bool ProcessKeyboardInput(IInputContext input, bool consumed)
    {
        ValidateFocusedWidgets();

        if (_activeContextMenu != null)
        {
            if (input.IsKeyPressed(Key.Escape))
                CloseContextMenu();
            return true;
        }

        var openKbMb = _menuBars.Find(mb => mb.IsOpen);
        if (openKbMb != null)
        {
            if (input.IsKeyPressed(Key.Escape))
            { openKbMb.CloseMenu(); return true; }
            if (input.IsKeyPressed(Key.Left))
            { openKbMb.KeyboardMoveLeft(); return true; }
            if (input.IsKeyPressed(Key.Right))
            { openKbMb.KeyboardMoveRight(); return true; }
            if (input.IsKeyPressed(Key.Up))
            { openKbMb.KeyboardMoveUp(); return true; }
            if (input.IsKeyPressed(Key.Down))
            { openKbMb.KeyboardMoveDown(); return true; }
            if (input.IsKeyPressed(Key.Enter))
            { openKbMb.KeyboardActivate(); return true; }
            return true;
        }

        if (consumed)
        {
            if (_focusedTextInput != null && _focusedTextInput.IsFocused)
            {
                _focusedTextInput.SetFocused(false, input);
                _focusedTextInput = null;
            }
            if (_focusedTextArea != null && _focusedTextArea.IsFocused)
            {
                _focusedTextArea.SetFocused(false, input);
                _focusedTextArea = null;
            }
            return false;
        }

        var escapeDialog = FindActiveDialog();
        if (escapeDialog != null)
        {
            if (input.IsKeyPressed(Key.Escape))
                escapeDialog.EscapeDismiss();

            if (input.IsKeyPressed(Key.Tab))
            {
                if (_focusedTextArea != null && _focusedTextArea.IsFocused && _focusedTextArea.TabInsertsTab)
                {
                    _focusedTextArea.InsertTab();
                    return true;
                }
                CycleFocusableWidgetsInDialog(escapeDialog, input);
                return true;
            }

            if (_focusedTextInput != null && _focusedTextInput.IsFocused)
            {
                HandleTextInputKeyboard();
                return true;
            }

            if (_focusedTextArea != null && _focusedTextArea.IsFocused)
            {
                HandleTextAreaKeyboard();
                return true;
            }

            if (_focusedWidget != null)
            {
                bool activate = input.IsKeyPressed(Key.Enter) || input.IsKeyPressed(Key.Space);

                switch (_focusedWidget)
                {
                    case UIButton btn when activate:
                        btn.Click();
                        return true;

                    case UICheckbox cb when activate:
                        cb.Toggle();
                        return true;

                    case UIRadioButton rb when activate:
                        rb.Select();
                        return true;

                    case UIDropdown dd:
                        if (input.IsKeyPressed(Key.Up))
                        { dd.NavigateItem(-1); return true; }
                        if (input.IsKeyPressed(Key.Down))
                        { dd.NavigateItem(1); return true; }
                        if (activate)
                        {
                            if (dd.IsExpanded) dd.ConfirmKeyboardSelection();
                            else dd.Toggle();
                            return true;
                        }
                        break;

                    case UISlider sl:
                        bool slHandled = false;
                        if (input.IsKeyPressed(Key.Right) || input.IsKeyPressed(Key.Up))
                        { sl.NudgeValue(1f); slHandled = true; }
                        else if (input.IsKeyPressed(Key.Left) || input.IsKeyPressed(Key.Down))
                        { sl.NudgeValue(-1f); slHandled = true; }
                        if (slHandled) return true;
                        break;

                    case UISpinBox sbD:
                        bool sbDHandled = false;
                        if (input.IsKeyPressed(Key.Right) || input.IsKeyPressed(Key.Up))
                        { sbD.NudgeValue(1f); sbDHandled = true; }
                        else if (input.IsKeyPressed(Key.Left) || input.IsKeyPressed(Key.Down))
                        { sbD.NudgeValue(-1f); sbDHandled = true; }
                        else if (input.IsKeyPressed(Key.Enter)) { sbD.CommitEdit(); sbDHandled = true; }
                        else if (input.IsKeyPressed(Key.Escape)) { sbD.CancelEdit(); sbDHandled = true; }
                        if (sbDHandled) return true;
                        break;

                    case UIScrollView svd:
                        bool svdHandled = false;
                        if (input.IsKeyPressed(Key.Up))
                        { svd.HandleScroll(1f); svdHandled = true; }
                        else if (input.IsKeyPressed(Key.Down))
                        { svd.HandleScroll(-1f); svdHandled = true; }
                        else if (input.IsKeyPressed(Key.PageUp))
                        { svd.HandlePageScroll(-1f); svdHandled = true; }
                        else if (input.IsKeyPressed(Key.PageDown))
                        { svd.HandlePageScroll(1f); svdHandled = true; }
                        else if (input.IsKeyPressed(Key.Home))
                        { svd.ScrollToTop(); svdHandled = true; }
                        else if (input.IsKeyPressed(Key.End))
                        { svd.ScrollToBottom(); svdHandled = true; }
                        if (svdHandled) return true;
                        break;

                    case UIVirtualListBase vlD:
                        bool vlDHandled = false;
                        if (input.IsKeyPressed(Key.Up))
                        { vlD.NavigateUp(); vlDHandled = true; }
                        else if (input.IsKeyPressed(Key.Down))
                        { vlD.NavigateDown(); vlDHandled = true; }
                        else if (input.IsKeyPressed(Key.PageUp))
                        { vlD.HandlePageScroll(-1f); vlDHandled = true; }
                        else if (input.IsKeyPressed(Key.PageDown))
                        { vlD.HandlePageScroll(1f); vlDHandled = true; }
                        else if (input.IsKeyPressed(Key.Home))
                        { vlD.ScrollToTop(); vlDHandled = true; }
                        else if (input.IsKeyPressed(Key.End))
                        { vlD.ScrollToBottom(); vlDHandled = true; }
                        if (vlDHandled) return true;
                        break;

                    case UITreeView tvD:
                        bool tvDHandled = false;
                        if (input.IsKeyPressed(Key.Up))
                        { tvD.NavigateUp(); tvDHandled = true; }
                        else if (input.IsKeyPressed(Key.Down))
                        { tvD.NavigateDown(); tvDHandled = true; }
                        else if (input.IsKeyPressed(Key.Right))
                        { tvD.ExpandSelected(); tvDHandled = true; }
                        else if (input.IsKeyPressed(Key.Left))
                        { tvD.CollapseSelected(); tvDHandled = true; }
                        else if (input.IsKeyPressed(Key.PageUp))
                        { tvD.HandlePageScroll(-1f); tvDHandled = true; }
                        else if (input.IsKeyPressed(Key.PageDown))
                        { tvD.HandlePageScroll(1f); tvDHandled = true; }
                        if (tvDHandled) return true;
                        break;
                }
            }

            return true;
        }

        if (input.IsKeyPressed(Key.Tab))
        {
            if (_focusedTextArea != null && _focusedTextArea.IsFocused && _focusedTextArea.TabInsertsTab)
            {
                _focusedTextArea.InsertTab();
                return true;
            }
            CycleFocusableWidgets(input);
            return true;
        }

        // Escape blurs any focused text field (when no modal dialog is intercepting Escape).
        if (input.IsKeyPressed(Key.Escape))
        {
            if (_focusedTextInput != null && _focusedTextInput.IsFocused)
            {
                _focusedTextInput.SetFocused(false, input);
                _focusedTextInput = null;
                ClearWidgetFocus();
                return true;
            }

            if (_focusedTextArea != null && _focusedTextArea.IsFocused)
            {
                _focusedTextArea.SetFocused(false, input);
                _focusedTextArea = null;
                ClearWidgetFocus();
                return true;
            }
        }

        // Focused text input gets all keyboard events.
        if (_focusedTextInput != null && _focusedTextInput.IsFocused)
        {
            HandleTextInputKeyboard();
            return true;
        }

        // Focused text area gets all keyboard events.
        if (_focusedTextArea != null && _focusedTextArea.IsFocused)
        {
            HandleTextAreaKeyboard();
            return true;
        }

        // Activate the focused widget with Enter or Space.
        if (_focusedWidget != null)
        {
            bool activate = input.IsKeyPressed(Key.Enter) || input.IsKeyPressed(Key.Space);

            switch (_focusedWidget)
            {
                case UIButton btn when activate:
                    btn.Click();
                    return true;

                case UICheckbox cb when activate:
                    cb.Toggle();
                    return true;

                case UIRadioButton rb when activate:
                    rb.Select();
                    return true;

                case UIDropdown dd:
                    if (input.IsKeyPressed(Key.Up))
                    { dd.NavigateItem(-1); return true; }
                    if (input.IsKeyPressed(Key.Down))
                    { dd.NavigateItem(1); return true; }
                    if (activate)
                    {
                        if (dd.IsExpanded) dd.ConfirmKeyboardSelection();
                        else dd.Toggle();
                        return true;
                    }
                    break;

                case UISlider sl:
                    bool handled = false;
                    if (input.IsKeyPressed(Key.Right) || input.IsKeyPressed(Key.Up))
                    { sl.NudgeValue(1f); handled = true; }
                    else if (input.IsKeyPressed(Key.Left) || input.IsKeyPressed(Key.Down))
                    { sl.NudgeValue(-1f); handled = true; }
                    if (handled) return true;
                    break;

                case UISpinBox sb:
                    bool sbHandled = false;
                    if (input.IsKeyPressed(Key.Right) || input.IsKeyPressed(Key.Up))
                    { sb.NudgeValue(1f); sbHandled = true; }
                    else if (input.IsKeyPressed(Key.Left) || input.IsKeyPressed(Key.Down))
                    { sb.NudgeValue(-1f); sbHandled = true; }
                    else if (input.IsKeyPressed(Key.Enter)) { sb.CommitEdit(); sbHandled = true; }
                    else if (input.IsKeyPressed(Key.Escape)) { sb.CancelEdit(); sbHandled = true; }
                    if (sbHandled) return true;
                    break;

                case UIScrollView sv:
                    bool svHandled = false;
                    if (input.IsKeyPressed(Key.Up))
                    { sv.HandleScroll(1f); svHandled = true; }
                    else if (input.IsKeyPressed(Key.Down))
                    { sv.HandleScroll(-1f); svHandled = true; }
                    else if (input.IsKeyPressed(Key.PageUp))
                    { sv.HandlePageScroll(-1f); svHandled = true; }
                    else if (input.IsKeyPressed(Key.PageDown))
                    { sv.HandlePageScroll(1f); svHandled = true; }
                    else if (input.IsKeyPressed(Key.Home))
                    { sv.ScrollToTop(); svHandled = true; }
                    else if (input.IsKeyPressed(Key.End))
                    { sv.ScrollToBottom(); svHandled = true; }
                    if (svHandled) return true;
                    break;

                case UIVirtualListBase vl:
                    bool vlHandled = false;
                    if (input.IsKeyPressed(Key.Up))
                    { vl.NavigateUp(); vlHandled = true; }
                    else if (input.IsKeyPressed(Key.Down))
                    { vl.NavigateDown(); vlHandled = true; }
                    else if (input.IsKeyPressed(Key.PageUp))
                    { vl.HandlePageScroll(-1f); vlHandled = true; }
                    else if (input.IsKeyPressed(Key.PageDown))
                    { vl.HandlePageScroll(1f); vlHandled = true; }
                    else if (input.IsKeyPressed(Key.Home))
                    { vl.ScrollToTop(); vlHandled = true; }
                    else if (input.IsKeyPressed(Key.End))
                    { vl.ScrollToBottom(); vlHandled = true; }
                    if (vlHandled) return true;
                    break;

                case UITreeView tv:
                    bool tvHandled = false;
                    if (input.IsKeyPressed(Key.Up))
                    { tv.NavigateUp(); tvHandled = true; }
                    else if (input.IsKeyPressed(Key.Down))
                    { tv.NavigateDown(); tvHandled = true; }
                    else if (input.IsKeyPressed(Key.Right))
                    { tv.ExpandSelected(); tvHandled = true; }
                    else if (input.IsKeyPressed(Key.Left))
                    { tv.CollapseSelected(); tvHandled = true; }
                    else if (input.IsKeyPressed(Key.PageUp))
                    { tv.HandlePageScroll(-1f); tvHandled = true; }
                    else if (input.IsKeyPressed(Key.PageDown))
                    { tv.HandlePageScroll(1f); tvHandled = true; }
                    if (tvHandled) return true;
                    break;
            }
        }

        if (_focusedTabContainer != null)
        {
            if (input.IsKeyPressed(Key.Right)) { _focusedTabContainer.SelectNextTab(); return true; }
            if (input.IsKeyPressed(Key.Left)) { _focusedTabContainer.SelectPreviousTab(); return true; }
        }

        return false;
    }

    private static void CollectFocusableWidgets(IUIComponent component, List<IUIComponent> results)
    {
        if (!component.Enabled || !component.Visible) return;

        switch (component)
        {
            case UITextInput:
            case UITextArea:
            case UIButton:
            case UICheckbox:
            case UISlider:
            case UISpinBox:
            case UIRadioButton:
            case UIDropdown:
            case UIVirtualListBase:
            case UITreeView:
                results.Add(component);
                return;

            case UITabContainer tab:
                results.Add(tab);
                foreach (var child in tab.GetTabComponents(tab.SelectedTabIndex))
                    CollectFocusableWidgets(child, results);
                return;

            case UIScrollView sv:
            {
                var svChildren = new List<IUIComponent>();
                foreach (var child in sv.GetChildren())
                    CollectFocusableWidgets(child, svChildren);
                if (svChildren.Count > 0)
                    results.AddRange(svChildren);
                else
                    results.Add(sv);
                return;
            }

            case UIPanel panel:
                foreach (var child in panel.GetChildren())
                    CollectFocusableWidgets(child, results);
                return;

            case UIStackPanel sp:
                foreach (var child in sp.GetChildren())
                    CollectFocusableWidgets(child, results);
                return;

            case UIGrid grid:
                foreach (var child in grid.GetChildren())
                    CollectFocusableWidgets(child, results);
                return;
        }
    }

    private static void CollectTextInputs(IUIComponent component, List<UITextInput> results)
    {
        if (!component.Enabled || !component.Visible) return;

        if (component is UITextInput ti)
        {
            results.Add(ti);
            return;
        }

        if (component is UITabContainer tab)
        {
            foreach (var child in tab.GetTabComponents(tab.SelectedTabIndex))
                CollectTextInputs(child, results);
            return;
        }

        if (component is UIScrollView sv)
        {
            foreach (var child in sv.GetChildren())
                CollectTextInputs(child, results);
            return;
        }

        if (component is UIPanel panel)
        {
            foreach (var child in panel.GetChildren())
                CollectTextInputs(child, results);
        }
    }

    private void CycleFocusableWidgetsInDialog(UIDialog dialog, IInputContext input)
    {
        bool reverse = input.IsKeyDown(Key.LeftShift) || input.IsKeyDown(Key.RightShift);
        CycleFocusableWidgetsInDialog(dialog, isReverse: reverse, input);
    }

    private void CycleFocusableWidgetsInDialog(UIDialog dialog, bool isReverse, IInputContext input)
    {
        var widgets = new List<IUIComponent>();

        foreach (var btn in dialog.GetButtons())
            CollectFocusableWidgets(btn, widgets);

        foreach (var child in dialog.GetChildren())
            CollectFocusableWidgets(child, widgets);

        if (widgets.Count == 0) return;

        widgets.StableSortByTabIndex();

        int currentIndex = _focusedWidget != null ? widgets.IndexOf(_focusedWidget) : -1;

        int nextIndex = isReverse
            ? (currentIndex <= 0 ? widgets.Count - 1 : currentIndex - 1)
            : (currentIndex + 1) % widgets.Count;

        ClearWidgetFocus();

        _focusedWidget = widgets[nextIndex];
        ApplyWidgetFocus(_focusedWidget, true, input);
        ScrollFocusedWidgetIntoView(_focusedWidget);
    }

    private void CycleFocusableWidgets(IInputContext input)
    {
        bool reverse = input.IsKeyDown(Key.LeftShift) || input.IsKeyDown(Key.RightShift);
        CycleFocusableWidgets(isReverse: reverse, input);
    }

    private void CycleFocusableWidgets(bool isReverse, IInputContext input)
    {
        var widgets = new List<IUIComponent>();
        // Collect front-to-back (topmost/last-rendered first) so Tab order matches visual layering.
        var ordered = InRenderOrder().ToList();
        for (int i = ordered.Count - 1; i >= 0; i--)
            CollectFocusableWidgets(ordered[i], widgets);

        if (widgets.Count == 0) return;

        widgets.StableSortByTabIndex();

        int currentIndex = _focusedWidget != null ? widgets.IndexOf(_focusedWidget) : -1;

        int nextIndex = isReverse
            ? (currentIndex <= 0 ? widgets.Count - 1 : currentIndex - 1)
            : (currentIndex + 1) % widgets.Count;

        ClearWidgetFocus();

        _focusedWidget = widgets[nextIndex];
        ApplyWidgetFocus(_focusedWidget, true, input);
        ScrollFocusedWidgetIntoView(_focusedWidget);
    }

    private void ClearWidgetFocus()
    {
        if (_focusedWidget == null) return;
        ApplyWidgetFocus(_focusedWidget, false, _input);
        _focusedWidget = null;
    }

    /// <summary>
    /// Programmatically moves keyboard focus to <paramref name="component"/>.
    /// The component must already be added to this canvas (directly, or inside a
    /// <see cref="UIPanel"/>, <see cref="UIScrollView"/>, or <see cref="UITabContainer"/>)
    /// and must be a focusable type (<see cref="UIButton"/>, <see cref="UICheckbox"/>,
    /// <see cref="UISlider"/>, <see cref="UIRadioButton"/>, <see cref="UIDropdown"/>,
    /// <see cref="UIScrollView"/>, <see cref="UITabContainer"/>, or <see cref="UITextInput"/>).
    /// Returns <c>true</c> when focus was applied, <c>false</c> if the component is
    /// not focusable, not enabled, or not visible.
    /// </summary>
    public bool SetFocus(IUIComponent component)
    {
        if (!component.Enabled || !component.Visible)
            return false;

        if (component is not (UIButton or UICheckbox or UISlider or UISpinBox or UIRadioButton
            or UIDropdown or UIScrollView or UITabContainer or UITextInput or UITextArea))
            return false;

        ClearWidgetFocus();
        _focusedWidget = component;
        ApplyWidgetFocus(component, true, _input);
        return true;
    }

    private void ApplyWidgetFocus(IUIComponent widget, bool focused, IInputContext input)
    {
        switch (widget)
        {
            case UIButton btn: btn.SetFocused(focused); break;
            case UICheckbox cb: cb.SetFocused(focused); break;
            case UISlider sl: sl.SetFocused(focused); break;
            case UISpinBox sb: sb.SetFocused(focused); break;
            case UIRadioButton rb: rb.SetFocused(focused); break;
            case UIDropdown dd: dd.SetFocused(focused); break;
            case UIScrollView sv: sv.SetFocused(focused); break;
            case UITabContainer tab:
                tab.SetFocused(focused);
                if (focused) _focusedTabContainer = tab;
                else if (_focusedTabContainer == tab) _focusedTabContainer = null;
                break;
            case UITextInput ti:
                ti.SetFocused(focused, input);
                _focusedTextInput = focused ? ti : null;
                break;
            case UITextArea ta:
                ta.SetFocused(focused, input);
                _focusedTextArea = focused ? ta : null;
                break;
            case UIVirtualListBase vl:
                vl.SetFocused(focused);
                break;
            case UITreeView tv:
                tv.SetFocused(focused);
                break;
        }
    }

    /// <summary>
    /// Scrolls any <see cref="UIScrollView"/> that contains <paramref name="widget"/>
    /// so the widget is visible.
    /// </summary>
    private void ScrollFocusedWidgetIntoView(IUIComponent widget)
    {
        foreach (var component in _components)
        {
            if (TryScrollWidgetIntoViewInContainer(component, widget))
                return;
        }
    }

    /// <summary>
    /// Activates or nudges the focused widget. Shared by keyboard and gamepad paths.
    /// </summary>
    private bool ActivateFocusedWidget(IInputContext input, bool activate, bool nudgeLeft, bool nudgeRight)
    {
        if (_focusedWidget == null) return false;

        switch (_focusedWidget)
        {
            case UIButton btn when activate:
                btn.Click();
                return true;

            case UICheckbox cb when activate:
                cb.Toggle();
                return true;

            case UIRadioButton rb when activate:
                rb.Select();
                return true;

            case UIDropdown dd:
                if (activate)
                {
                    if (dd.IsExpanded) dd.ConfirmKeyboardSelection();
                    else dd.Toggle();
                    return true;
                }
                if (nudgeRight) { dd.NavigateItem(1); return true; }
                if (nudgeLeft) { dd.NavigateItem(-1); return true; }
                break;

            case UISlider sl:
                if (nudgeRight || activate) { sl.NudgeValue(1f); return true; }
                if (nudgeLeft) { sl.NudgeValue(-1f); return true; }
                break;

            case UISpinBox sb:
                if (nudgeRight || activate) { sb.NudgeValue(1f); return true; }
                if (nudgeLeft) { sb.NudgeValue(-1f); return true; }
                break;

            case UIScrollView sv:
                if (nudgeRight) { sv.HandleScroll(-1f); return true; }
                if (nudgeLeft) { sv.HandleScroll(1f); return true; }
                break;
        }

        return false;
    }

    private static bool TryScrollWidgetIntoViewInContainer(IUIComponent container, IUIComponent target)
    {
        if (container is UIScrollView sv)
        {
            foreach (var child in sv.GetChildren())
            {
                if (child == target)
                {
                    sv.ScrollToChild(target);
                    return true;
                }

                if (TryScrollWidgetIntoViewInContainer(child, target))
                    return true;
            }
        }
        else if (container is UIPanel panel)
        {
            foreach (var child in panel.GetChildren())
            {
                if (TryScrollWidgetIntoViewInContainer(child, target))
                    return true;
            }
        }
        else if (container is UITabContainer tab)
        {
            foreach (var child in tab.GetTabComponents(tab.SelectedTabIndex))
            {
                if (TryScrollWidgetIntoViewInContainer(child, target))
                    return true;
            }
        }
        else if (container is UIStackPanel sp)
        {
            foreach (var child in sp.GetChildren())
            {
                if (TryScrollWidgetIntoViewInContainer(child, target))
                    return true;
            }
        }
        else if (container is UIGrid grid)
        {
            foreach (var child in grid.GetChildren())
            {
                if (TryScrollWidgetIntoViewInContainer(child, target))
                    return true;
            }
        }
        else if (container is UIDialog dialog)
        {
            foreach (var child in dialog.GetChildren())
            {
                if (TryScrollWidgetIntoViewInContainer(child, target))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clears focus on any widget that is no longer visible or enabled.
    /// </summary>
    private void ValidateFocusedWidgets()
    {
        if (_focusedWidget != null && (!_focusedWidget.Visible || !_focusedWidget.Enabled))
            ClearWidgetFocus();

        if (_focusedTextInput != null && (!_focusedTextInput.Visible || !_focusedTextInput.Enabled))
        {
            _focusedTextInput.SetFocused(false, _input);
            _focusedTextInput = null;
        }

        if (_focusedTextArea != null && (!_focusedTextArea.Visible || !_focusedTextArea.Enabled))
        {
            _focusedTextArea.SetFocused(false, _input);
            _focusedTextArea = null;
        }
    }

    /// <summary>
    /// Processes mouse input for UI. Returns <c>true</c> if input was consumed.
    /// When <paramref name="consumed"/> is true, clears hover/press state.
    /// Non-dialog input is blocked while a modal dialog is active.
    /// </summary>
    public bool ProcessMouseInput(IInputContext input, bool consumed)
    {
        ValidateFocusedWidgets();

        if (_activeContextMenu != null)
        {
            var mousePos = input.MousePosition;
            _contextMenuHoveredIndex = _activeContextMenu.HitTestItem(mousePos);

            if (input.IsMouseButtonPressed(MouseButton.Left) || input.IsMouseButtonPressed(MouseButton.Right))
            {
                if (_contextMenuHoveredIndex >= 0 && _activeContextMenu.IsItemEnabled(_contextMenuHoveredIndex))
                {
                    int selected = _contextMenuHoveredIndex;
                    var menu = _activeContextMenu;
                    CloseContextMenu();
                    menu.FireItemSelected(selected);
                }
                else if (!_activeContextMenu.Contains(mousePos))
                {
                    CloseContextMenu();
                }
                return true;
            }

            return true;
        }

        var openMouseMb = _menuBars.Find(mb => mb.IsOpen);
        if (openMouseMb != null)
        {
            var mousePos = input.MousePosition;
            openMouseMb.UpdateHover(mousePos);

            if (input.IsMouseButtonPressed(MouseButton.Left))
            {
                openMouseMb.HandleClick(mousePos);
                return true;
            }

            return true;
        }

        if (consumed)
        {
            ClearInteractionState();
            return false;
        }

        RebuildInputBuffer();

        // Update menu bar hover / handle title-bar clicks even when no submenu is open.
        foreach (var mb in _menuBars)
        {
            if (!mb.Visible || !mb.Enabled) continue;
            var mp = input.MousePosition;
            mb.UpdateHover(mp);
            if (input.IsMouseButtonPressed(MouseButton.Left) && mb.Contains(mp))
            {
                _activeMenuBar = mb;
                mb.HandleClick(mp);
                return true;
            }
        }

        var activeDialog = FindActiveDialog();
        if (activeDialog != null)
        {
            ClearNonDialogInteractionState();
            _activeDialog = activeDialog;

            var mousePos = _input.MousePosition;

            if (_draggingDialog == activeDialog)
            {
                if (_input.IsMouseButtonDown(MouseButton.Left))
                {
                    activeDialog.Position = mousePos - _dialogDragOffset;
                }
                else
                {
                    _draggingDialog = null;
                    _dialogDragOffset = Vector2.Zero;
                }
            }
            else if (_input.IsMouseButtonPressed(MouseButton.Left) &&
                     activeDialog.IsOverCloseButton(mousePos))
            {
                activeDialog.SetCloseButtonHovered(false);
                activeDialog.EscapeDismiss();
            }
            else if (_input.IsMouseButtonPressed(MouseButton.Left) &&
                     activeDialog.IsDraggable &&
                     activeDialog.IsOverTitleBar(mousePos))
            {
                _draggingDialog = activeDialog;
                _dialogDragOffset = mousePos - activeDialog.Position;
            }
            else
            {
                _activeDialog.ProcessButtonInput(
                    mousePos,
                    _input.IsMouseButtonPressed(MouseButton.Left),
                    _input.IsMouseButtonReleased(MouseButton.Left));

                // Route input to interactive children embedded in the dialog.
                HandleDialogChildrenInput(_activeDialog);
            }

            activeDialog.SetCloseButtonHovered(activeDialog.IsOverCloseButton(mousePos));

            HandleTooltips(input);
            return true;
        }

        _activeDialog = null;
        _draggingDialog = null;
        _dialogDragOffset = Vector2.Zero;
        _dialogScrollbarDragView?.EndScrollbarDrag();
        _dialogScrollbarDragView = null;
        _activeDialogDropdown?.Close();
        _activeDialogDropdown = null;

        HandleButtonInput();
        HandleSliderInput();
        HandleSpinBoxInput();
        HandleVirtualListInput();
        HandleTreeViewInput();
        HandleDragInput();
        HandleTextInputMouse();
        HandleTextAreaMouse();
        HandleCheckboxInput();
        HandleDropdownInput();
        HandleRadioButtonInput();
        HandleTabContainerInput();
        HandleScrollViewInput();
        HandleLabelAndImageInput();
        HandleTooltips(input);

        bool isInteractingWithUI =
            _hoveredButton != null ||
            _pressedButton != null ||
            _activeSlider?.IsDragging == true ||
            _hoveredCheckbox != null ||
            _hoveredSpinBox != null ||
            _hoveredVirtualList != null ||
            _thumbDragVirtualList != null ||
            _activeDropdown != null ||
            _hoveredRadioButton != null ||
            _hoveredTabContainer != null ||
            _scrollbarDragView != null ||
            (_activeScrollView != null && Math.Abs(_input.ScrollWheelDelta) > 0.001f) ||
            _focusedTextInput?.IsFocused == true ||
            _focusedTextArea?.IsFocused == true ||
            IsMouseOverBlockingPanel();

        return isInteractingWithUI;
    }

    /// <summary>
    /// Rebuilds the flat input dispatch buffer from top-level components by recursively
    /// expanding containers (<see cref="UIPanel"/>, <see cref="UITabContainer"/>,
    /// <see cref="UIScrollView"/>) at any nesting depth. Each entry carries the coordinate
    /// offset required so that the component's own <see cref="IUIComponent.Contains"/> check
    /// works correctly against screen-space mouse positions.
    /// Called once per <see cref="ProcessMouseInput"/> frame before any handlers run.
    /// </summary>
    private void RebuildInputBuffer()
    {
        _inputDispatchBuffer.Clear();
        foreach (var component in InRenderOrder())
            ExpandComponentToBuffer(component, Vector2.Zero, hasClip: false, 0f, 0f, 0f, 0f);
    }

    private void ExpandComponentToBuffer(IUIComponent component, Vector2 parentAbsoluteOrigin,
        bool hasClip, float clipL, float clipT, float clipR, float clipB)
    {
        Vector2 componentScreenPos;
        Vector2 inputOffset;

        if (component is IAnchoredUIComponent anchored &&
            (anchored.Anchor != UIAnchor.TopLeft || anchored.AnchorOffset != Vector2.Zero))
        {
            componentScreenPos = UIAnchorResolver.ResolveAnchoredPosition(anchored, _screenSize);
            inputOffset = UIAnchorResolver.ComputeInputOffsetForAnchored(component, _screenSize);
        }
        else
        {
            componentScreenPos = parentAbsoluteOrigin + component.Position;
            inputOffset = -parentAbsoluteOrigin;
        }

        _inputDispatchBuffer.Add(new InputDispatchEntry(component, inputOffset, hasClip, clipL, clipT, clipR, clipB));

        if (!component.Enabled || !component.Visible) return;

        switch (component)
        {
            case UIPanel panel:
            {
                bool panelClips = panel.ClipChildren;
                bool childHasClip = hasClip || panelClips;
                float childClipL = panelClips ? Math.Max(clipL, componentScreenPos.X) : clipL;
                float childClipT = panelClips ? Math.Max(clipT, componentScreenPos.Y) : clipT;
                float childClipR = panelClips ? Math.Min(hasClip ? clipR : float.MaxValue, componentScreenPos.X + panel.Size.X) : clipR;
                float childClipB = panelClips ? Math.Min(hasClip ? clipB : float.MaxValue, componentScreenPos.Y + panel.Size.Y) : clipB;
                foreach (var child in panel.GetChildren())
                    ExpandComponentToBuffer(child, componentScreenPos, childHasClip, childClipL, childClipT, childClipR, childClipB);
                break;
            }

            case UIStackPanel sp:
            {
                bool spClips = sp.ClipChildren;
                bool childHasClip = hasClip || spClips;
                float childClipL = spClips ? Math.Max(clipL, componentScreenPos.X) : clipL;
                float childClipT = spClips ? Math.Max(clipT, componentScreenPos.Y) : clipT;
                float childClipR = spClips ? Math.Min(hasClip ? clipR : float.MaxValue, componentScreenPos.X + sp.Size.X) : clipR;
                float childClipB = spClips ? Math.Min(hasClip ? clipB : float.MaxValue, componentScreenPos.Y + sp.Size.Y) : clipB;
                foreach (var child in sp.GetChildren())
                    ExpandComponentToBuffer(child, componentScreenPos, childHasClip, childClipL, childClipT, childClipR, childClipB);
                break;
            }

            case UIGrid grid:
            {
                bool gridClips = grid.ClipChildren;
                bool childHasClip = hasClip || gridClips;
                float childClipL = gridClips ? Math.Max(clipL, componentScreenPos.X) : clipL;
                float childClipT = gridClips ? Math.Max(clipT, componentScreenPos.Y) : clipT;
                float childClipR = gridClips ? Math.Min(hasClip ? clipR : float.MaxValue, componentScreenPos.X + grid.Size.X) : clipR;
                float childClipB = gridClips ? Math.Min(hasClip ? clipB : float.MaxValue, componentScreenPos.Y + grid.Size.Y) : clipB;
                foreach (var child in grid.GetChildren())
                    ExpandComponentToBuffer(child, componentScreenPos, childHasClip, childClipL, childClipT, childClipR, childClipB);
                break;
            }

            case UITabContainer tab:
                // Tab children are content-origin-relative: (0,0) = top-left of content area (below tab bar).
                var contentOrigin = componentScreenPos + new Vector2(0f, tab.TabHeight);
                foreach (var child in tab.GetTabComponents(tab.SelectedTabIndex))
                    ExpandComponentToBuffer(child, contentOrigin, hasClip, clipL, clipT, clipR, clipB);
                break;

            case UIScrollView sv:
            {
                var svClipL = componentScreenPos.X;
                var svClipT = componentScreenPos.Y;
                var svClipR = componentScreenPos.X + sv.Size.X;
                var svClipB = componentScreenPos.Y + sv.Size.Y;
                var svContentOrigin = componentScreenPos - sv.ScrollOffset;
                foreach (var child in sv.GetChildren())
                    ExpandComponentToBuffer(child, svContentOrigin, true, svClipL, svClipT, svClipR, svClipB);
                break;
            }

            case UIDialog dialog:
                foreach (var btn in dialog.GetButtons())
                    ExpandComponentToBuffer(btn, componentScreenPos, hasClip, clipL, clipT, clipR, clipB);
                foreach (var child in dialog.GetChildren())
                    ExpandComponentToBuffer(child, componentScreenPos, hasClip, clipL, clipT, clipR, clipB);
                break;
        }
    }

    private void RenderToasts(IRenderer renderer)
    {
        if (_toasts.Count == 0) return;

        bool isBottom = ToastAnchor is ToastAnchor.BottomLeft or ToastAnchor.BottomRight;
        bool isRight  = ToastAnchor is ToastAnchor.TopRight  or ToastAnchor.BottomRight;

        float stackY = isBottom ? _screenSize.Y - ToastPadding : ToastPadding;

        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            var toast = _toasts[i];
            if (toast.Alpha <= 0f) continue;

            float x = isRight
                ? _screenSize.X - toast.Width - ToastPadding
                : ToastPadding;

            // Let Render() measure and set Height. On the very first frame Height is 0
            // so we draw at a placeholder position; the correct position is used from
            // the second frame onward once Height has been measured by the renderer.
            float y = isBottom ? stackY - Math.Max(toast.Height, 1f) : stackY;
            toast.Render(renderer, new Vector2(x, y));

            // Now Height is accurate (set inside Render); use it for the next toast's slot.
            if (isBottom)
                stackY -= toast.Height + ToastPadding;
            else
                stackY += toast.Height + ToastPadding;
        }
    }

    private UIDialog? FindActiveDialog()
    {
        var ordered = InRenderOrder().ToList();
        for (int i = ordered.Count - 1; i >= 0; i--)
        {
            if (ordered[i] is UIDialog dialog && dialog.Enabled && dialog.Visible)
                return dialog;
        }

        return null;
    }

    private bool IsMouseOverBlockingPanel()
    {
        var mousePos = _input.MousePosition;
        foreach (var entry in _inputDispatchBuffer)
        {
            if (entry.Component is UIPanel panel &&
                panel.BlocksInput && panel.Enabled && panel.Visible &&
                entry.IsHit(mousePos))
                return true;

            if (entry.Component is UIStackPanel sp &&
                sp.BlocksInput && sp.Enabled && sp.Visible &&
                entry.IsHit(mousePos))
                return true;

            if (entry.Component is UIGrid grid &&
                grid.BlocksInput && grid.Enabled && grid.Visible &&
                entry.IsHit(mousePos))
                return true;
        }
        return false;
    }

    private void CloseActiveDropdown()
    {
        if (_activeDropdown != null)
        {
            _activeDropdown.SuppressListRender = false;
            _activeDropdown.Close();
        }
        _activeDropdown = null;
        _activeDropdownInputOffset = Vector2.Zero;
        _activeDropdownNeedsOverlay = false;
    }

    private void ClearInteractionState()
    {
        if (_hoveredButton != null)
        {
            _hoveredButton.SetHovered(false);
            _hoveredButton = null;
        }

        if (_pressedButton != null)
        {
            _pressedButton.SetPressed(false);
            _pressedButton = null;
        }

        _activeSlider = null;
        _activeSliderInputOffset = Vector2.Zero;
        _hoveredCheckbox = null;
        _hoveredSpinBox = null;
        _hoveredVirtualList?.ClearHover();
        _hoveredVirtualList = null;
        _hoveredVirtualListInputOffset = Vector2.Zero;
        _thumbDragVirtualList?.EndThumbDrag();
        _thumbDragVirtualList = null;
        _thumbDragVirtualListInputOffset = Vector2.Zero;
        CloseActiveDropdown();
        _hoveredRadioButton = null;
        _hoveredTabContainer = null;
        _hoveredTabContainerInputOffset = Vector2.Zero;
        _focusedTabContainer?.SetFocused(false);
        _focusedTabContainer = null;
        _activeScrollView = null;
        _scrollbarDragView?.EndScrollbarDrag();
        _scrollbarDragView = null;
        _dialogScrollbarDragView?.EndScrollbarDrag();
        _dialogScrollbarDragView = null;
        _activeDialog = null;
        _draggingDialog = null;
        _dialogDragOffset = Vector2.Zero;
        _activeDialogDropdown?.Close();
        _activeDialogDropdown = null;
    }

    private void ClearNonDialogInteractionState()
    {
        if (_hoveredButton != null)
        {
            _hoveredButton.SetHovered(false);
            _hoveredButton = null;
        }

        if (_pressedButton != null)
        {
            _pressedButton.SetPressed(false);
            _pressedButton = null;
        }

        _activeSlider = null;
        _activeSliderInputOffset = Vector2.Zero;
        _hoveredCheckbox = null;
        _hoveredSpinBox = null;
        _hoveredVirtualList?.ClearHover();
        _hoveredVirtualList = null;
        _hoveredVirtualListInputOffset = Vector2.Zero;
        _thumbDragVirtualList?.EndThumbDrag();
        _thumbDragVirtualList = null;
        _thumbDragVirtualListInputOffset = Vector2.Zero;
        CloseActiveDropdown();
        _hoveredRadioButton = null;
        _hoveredTabContainer = null;
        _hoveredTabContainerInputOffset = Vector2.Zero;
        _focusedTabContainer?.SetFocused(false);
        _focusedTabContainer = null;
        _activeScrollView = null;
        _scrollbarDragView?.EndScrollbarDrag();
        _scrollbarDragView = null;
    }

    private void HandleButtonInput()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_hoveredButton != null)
        {
            _hoveredButton.SetHovered(false);
            _hoveredButton = null;
        }

        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (entry.Component is UIButton button && button.Enabled && button.Visible)
            {
                if (entry.IsHit(mouseScreenPos))
                {
                    button.SetHovered(true);
                    _hoveredButton = button;
                    break;
                }
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_hoveredButton != null)
            {
                _hoveredButton.SetPressed(true);
                _pressedButton = _hoveredButton;
            }
        }

        if (_input.IsMouseButtonReleased(MouseButton.Left))
        {
            if (_pressedButton != null)
            {
                _pressedButton.SetPressed(false);

                if (_hoveredButton == _pressedButton)
                    _pressedButton.Click();

                _pressedButton = null;
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Right) && _hoveredButton != null)
            _hoveredButton.RightClick();
    }

    private void HandleSliderInput()
    {
        var mouseScreenPos = _input.MousePosition;

        foreach (var entry in _inputDispatchBuffer)
        {
            if (entry.Component is UISlider slider && slider.Enabled && slider.Visible)
                slider.SetHovered(entry.IsHit(mouseScreenPos));
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left) && _activeSlider == null)
        {
            for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
            {
                var entry = _inputDispatchBuffer[i];
                if (entry.Component is UISlider slider && slider.Enabled && slider.Visible &&
                    entry.IsHit(mouseScreenPos))
                {
                    slider.StartDrag();
                    slider.UpdateDrag(mouseScreenPos + entry.InputOffset);
                    _activeSlider = slider;
                    _activeSliderInputOffset = entry.InputOffset;
                    break;
                }
            }
        }

        if (_activeSlider != null && _input.IsMouseButtonDown(MouseButton.Left))
            _activeSlider.UpdateDrag(mouseScreenPos + _activeSliderInputOffset);

        if (_input.IsMouseButtonReleased(MouseButton.Left) && _activeSlider != null)
        {
            _activeSlider.EndDrag();
            _activeSlider = null;
            _activeSliderInputOffset = Vector2.Zero;
        }
    }

    private void HandleTextInputMouse()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            UITextInput? clickedInput = null;
            float clickedX = mouseScreenPos.X;

            for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
            {
                var entry = _inputDispatchBuffer[i];
                if (entry.Component is UITextInput textInput && textInput.Enabled && textInput.Visible)
                {
                    if (entry.IsHit(mouseScreenPos))
                    {
                        clickedInput = textInput;
                        clickedX = mouseScreenPos.X;
                        break;
                    }
                }
            }

            if (clickedInput != null && _focusedTextArea != null)
            {
                _focusedTextArea.SetFocused(false, _input);
                _focusedTextArea = null;
            }

            if (clickedInput != null)
                ClearWidgetFocus();

            if (_focusedTextInput != null)
                _focusedTextInput.SetFocused(false, _input);

            _focusedTextInput = clickedInput;

            if (_focusedTextInput != null)
            {
                _focusedTextInput.SetFocused(true, _input, clickedX);
                _focusedTextInput.StartMouseDrag(clickedX);
                _focusedWidget = _focusedTextInput;
            }
        }
        else if (_input.IsMouseButtonDown(MouseButton.Left) && _focusedTextInput != null)
        {
            _focusedTextInput.UpdateMouseDrag(mouseScreenPos.X);
        }
        else if (_input.IsMouseButtonReleased(MouseButton.Left) && _focusedTextInput != null)
        {
            _focusedTextInput.EndMouseDrag();
        }
    }

    private void HandleTextAreaMouse()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            UITextArea? clickedArea = null;

            for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
            {
                var entry = _inputDispatchBuffer[i];
                if (entry.Component is UITextArea textArea && textArea.Enabled && textArea.Visible)
                {
                    if (entry.IsHit(mouseScreenPos))
                    {
                        clickedArea = textArea;
                        break;
                    }
                }
            }

            if (clickedArea != null && _focusedTextInput != null)
            {
                _focusedTextInput.SetFocused(false, _input);
                _focusedTextInput = null;
            }

            if (clickedArea != null)
                ClearWidgetFocus();

            if (_focusedTextArea != null)
                _focusedTextArea.SetFocused(false, _input);

            _focusedTextArea = clickedArea;

            if (_focusedTextArea != null)
            {
                _focusedTextArea.SetFocused(true, _input, mouseScreenPos.X, mouseScreenPos.Y);
                _focusedTextArea.StartMouseDrag(mouseScreenPos.X, mouseScreenPos.Y);
                _focusedWidget = _focusedTextArea;
            }
        }
        else if (_input.IsMouseButtonDown(MouseButton.Left) && _focusedTextArea != null)
        {
            _focusedTextArea.UpdateMouseDrag(mouseScreenPos.X, mouseScreenPos.Y);
        }
        else if (_input.IsMouseButtonReleased(MouseButton.Left) && _focusedTextArea != null)
        {
            _focusedTextArea.EndMouseDrag();
        }

        if (_focusedTextArea?.IsFocused == true && Math.Abs(_input.ScrollWheelDelta) > 0.001f)
        {
            if (_focusedTextArea.Contains(mouseScreenPos))
                _focusedTextArea.HandleScroll(_input.ScrollWheelDelta);
        }
    }

    private void HandleTextInputKeyboard()
    {
        if (_focusedTextInput == null || !_focusedTextInput.IsFocused)
            return;

        _focusedTextInput.HandleTextInput(_input);
    }

    private void HandleTextAreaKeyboard()
    {
        if (_focusedTextArea == null || !_focusedTextArea.IsFocused)
            return;

        _focusedTextArea.HandleTextInput(_input);
    }

    private void HandleSpinBoxInput()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_hoveredSpinBox != null)
        {
            _hoveredSpinBox.SetHoveredIncrement(false);
            _hoveredSpinBox.SetHoveredDecrement(false);
            _hoveredSpinBox = null;
        }

        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (entry.Component is UISpinBox spinBox && spinBox.Enabled && spinBox.Visible)
            {
                if (entry.IsHit(mouseScreenPos))
                {
                    var localPos = mouseScreenPos + entry.InputOffset;
                    spinBox.SetHoveredIncrement(spinBox.ContainsIncrement(localPos));
                    spinBox.SetHoveredDecrement(spinBox.ContainsDecrement(localPos));
                    _hoveredSpinBox = spinBox;
                    break;
                }
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_hoveredSpinBox != null)
            {
                var localPos = mouseScreenPos;
                foreach (var entry in _inputDispatchBuffer)
                {
                    if (entry.Component == _hoveredSpinBox)
                    {
                        localPos = mouseScreenPos + entry.InputOffset;
                        break;
                    }
                }

                if (_hoveredSpinBox.ContainsIncrement(localPos))
                {
                    _hoveredSpinBox.SetPressedIncrement(true);
                    _hoveredSpinBox.Increment();
                }
                else if (_hoveredSpinBox.ContainsDecrement(localPos))
                {
                    _hoveredSpinBox.SetPressedDecrement(true);
                    _hoveredSpinBox.Decrement();
                }
                else if (_hoveredSpinBox.ContainsField(localPos))
                {
                    _hoveredSpinBox.BeginEdit();
                }

                ClearWidgetFocus();
                _focusedWidget = _hoveredSpinBox;
                ApplyWidgetFocus(_hoveredSpinBox, true, _input);
                ScrollFocusedWidgetIntoView(_hoveredSpinBox);
            }
            else
            {
                // Click outside any spinbox — lose focus/edit on any focused spinbox.
                if (_focusedWidget is UISpinBox focused)
                {
                    focused.CommitEdit();
                    ClearWidgetFocus();
                }
            }
        }

        if (_input.IsMouseButtonReleased(MouseButton.Left))
        {
            foreach (var entry in _inputDispatchBuffer)
            {
                if (entry.Component is UISpinBox sb)
                {
                    sb.SetPressedIncrement(false);
                    sb.SetPressedDecrement(false);
                }
            }
        }
    }

    private void HandleVirtualListInput()
    {
        var mousePos = _input.MousePosition;
        bool pressed = _input.IsMouseButtonPressed(MouseButton.Left);
        bool released = _input.IsMouseButtonReleased(MouseButton.Left);
        float scroll = _input.ScrollWheelDelta;

        // Scrollbar thumb drag in progress — feed updates even when mouse leaves the list.
        if (_thumbDragVirtualList != null)
        {
            if (_input.IsMouseButtonDown(MouseButton.Left))
            {
                _thumbDragVirtualList.UpdateThumbDrag(mousePos.Y + _thumbDragVirtualListInputOffset.Y);
            }
            else if (released)
            {
                _thumbDragVirtualList.EndThumbDrag();
                _thumbDragVirtualList = null;
                _thumbDragVirtualListInputOffset = Vector2.Zero;
            }
            return;
        }

        // Clear previous hover.
        if (_hoveredVirtualList != null)
        {
            _hoveredVirtualList.ClearHover();
            _hoveredVirtualList = null;
            _hoveredVirtualListInputOffset = Vector2.Zero;
        }

        // Find the topmost virtual list under the cursor.
        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (entry.Component is UIVirtualListBase vl && vl.Enabled && vl.Visible && entry.IsHit(mousePos))
            {
                _hoveredVirtualList = vl;
                _hoveredVirtualListInputOffset = entry.InputOffset;
                break;
            }
        }

        if (_hoveredVirtualList != null)
        {
            var localPos = mousePos + _hoveredVirtualListInputOffset;
            _hoveredVirtualList.UpdateHover(localPos);

            if (pressed)
            {
                if (_hoveredVirtualList.IsOverScrollbarTrack(localPos))
                {
                    if (_hoveredVirtualList.IsOverScrollbarThumb(localPos))
                        _hoveredVirtualList.StartThumbDrag(localPos.Y);
                    else
                        _hoveredVirtualList.JumpScrollTo(localPos.Y);

                    _thumbDragVirtualList = _hoveredVirtualList;
                    _thumbDragVirtualListInputOffset = _hoveredVirtualListInputOffset;
                }
                else
                {
                    _hoveredVirtualList.HandleClick(localPos);
                }

                if (_focusedWidget != _hoveredVirtualList)
                {
                    ClearWidgetFocus();
                    _focusedWidget = _hoveredVirtualList;
                    _hoveredVirtualList.SetFocused(true);
                }
            }
            else if (Math.Abs(scroll) > 0.001f)
            {
                _hoveredVirtualList.HandleScroll(-scroll);
            }
        }
        else if (pressed && _focusedWidget is UIVirtualListBase focused)
        {
            focused.SetFocused(false);
            ClearWidgetFocus();
        }
    }

    private void HandleTreeViewInput()
    {
        var mousePos = _input.MousePosition;
        bool pressed = _input.IsMouseButtonPressed(MouseButton.Left);
        bool released = _input.IsMouseButtonReleased(MouseButton.Left);
        float scroll = _input.ScrollWheelDelta;

        if (_thumbDragTreeView != null)
        {
            if (_input.IsMouseButtonDown(MouseButton.Left))
                _thumbDragTreeView.UpdateThumbDrag(mousePos.Y + _thumbDragTreeViewInputOffset.Y);
            else if (released)
            {
                _thumbDragTreeView.EndThumbDrag();
                _thumbDragTreeView = null;
                _thumbDragTreeViewInputOffset = Vector2.Zero;
            }
            return;
        }

        if (_hoveredTreeView != null)
        {
            _hoveredTreeView.ClearHover();
            _hoveredTreeView = null;
            _hoveredTreeViewInputOffset = Vector2.Zero;
        }

        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (entry.Component is UITreeView tv && tv.Enabled && tv.Visible && entry.IsHit(mousePos))
            {
                _hoveredTreeView = tv;
                _hoveredTreeViewInputOffset = entry.InputOffset;
                break;
            }
        }

        if (_hoveredTreeView != null)
        {
            var localPos = mousePos + _hoveredTreeViewInputOffset;
            _hoveredTreeView.UpdateHover(localPos);

            if (pressed)
            {
                if (_hoveredTreeView.IsOverScrollbarTrack(localPos))
                {
                    if (_hoveredTreeView.IsOverScrollbarThumb(localPos))
                        _hoveredTreeView.StartThumbDrag(localPos.Y);
                    else
                        _hoveredTreeView.JumpScrollTo(localPos.Y);
                    _thumbDragTreeView = _hoveredTreeView;
                    _thumbDragTreeViewInputOffset = _hoveredTreeViewInputOffset;
                }
                else
                {
                    _hoveredTreeView.HandleClick(localPos);
                }

                if (_focusedWidget != _hoveredTreeView)
                {
                    ClearWidgetFocus();
                    _focusedWidget = _hoveredTreeView;
                    _hoveredTreeView.SetFocused(true);
                }
            }
            else if (Math.Abs(scroll) > 0.001f)
            {
                _hoveredTreeView.HandleScroll(-scroll);
            }
        }
        else if (pressed && _focusedWidget is UITreeView focusedTv)
        {
            focusedTv.SetFocused(false);
            ClearWidgetFocus();
        }
    }

    private void HandleDragInput()
    {
        var mousePos = _input.MousePosition;

        if (_dragActive)
        {
            // Update drop-target hover highlights.
            if (_hoveredDropTarget != null)
            {
                _hoveredDropTarget.SetHovered(false);
                _hoveredDropTarget = null;
            }

            foreach (var entry in _inputDispatchBuffer)
            {
                if (entry.Component is not UIDropTarget target) continue;
                if (!target.Enabled || !target.Visible) continue;
                bool compatible = target.AcceptsPayload == null || target.AcceptsPayload(_dragPayload!);
                if (compatible && entry.IsHit(mousePos))
                {
                    target.SetHovered(true);
                    _hoveredDropTarget = target;
                    break;
                }
            }

            if (_input.IsMouseButtonReleased(MouseButton.Left))
            {
                if (_hoveredDropTarget != null)
                {
                    _hoveredDropTarget.SetHovered(false);
                    _hoveredDropTarget.FireDrop(_dragPayload!);
                    _hoveredDropTarget = null;
                }
                else
                {
                    OnDragCancelled?.Invoke(_dragSource!, _dragPayload!);
                }

                _dragActive = false;
                _dragSource = null;
                _dragPayload = null;
            }

            return;
        }

        // Track press start for threshold detection.
        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            // Look for a draggable component under the cursor.
            for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
            {
                var entry = _inputDispatchBuffer[i];
                if (!entry.Component.Enabled || !entry.Component.Visible) continue;
                if (!_draggableRegistry.TryGetValue(entry.Component, out var payload)) continue;
                if (!entry.IsHit(mousePos)) continue;

                _dragSource = entry.Component;
                _dragPayload = payload;
                _dragStartMousePos = mousePos;
                _dragGhostOffset = entry.Component.Position - mousePos;
                break;
            }
        }

        if (_dragSource != null && _input.IsMouseButtonDown(MouseButton.Left) && !_dragActive)
        {
            var delta = mousePos - _dragStartMousePos;
            if (delta.Length() >= DragThreshold)
            {
                _dragActive = true;
                OnDragStarted?.Invoke(_dragSource, _dragPayload!);
            }
        }

        if (_input.IsMouseButtonReleased(MouseButton.Left) && _dragSource != null && !_dragActive)
        {
            // Released before threshold — treat as a normal click, not a drag.
            _dragSource = null;
            _dragPayload = null;
        }
    }

    private void CancelDrag()
    {
        if (_dragActive && _dragSource != null && _dragPayload != null)
            OnDragCancelled?.Invoke(_dragSource, _dragPayload);

        _dragActive = false;
        _dragSource = null;
        _dragPayload = null;
        _dragStartMousePos = Vector2.Zero;
        _dragGhostOffset = Vector2.Zero;
        if (_hoveredDropTarget != null)
        {
            _hoveredDropTarget.SetHovered(false);
            _hoveredDropTarget = null;
        }
    }

    private void RenderDragGhost(IRenderer renderer)
    {
        if (_dragSource == null) return;

        var mousePos = _input.MousePosition;
        var ghostPos = mousePos + _dragGhostOffset;
        var size = _dragSource.Size;

        renderer.DrawRectangleFilled(ghostPos.X, ghostPos.Y, size.X, size.Y,
            new Color(180, 180, 255, 120));
        renderer.DrawRectangleOutline(ghostPos.X, ghostPos.Y, size.X, size.Y,
            new Color(120, 180, 255, 200), 2f);
    }

    private void RenderWorldComponents(IRenderer renderer)
    {
        if (_worldComponents.Count == 0) return;

        foreach (var wc in _worldComponents)
        {
            if (!wc.Visible) continue;

            if (WorldCamera != null)
            {
                var screen = WorldCamera.WorldToScreen(wc.WorldPosition) + wc.ScreenOffset;

                if (wc.CullWhenOffScreen)
                {
                    if (screen.X < 0 || screen.Y < 0 ||
                        screen.X > _screenSize.X || screen.Y > _screenSize.Y)
                        continue;
                }

                wc.Position = screen;
            }

            wc.Render(renderer);
        }
    }

    private void HandleCheckboxInput()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_hoveredCheckbox != null)
        {
            _hoveredCheckbox.SetHovered(false);
            _hoveredCheckbox = null;
        }

        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (entry.Component is UICheckbox checkbox && checkbox.Enabled && checkbox.Visible)
            {
                if (entry.IsHit(mouseScreenPos))
                {
                    checkbox.SetHovered(true);
                    _hoveredCheckbox = checkbox;
                    break;
                }
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_hoveredCheckbox != null)
                _hoveredCheckbox.Toggle();
        }
    }

    private void HandleDropdownInput()
    {
        var mouseScreenPos = _input.MousePosition;
        var scrollDelta = _input.ScrollWheelDelta;

        // For overlay dropdowns (inside a scroll view), hover and scroll use the resolved screen position.
        if (_activeDropdown != null && _activeDropdown.IsExpanded)
        {
            var hoverPos = _activeDropdownNeedsOverlay
                ? mouseScreenPos
                : mouseScreenPos + _activeDropdownInputOffset;
            _activeDropdown.UpdateHover(hoverPos);
        }

        if (_activeDropdown != null && _activeDropdown.IsExpanded && Math.Abs(scrollDelta) > 0.001f)
        {
            var containsPos = _activeDropdownNeedsOverlay
                ? mouseScreenPos
                : mouseScreenPos + _activeDropdownInputOffset;
            if (_activeDropdown.Contains(containsPos))
            {
                _activeDropdown.Scroll(scrollDelta);
                return;
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            UIDropdown? clickedDropdown = null;
            Vector2 clickedDropdownOffset = Vector2.Zero;
            bool clickedNeedsOverlay = false;

            // Check if we clicked the expanded list of an overlay dropdown first.
            if (_activeDropdown != null && _activeDropdown.IsExpanded && _activeDropdownNeedsOverlay)
            {
                if (_activeDropdown.IsOverExpandedList(mouseScreenPos))
                {
                    _activeDropdown.SelectItem(mouseScreenPos);
                    _activeDropdown.SuppressListRender = false;
                    _activeDropdown = null;
                    _activeDropdownInputOffset = Vector2.Zero;
                    _activeDropdownNeedsOverlay = false;
                    return;
                }
            }

            for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
            {
                var entry = _inputDispatchBuffer[i];
                if (entry.Component is UIDropdown dropdown && dropdown.Enabled && dropdown.Visible)
                {
                    if (entry.IsHit(mouseScreenPos))
                    {
                        clickedDropdown = dropdown;
                        clickedDropdownOffset = entry.InputOffset;
                        clickedNeedsOverlay = entry.HasClip;
                        break;
                    }
                }
            }

            var adjustedPos = mouseScreenPos + clickedDropdownOffset;

            if (clickedDropdown != null)
            {
                if (clickedDropdown.IsExpanded &&
                    clickedDropdown.IsOverExpandedList(adjustedPos))
                {
                    clickedDropdown.SelectItem(adjustedPos);
                }
                else
                {
                    if (_activeDropdown != null && _activeDropdown != clickedDropdown)
                    {
                        _activeDropdown.SuppressListRender = false;
                        _activeDropdown.Close();
                    }

                    clickedDropdown.Toggle();
                    if (clickedDropdown.IsExpanded)
                    {
                        if (!clickedNeedsOverlay)
                            BringToFront(clickedDropdown);
                        _activeDropdown = clickedDropdown;
                        _activeDropdownInputOffset = clickedDropdownOffset;
                        _activeDropdownNeedsOverlay = clickedNeedsOverlay;
                        clickedDropdown.SuppressListRender = clickedNeedsOverlay;
                    }
                    else
                    {
                        clickedDropdown.SuppressListRender = false;
                        _activeDropdown = null;
                        _activeDropdownInputOffset = Vector2.Zero;
                        _activeDropdownNeedsOverlay = false;
                    }
                }
            }
            else
            {
                if (_activeDropdown != null)
                {
                    _activeDropdown.SuppressListRender = false;
                    _activeDropdown.Close();
                    _activeDropdown = null;
                    _activeDropdownInputOffset = Vector2.Zero;
                    _activeDropdownNeedsOverlay = false;
                }
            }
        }
    }

    private void HandleRadioButtonInput()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_hoveredRadioButton != null)
        {
            _hoveredRadioButton.SetHovered(false);
            _hoveredRadioButton = null;
        }

        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (entry.Component is UIRadioButton radioButton && radioButton.Enabled && radioButton.Visible)
            {
                if (entry.IsHit(mouseScreenPos))
                {
                    radioButton.SetHovered(true);
                    _hoveredRadioButton = radioButton;
                    break;
                }
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_hoveredRadioButton != null)
                _hoveredRadioButton.Select();
        }
    }

    private void HandleTabContainerInput()
    {
        var mouseScreenPos = _input.MousePosition;
        _hoveredTabContainer = null;

        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (entry.Component is UITabContainer tabContainer && tabContainer.Enabled && tabContainer.Visible)
            {
                if (entry.IsHit(mouseScreenPos))
                {
                    tabContainer.UpdateHover(mouseScreenPos + entry.InputOffset);
                    _hoveredTabContainer = tabContainer;
                    _hoveredTabContainerInputOffset = entry.InputOffset;
                    break;
                }
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_hoveredTabContainer != null)
            {
                var adjustedPos = mouseScreenPos + _hoveredTabContainerInputOffset;
                _hoveredTabContainer.SelectTab(adjustedPos);

                // Grant tab-switch focus only when the click is on the tab bar itself.
                bool clickedTabBar = adjustedPos.Y >= _hoveredTabContainer.Position.Y &&
                                     adjustedPos.Y <= _hoveredTabContainer.Position.Y + _hoveredTabContainer.TabHeight;
                if (clickedTabBar)
                {
                    if (_focusedTabContainer != null && _focusedTabContainer != _hoveredTabContainer)
                        _focusedTabContainer.SetFocused(false);
                    _focusedTabContainer = _hoveredTabContainer;
                    _focusedTabContainer.SetFocused(true);
                }
            }
            else if (_focusedTabContainer != null)
            {
                _focusedTabContainer.SetFocused(false);
                _focusedTabContainer = null;
            }
        }
    }

    private void HandleScrollViewInput()
    {
        var mouseScreenPos = _input.MousePosition;
        var scrollDelta = _input.ScrollWheelDelta;

        if (_scrollbarDragView != null)
        {
            if (_input.IsMouseButtonDown(MouseButton.Left))
            {
                _scrollbarDragView.UpdateScrollbarDrag(mouseScreenPos);
            }
            else if (_input.IsMouseButtonReleased(MouseButton.Left))
            {
                _scrollbarDragView.EndScrollbarDrag();
                _scrollbarDragView = null;
            }

            return;
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            foreach (var entry in _inputDispatchBuffer)
            {
                if (entry.Component is not UIScrollView sv || !sv.Enabled || !sv.Visible) continue;

                var adjustedPos = mouseScreenPos + entry.InputOffset;

                if (sv.IsOverVerticalScrollbarTrack(adjustedPos))
                {
                    if (!sv.IsOverVerticalScrollbar(adjustedPos))
                        sv.JumpScrollToVertical(mouseScreenPos.Y);
                    sv.StartScrollbarDrag(mouseScreenPos, isVertical: true);
                    _scrollbarDragView = sv;
                    _scrollbarDragIsVertical = true;
                    return;
                }

                if (sv.IsOverHorizontalScrollbarTrack(adjustedPos))
                {
                    if (!sv.IsOverHorizontalScrollbar(adjustedPos))
                        sv.JumpScrollToHorizontal(mouseScreenPos.X);
                    sv.StartScrollbarDrag(mouseScreenPos, isVertical: false);
                    _scrollbarDragView = sv;
                    _scrollbarDragIsVertical = false;
                    return;
                }
            }
        }

        _activeScrollView = null;
        foreach (var entry in _inputDispatchBuffer)
        {
            if (entry.Component is UIScrollView scrollView && scrollView.Enabled && scrollView.Visible)
            {
                scrollView.UpdateScrollbarHover(mouseScreenPos + entry.InputOffset);
                if (entry.IsHit(mouseScreenPos))
                {
                    _activeScrollView = scrollView;
                }
            }
        }

        if (_activeScrollView != null && Math.Abs(scrollDelta) > 0.001f)
            _activeScrollView.HandleScroll(scrollDelta);
    }

    private void HandleLabelAndImageInput()
    {
        if (!_input.IsMouseButtonReleased(MouseButton.Left)) return;

        var mousePos = _input.MousePosition;
        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (!entry.Component.Enabled || !entry.Component.Visible) continue;

            if (entry.Component is UILabel lbl && lbl.HasOnClick && entry.IsHit(mousePos))
            { lbl.Click(); break; }

            if (entry.Component is UIRichTextLabel rtl && rtl.HasOnClick && entry.IsHit(mousePos))
            { rtl.Click(); break; }

            if (entry.Component is UIImage img && img.HasOnClick && entry.IsHit(mousePos))
            { img.Click(); break; }
        }
    }

    private void HandleTooltips(IInputContext input)
    {
        var mousePos = input.MousePosition;
        IUIComponent? hoveredComponent = null;

        var activeDialog = FindActiveDialog();

        for (int i = _inputDispatchBuffer.Count - 1; i >= 0; i--)
        {
            var entry = _inputDispatchBuffer[i];
            if (!entry.Component.Enabled || !entry.Component.Visible || !entry.IsHit(mousePos))
                continue;

            // When a modal dialog is active, only allow tooltips on the dialog itself
            // or components that are direct children of that dialog.
            if (activeDialog != null &&
                entry.Component != activeDialog &&
                !ContainsDescendant(activeDialog, entry.Component))
                continue;

            hoveredComponent = entry.Component;
            break;
        }

        if (hoveredComponent != null && hoveredComponent.Tooltip != null)
        {
            if (_tooltipOwner != hoveredComponent)
            {
                _activeTooltip?.OnHoverEnd();
                _activeTooltip = hoveredComponent.Tooltip;
                _tooltipOwner = hoveredComponent;
                _activeTooltip.OnHoverStart(mousePos);
                _activeTooltip.UpdatePosition(mousePos, _screenSize);
            }
            else
            {
                _activeTooltip?.UpdatePosition(mousePos, _screenSize);
            }
        }
        else
        {
            if (_activeTooltip != null)
            {
                _activeTooltip.OnHoverEnd();
                _activeTooltip = null;
                _tooltipOwner = null;
            }
        }
    }

    private void HandleDialogChildrenInput(UIDialog dialog)
    {
        var mousePos = _input.MousePosition;
        bool pressed = _input.IsMouseButtonPressed(MouseButton.Left);
        bool released = _input.IsMouseButtonReleased(MouseButton.Left);

        // Children are stored in dialog-relative coordinates, so translate mouse into that space.
        var dialogRelativePos = mousePos - dialog.Position;

        // Update hover state on any open dialog dropdown and handle its list clicks.
        if (_activeDialogDropdown != null && _activeDialogDropdown.IsExpanded)
        {
            _activeDialogDropdown.UpdateHover(dialogRelativePos);

            if (pressed)
            {
                if (_activeDialogDropdown.IsOverExpandedList(dialogRelativePos))
                {
                    _activeDialogDropdown.SelectItem(dialogRelativePos);
                    return;
                }

                // Clicked outside the expanded list — close it.
                _activeDialogDropdown.Close();
                _activeDialogDropdown = null;
            }
        }

        var dropdownBefore = _activeDialogDropdown;

        DispatchChildrenInput(dialog.GetChildren(), dialogRelativePos, pressed, released, ref _dialogPressedButton);

        // If a new dropdown opened inside the dialog, bring it to front so its list
        // renders above sibling widgets regardless of add order.
        if (_activeDialogDropdown != null && _activeDialogDropdown != dropdownBefore)
            dialog.BringChildToFront(_activeDialogDropdown);
    }

    private void DispatchChildrenInput(IReadOnlyList<IUIComponent> children, Vector2 mousePos, bool pressed, bool released, ref UIButton? pressedButton)
    {
        foreach (var child in children)
        {
            if (!child.Enabled || !child.Visible) continue;

            switch (child)
            {
                case UIPanel panel:
                    DispatchChildrenInput(panel.GetChildren(), mousePos - panel.Position, pressed, released, ref pressedButton);
                    break;

                case UIStackPanel sp:
                    DispatchChildrenInput(sp.GetChildren(), mousePos - sp.Position, pressed, released, ref pressedButton);
                    break;

                case UIGrid grid:
                    DispatchChildrenInput(grid.GetChildren(), mousePos - grid.Position, pressed, released, ref pressedButton);
                    break;

                case UITabContainer tab:
                    if (pressed && tab.Contains(mousePos))
                        tab.SelectTab(mousePos);
                    tab.UpdateHover(mousePos);
                    // Tab children are content-origin-relative: translate mouse into content space.
                    var tabContentOrigin = tab.GetContentOrigin();
                    DispatchChildrenInput(tab.GetTabComponents(tab.SelectedTabIndex), mousePos - tabContentOrigin, pressed, released, ref pressedButton);
                    break;

                case UIScrollView sv:
                    if (_dialogScrollbarDragView == sv)
                    {
                        if (_input.IsMouseButtonDown(MouseButton.Left))
                            sv.UpdateScrollbarDrag(mousePos);
                        else if (released)
                        {
                            sv.EndScrollbarDrag();
                            _dialogScrollbarDragView = null;
                        }
                    }
                    else if (_dialogScrollbarDragView == null)
                    {
                        if (pressed && sv.IsOverVerticalScrollbarTrack(mousePos))
                        {
                            if (!sv.IsOverVerticalScrollbar(mousePos))
                                sv.JumpScrollToVertical(mousePos.Y);
                            sv.StartScrollbarDrag(mousePos, isVertical: true);
                            _dialogScrollbarDragView = sv;
                        }
                        else if (pressed && sv.IsOverHorizontalScrollbarTrack(mousePos))
                        {
                            if (!sv.IsOverHorizontalScrollbar(mousePos))
                                sv.JumpScrollToHorizontal(mousePos.X);
                            sv.StartScrollbarDrag(mousePos, isVertical: false);
                            _dialogScrollbarDragView = sv;
                        }
                    }

                    if (sv.Contains(mousePos) && Math.Abs(_input.ScrollWheelDelta) > 0.001f)
                        sv.HandleScroll(_input.ScrollWheelDelta);

                    DispatchScrollViewChildrenInput(sv, mousePos, pressed, released, ref pressedButton);
                    break;

                case UIDropdown dd:
                    if (pressed && dd.Contains(mousePos))
                    {
                        if (dd.IsExpanded && dd.IsOverExpandedList(mousePos))
                            dd.SelectItem(mousePos);
                        else
                        {
                            dd.Toggle();
                            if (dd.IsExpanded)
                                _activeDialogDropdown = dd;
                            else if (_activeDialogDropdown == dd)
                                _activeDialogDropdown = null;
                        }
                    }
                    dd.UpdateHover(mousePos);
                    break;

                case UIButton btn:
                    btn.SetHovered(btn.Contains(mousePos));
                    if (pressed && btn.Contains(mousePos))
                    {
                        btn.SetPressed(true);
                        pressedButton = btn;
                    }
                    if (released)
                    {
                        btn.SetPressed(false);
                        if (btn.Contains(mousePos) && pressedButton == btn)
                            btn.Click();
                        if (pressedButton == btn)
                            pressedButton = null;
                    }
                    break;

                case UICheckbox cb:
                    cb.SetHovered(cb.Contains(mousePos));
                    if (pressed && cb.Contains(mousePos)) cb.Toggle();
                    break;

                case UISlider sl:
                    sl.SetHovered(sl.Contains(mousePos));
                    if (pressed && sl.Contains(mousePos)) { sl.StartDrag(); sl.UpdateDrag(mousePos); }
                    else if (_input.IsMouseButtonDown(MouseButton.Left) && sl.IsDragging) sl.UpdateDrag(mousePos);
                    else if (released) sl.EndDrag();
                    break;

                case UISpinBox spinBoxD:
                    if (spinBoxD.Contains(mousePos))
                    {
                        spinBoxD.SetHoveredIncrement(spinBoxD.ContainsIncrement(mousePos));
                        spinBoxD.SetHoveredDecrement(spinBoxD.ContainsDecrement(mousePos));
                    }
                    else
                    {
                        spinBoxD.SetHoveredIncrement(false);
                        spinBoxD.SetHoveredDecrement(false);
                    }
                    if (pressed && spinBoxD.Contains(mousePos))
                    {
                        if (spinBoxD.ContainsIncrement(mousePos)) { spinBoxD.SetPressedIncrement(true); spinBoxD.Increment(); }
                        else if (spinBoxD.ContainsDecrement(mousePos)) { spinBoxD.SetPressedDecrement(true); spinBoxD.Decrement(); }
                        else if (spinBoxD.ContainsField(mousePos)) spinBoxD.BeginEdit();
                    }
                    if (released) { spinBoxD.SetPressedIncrement(false); spinBoxD.SetPressedDecrement(false); }
                    break;

                case UIRadioButton rb:
                    rb.SetHovered(rb.Contains(mousePos));
                    if (pressed && rb.Contains(mousePos)) rb.Select();
                    break;

                case UITextInput ti:
                    if (pressed)
                    {
                        if (ti.Contains(mousePos))
                        {
                            if (_focusedTextInput != null) _focusedTextInput.SetFocused(false, _input);
                            if (_focusedTextArea != null) { _focusedTextArea.SetFocused(false, _input); _focusedTextArea = null; }
                            _focusedTextInput = ti;
                            ti.SetFocused(true, _input, mousePos.X);
                            ti.StartMouseDrag(mousePos.X);
                        }
                        else if (_focusedTextInput == ti)
                        {
                            ti.SetFocused(false, _input);
                            _focusedTextInput = null;
                        }
                    }
                    else if (_input.IsMouseButtonDown(MouseButton.Left) && _focusedTextInput == ti)
                    {
                        ti.UpdateMouseDrag(mousePos.X);
                    }
                    else if (released && _focusedTextInput == ti)
                    {
                        ti.EndMouseDrag();
                    }
                    break;

                case UITextArea ta:
                    if (pressed)
                    {
                        if (ta.Contains(mousePos))
                        {
                            if (_focusedTextArea != null) _focusedTextArea.SetFocused(false, _input);
                            if (_focusedTextInput != null) { _focusedTextInput.SetFocused(false, _input); _focusedTextInput = null; }
                            _focusedTextArea = ta;
                            ta.SetFocused(true, _input, mousePos.X, mousePos.Y);
                            ta.StartMouseDrag(mousePos.X, mousePos.Y);
                        }
                        else if (_focusedTextArea == ta)
                        {
                            ta.SetFocused(false, _input);
                            _focusedTextArea = null;
                        }
                    }
                    else if (_input.IsMouseButtonDown(MouseButton.Left) && _focusedTextArea == ta)
                    {
                        ta.UpdateMouseDrag(mousePos.X, mousePos.Y);
                    }
                    else if (released && _focusedTextArea == ta)
                    {
                        ta.EndMouseDrag();
                    }
                    break;

                case UILabel lbl when lbl.HasOnClick:
                    if (released && lbl.Contains(mousePos))
                        lbl.Click();
                    break;

                case UIImage img when img.HasOnClick:
                    if (released && img.Contains(mousePos))
                        img.Click();
                    break;
            }
        }
    }

    private void DispatchScrollViewChildrenInput(UIScrollView sv, Vector2 mousePos, bool pressed, bool released, ref UIButton? pressedButton)
    {
        foreach (var child in sv.GetChildren())
        {
            if (!child.Enabled || !child.Visible) continue;

            // Children are in content space; translate to screen space for hit-testing.
            var screenPos = new Vector2(
                sv.Position.X + child.Position.X - sv.ScrollOffset.X,
                sv.Position.Y + child.Position.Y - sv.ScrollOffset.Y);

            var originalPos = child.Position;
            child.Position = screenPos;

            switch (child)
            {
                case UIButton btn:
                    btn.SetHovered(btn.Contains(mousePos));
                    if (pressed && btn.Contains(mousePos))
                    {
                        btn.SetPressed(true);
                        pressedButton = btn;
                    }
                    if (released)
                    {
                        btn.SetPressed(false);
                        if (btn.Contains(mousePos) && pressedButton == btn)
                            btn.Click();
                        if (pressedButton == btn)
                            pressedButton = null;
                    }
                    break;

                case UICheckbox cb:
                    cb.SetHovered(cb.Contains(mousePos));
                    if (pressed && cb.Contains(mousePos)) cb.Toggle();
                    break;

                case UISlider sl:
                    sl.SetHovered(sl.Contains(mousePos));
                    if (pressed && sl.Contains(mousePos)) { sl.StartDrag(); sl.UpdateDrag(mousePos); }
                    else if (_input.IsMouseButtonDown(MouseButton.Left) && sl.IsDragging) sl.UpdateDrag(mousePos);
                    else if (released) sl.EndDrag();
                    break;

                case UISpinBox spinBoxSv:
                    if (spinBoxSv.Contains(mousePos))
                    {
                        spinBoxSv.SetHoveredIncrement(spinBoxSv.ContainsIncrement(mousePos));
                        spinBoxSv.SetHoveredDecrement(spinBoxSv.ContainsDecrement(mousePos));
                    }
                    else
                    {
                        spinBoxSv.SetHoveredIncrement(false);
                        spinBoxSv.SetHoveredDecrement(false);
                    }
                    if (pressed && spinBoxSv.Contains(mousePos))
                    {
                        if (spinBoxSv.ContainsIncrement(mousePos)) { spinBoxSv.SetPressedIncrement(true); spinBoxSv.Increment(); }
                        else if (spinBoxSv.ContainsDecrement(mousePos)) { spinBoxSv.SetPressedDecrement(true); spinBoxSv.Decrement(); }
                        else if (spinBoxSv.ContainsField(mousePos)) spinBoxSv.BeginEdit();
                    }
                    if (released) { spinBoxSv.SetPressedIncrement(false); spinBoxSv.SetPressedDecrement(false); }
                    break;

                case UIRadioButton rb:
                    rb.SetHovered(rb.Contains(mousePos));
                    if (pressed && rb.Contains(mousePos)) rb.Select();
                    break;

                case UITextInput ti:
                    if (pressed)
                    {
                        if (ti.Contains(mousePos))
                        {
                            if (_focusedTextInput != null) _focusedTextInput.SetFocused(false, _input);
                            if (_focusedTextArea != null) { _focusedTextArea.SetFocused(false, _input); _focusedTextArea = null; }
                            _focusedTextInput = ti;
                            ti.SetFocused(true, _input, mousePos.X);
                            ti.StartMouseDrag(mousePos.X);
                        }
                        else if (_focusedTextInput == ti)
                        {
                            ti.SetFocused(false, _input);
                            _focusedTextInput = null;
                        }
                    }
                    else if (_input.IsMouseButtonDown(MouseButton.Left) && _focusedTextInput == ti)
                    {
                        ti.UpdateMouseDrag(mousePos.X);
                    }
                    else if (released && _focusedTextInput == ti)
                    {
                        ti.EndMouseDrag();
                    }
                    break;

                case UITextArea ta:
                    if (pressed)
                    {
                        if (ta.Contains(mousePos))
                        {
                            if (_focusedTextArea != null) _focusedTextArea.SetFocused(false, _input);
                            if (_focusedTextInput != null) { _focusedTextInput.SetFocused(false, _input); _focusedTextInput = null; }
                            _focusedTextArea = ta;
                            ta.SetFocused(true, _input, mousePos.X, mousePos.Y);
                            ta.StartMouseDrag(mousePos.X, mousePos.Y);
                        }
                        else if (_focusedTextArea == ta)
                        {
                            ta.SetFocused(false, _input);
                            _focusedTextArea = null;
                        }
                    }
                    else if (_input.IsMouseButtonDown(MouseButton.Left) && _focusedTextArea == ta)
                    {
                        ta.UpdateMouseDrag(mousePos.X, mousePos.Y);
                    }
                    else if (released && _focusedTextArea == ta)
                    {
                        ta.EndMouseDrag();
                    }
                    break;

                case UIPanel panel:
                {
                    // Restore to content-space position so panel children can be dispatched
                    // using panel-relative coordinates.
                    child.Position = originalPos;
                    var panelScreenOrigin = new Vector2(
                        sv.Position.X + originalPos.X - sv.ScrollOffset.X,
                        sv.Position.Y + originalPos.Y - sv.ScrollOffset.Y);
                    var childrenInPanelSpace = mousePos - panelScreenOrigin;
                    DispatchChildrenInput(panel.GetChildren(), childrenInPanelSpace, pressed, released, ref pressedButton);
                    continue;
                }

                case UIStackPanel sp:
                {
                    child.Position = originalPos;
                    var spScreenOrigin = new Vector2(
                        sv.Position.X + originalPos.X - sv.ScrollOffset.X,
                        sv.Position.Y + originalPos.Y - sv.ScrollOffset.Y);
                    DispatchChildrenInput(sp.GetChildren(), mousePos - spScreenOrigin, pressed, released, ref pressedButton);
                    continue;
                }

                case UIGrid grid:
                {
                    child.Position = originalPos;
                    var gridScreenOrigin = new Vector2(
                        sv.Position.X + originalPos.X - sv.ScrollOffset.X,
                        sv.Position.Y + originalPos.Y - sv.ScrollOffset.Y);
                    DispatchChildrenInput(grid.GetChildren(), mousePos - gridScreenOrigin, pressed, released, ref pressedButton);
                    continue;
                }

                case UIScrollView nestedSv:
                {
                    child.Position = originalPos;
                    var nestedScreenPos = new Vector2(
                        sv.Position.X + originalPos.X - sv.ScrollOffset.X,
                        sv.Position.Y + originalPos.Y - sv.ScrollOffset.Y);
                    nestedSv.Position = nestedScreenPos;

                    if (nestedSv.IsOverVerticalScrollbarTrack(mousePos))
                    {
                        if (!nestedSv.IsOverVerticalScrollbar(mousePos))
                            nestedSv.JumpScrollToVertical(mousePos.Y);
                        nestedSv.StartScrollbarDrag(mousePos, isVertical: true);
                        _scrollbarDragView = nestedSv;
                    }
                    else if (nestedSv.IsOverHorizontalScrollbarTrack(mousePos))
                    {
                        if (!nestedSv.IsOverHorizontalScrollbar(mousePos))
                            nestedSv.JumpScrollToHorizontal(mousePos.X);
                        nestedSv.StartScrollbarDrag(mousePos, isVertical: false);
                        _scrollbarDragView = nestedSv;
                    }

                    if (nestedSv.Contains(mousePos) && Math.Abs(_input.ScrollWheelDelta) > 0.001f)
                        nestedSv.HandleScroll(_input.ScrollWheelDelta);

                    DispatchScrollViewChildrenInput(nestedSv, mousePos, pressed, released, ref pressedButton);
                    nestedSv.Position = originalPos;
                    continue;
                }

                case UIDropdown dd:
                    if (pressed && dd.Contains(mousePos))
                    {
                        if (dd.IsExpanded && dd.IsOverExpandedList(mousePos))
                            dd.SelectItem(mousePos);
                        else
                        {
                            dd.Toggle();
                            if (dd.IsExpanded)
                                _activeDialogDropdown = dd;
                            else if (_activeDialogDropdown == dd)
                                _activeDialogDropdown = null;
                        }
                    }
                    dd.UpdateHover(mousePos);
                    break;

                case UILabel lbl when lbl.HasOnClick:
                    if (released && lbl.Contains(mousePos))
                        lbl.Click();
                    break;

                case UIImage img when img.HasOnClick:
                    if (released && img.Contains(mousePos))
                        img.Click();
                    break;
            }

            child.Position = originalPos;
        }
    }
    /// <summary>
    /// Releases the <see cref="WindowResizedEvent"/> subscription if any.
    /// </summary>
    public void Dispose()
    {
        _resizeSubscription?.Dispose();
        _resizeSubscription = null;
    }
}