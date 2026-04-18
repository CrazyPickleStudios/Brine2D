using System.Numerics;
using Brine2D.Input;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Canvas that contains and manages UI components.
/// Handles input routing and rendering.
/// </summary>
public class UICanvas : IInputLayer
{
    private readonly List<IUIComponent> _components = new();
    private readonly IInputContext _input;

    private UIButton? _hoveredButton;
    private UIButton? _pressedButton;
    private UISlider? _activeSlider;
    private UITextInput? _focusedTextInput;
    private UICheckbox? _hoveredCheckbox;
    private UIDropdown? _activeDropdown;
    private UIRadioButton? _hoveredRadioButton;
    private UITabContainer? _hoveredTabContainer;
    private UIScrollView? _activeScrollView;
    private UIDialog? _activeDialog;

    /// <summary>
    /// UI has high priority so it intercepts input first.
    /// </summary>
    public int Priority => 1000;

    private UITooltip? _activeTooltip;
    private IUIComponent? _tooltipOwner;

    public UICanvas(IInputContext input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public void Add(IUIComponent component)
    {
        _components.Add(component);
    }

    public void Remove(IUIComponent component)
    {
        _components.Remove(component);
    }

    public void Clear()
    {
        _components.Clear();
        _hoveredButton = null;
        _pressedButton = null;
        _activeSlider = null;
        _focusedTextInput = null;
        _hoveredCheckbox = null;
        _activeDropdown = null;
        _hoveredRadioButton = null;
        _hoveredTabContainer = null;
        _activeTooltip = null;
        _tooltipOwner = null;
    }

    public void Update(float deltaTime)
    {
        foreach (var component in _components)
        {
            if (component.Enabled)
            {
                component.Update(deltaTime);
            }
        }

        _activeTooltip?.Update(deltaTime);
    }

    public void Render(IRenderer renderer)
    {
        var previousCamera = renderer.Camera;
        renderer.Camera = null;

        foreach (var component in _components)
        {
            if (component.Visible)
            {
                component.Render(renderer);
            }
        }

        if (_activeTooltip != null && _activeTooltip.Visible)
        {
            _activeTooltip.Render(renderer);
        }

        renderer.Camera = previousCamera;
    }

    /// <summary>
    /// Process keyboard input for UI. Returns true if input was consumed.
    /// When <paramref name="consumed"/> is true, unfocuses any active text input.
    /// </summary>
    public bool ProcessKeyboardInput(IInputContext input, bool consumed)
    {
        if (consumed)
        {
            if (_focusedTextInput != null && _focusedTextInput.IsFocused)
                _focusedTextInput.SetFocused(false, input);
            return false;
        }

        if (_focusedTextInput != null && _focusedTextInput.IsFocused)
        {
            HandleTextInputKeyboard();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Process mouse input for UI. Returns true if input was consumed.
    /// When <paramref name="consumed"/> is true, clears hover/press state.
    /// </summary>
    public bool ProcessMouseInput(IInputContext input, bool consumed)
    {
        if (consumed)
        {
            ClearInteractionState();
            return false;
        }

        HandleButtonInput();
        HandleSliderInput();
        HandleTextInputMouse();
        HandleCheckboxInput();
        HandleDropdownInput();
        HandleRadioButtonInput();
        HandleTabContainerInput();
        HandleScrollViewInput();
        HandleDialogInput();
        HandleTooltips(input);

        bool isInteractingWithUI =
            _hoveredButton != null || 
            _pressedButton != null || 
            _activeSlider?.IsDragging == true ||
            _hoveredCheckbox != null ||
            _activeDropdown != null ||
            _hoveredRadioButton != null ||
            _hoveredTabContainer != null ||
            _activeScrollView != null ||
            _activeDialog != null;

        return isInteractingWithUI;
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
        _hoveredCheckbox = null;
        _activeDropdown = null;
        _hoveredRadioButton = null;
        _hoveredTabContainer = null;
        _activeScrollView = null;
        _activeDialog = null;
    }

    private void HandleButtonInput()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_hoveredButton != null)
        {
            _hoveredButton.SetHovered(false);
            _hoveredButton = null;
        }

        foreach (var component in _components)
        {
            if (component is UIButton button && button.Enabled && button.Visible)
            {
                if (button.Contains(mouseScreenPos))
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
                {
                    _pressedButton.Click();
                }

                _pressedButton = null;
            }
        }
    }

    private void HandleSliderInput()
    {
        var mouseScreenPos = _input.MousePosition;

        foreach (var component in _components)
        {
            if (component is UISlider slider && slider.Enabled && slider.Visible)
            {
                slider.SetHovered(slider.Contains(mouseScreenPos));
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left) && _activeSlider == null)
        {
            foreach (var component in _components)
            {
                if (component is UISlider slider && slider.Enabled && slider.Visible && slider.Contains(mouseScreenPos))
                {
                    slider.StartDrag();
                    slider.UpdateDrag(mouseScreenPos);
                    _activeSlider = slider;
                    break;
                }
            }
        }

        if (_activeSlider != null && _input.IsMouseButtonDown(MouseButton.Left))
        {
            _activeSlider.UpdateDrag(mouseScreenPos);
        }

        if (_input.IsMouseButtonReleased(MouseButton.Left) && _activeSlider != null)
        {
            _activeSlider.EndDrag();
            _activeSlider = null;
        }
    }

    private void HandleTextInputMouse()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            UITextInput? clickedInput = null;

            foreach (var component in _components)
            {
                if (component is UITextInput textInput && textInput.Enabled && textInput.Visible)
                {
                    if (textInput.Contains(mouseScreenPos))
                    {
                        clickedInput = textInput;
                        break;
                    }
                }
            }

            if (_focusedTextInput != null)
            {
                _focusedTextInput.SetFocused(false, _input);
            }

            _focusedTextInput = clickedInput;

            if (_focusedTextInput != null)
            {
                _focusedTextInput.SetFocused(true, _input);
            }
        }
    }

    private void HandleTextInputKeyboard()
    {
        if (_focusedTextInput == null || !_focusedTextInput.IsFocused)
            return;

        _focusedTextInput.HandleTextInput(_input);
    }

    private void HandleCheckboxInput()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_hoveredCheckbox != null)
        {
            _hoveredCheckbox.SetHovered(false);
            _hoveredCheckbox = null;
        }

        foreach (var component in _components)
        {
            if (component is UICheckbox checkbox && checkbox.Enabled && checkbox.Visible)
            {
                if (checkbox.Contains(mouseScreenPos))
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
            {
                _hoveredCheckbox.Toggle();
            }
        }
    }

    private void HandleDropdownInput()
    {
        var mouseScreenPos = _input.MousePosition;

        if (_activeDropdown != null && _activeDropdown.IsExpanded)
        {
            _activeDropdown.UpdateHover(mouseScreenPos);
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            UIDropdown? clickedDropdown = null;

            foreach (var component in _components)
            {
                if (component is UIDropdown dropdown && dropdown.Enabled && dropdown.Visible)
                {
                    if (dropdown.Contains(mouseScreenPos))
                    {
                        clickedDropdown = dropdown;
                        break;
                    }
                }
            }

            if (clickedDropdown != null)
            {
                if (clickedDropdown.IsExpanded && 
                    mouseScreenPos.Y > clickedDropdown.Position.Y + clickedDropdown.Size.Y)
                {
                    clickedDropdown.SelectItem(mouseScreenPos);
                }
                else
                {
                    if (_activeDropdown != null && _activeDropdown != clickedDropdown)
                    {
                        _activeDropdown.Close();
                    }

                    clickedDropdown.Toggle();
                    _activeDropdown = clickedDropdown.IsExpanded ? clickedDropdown : null;
                }
            }
            else
            {
                if (_activeDropdown != null)
                {
                    _activeDropdown.Close();
                    _activeDropdown = null;
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

        foreach (var component in _components)
        {
            if (component is UIRadioButton radioButton && radioButton.Enabled && radioButton.Visible)
            {
                if (radioButton.Contains(mouseScreenPos))
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
            {
                _hoveredRadioButton.Select();
            }
        }
    }

    private void HandleTabContainerInput()
    {
        var mouseScreenPos = _input.MousePosition;

        _hoveredTabContainer = null;

        foreach (var component in _components)
        {
            if (component is UITabContainer tabContainer && tabContainer.Enabled && tabContainer.Visible)
            {
                if (tabContainer.Contains(mouseScreenPos))
                {
                    tabContainer.UpdateHover(mouseScreenPos);
                    _hoveredTabContainer = tabContainer;
                    break;
                }
            }
        }

        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_hoveredTabContainer != null)
            {
                _hoveredTabContainer.SelectTab(mouseScreenPos);
            }
        }
    }

    private void HandleScrollViewInput()
    {
        var mouseScreenPos = _input.MousePosition;
        var scrollDelta = _input.ScrollWheelDelta;

        _activeScrollView = null;
        foreach (var component in _components)
        {
            if (component is UIScrollView scrollView && scrollView.Enabled && scrollView.Visible)
            {
                if (scrollView.Contains(mouseScreenPos))
                {
                    _activeScrollView = scrollView;
                    break;
                }
            }
        }

        if (_activeScrollView != null && Math.Abs(scrollDelta) > 0.001f)
        {
            _activeScrollView.HandleScroll(scrollDelta);
        }
    }

    private void HandleDialogInput()
    {
        var mouseScreenPos = _input.MousePosition;

        for (int i = _components.Count - 1; i >= 0; i--)
        {
            if (_components[i] is UIDialog dialog && dialog.Enabled && dialog.Visible)
            {
                _activeDialog = dialog;
                break;
            }
        }

        if (_activeDialog != null)
        {
            bool isPressed = _input.IsMouseButtonPressed(MouseButton.Left);
            bool isReleased = _input.IsMouseButtonReleased(MouseButton.Left);

            _activeDialog.ProcessButtonInput(mouseScreenPos, isPressed, isReleased);
        }
    }

    private void HandleTooltips(IInputContext input)
    {
        var mousePos = input.MousePosition;
        IUIComponent? hoveredComponent = null;

        for (int i = _components.Count - 1; i >= 0; i--)
        {
            var component = _components[i];
            if (component.Enabled && component.Visible && component.Contains(mousePos))
            {
                hoveredComponent = component;
                break;
            }
        }

        if (hoveredComponent != null && hoveredComponent.Tooltip != null)
        {
            if (_tooltipOwner != hoveredComponent)
            {
                _activeTooltip?.OnHoverEnd();
                _activeTooltip = hoveredComponent.Tooltip;
                _tooltipOwner = hoveredComponent;
                _activeTooltip.OnHoverStart(mousePos);
            }
            else
            {
                _activeTooltip?.UpdatePosition(mousePos);
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
}