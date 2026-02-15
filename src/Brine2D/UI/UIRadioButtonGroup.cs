namespace Brine2D.UI;

/// <summary>
/// Manages a group of radio buttons to ensure only one is selected at a time.
/// </summary>
public class UIRadioButtonGroup
{
    private readonly List<UIRadioButton> _buttons = new();
    private UIRadioButton? _selectedButton;

    /// <summary>
    /// Currently selected button in the group (null if none selected).
    /// </summary>
    public UIRadioButton? SelectedButton => _selectedButton;

    /// <summary>
    /// Index of the selected button (-1 if none selected).
    /// </summary>
    public int SelectedIndex => _selectedButton != null ? _buttons.IndexOf(_selectedButton) : -1;

    /// <summary>
    /// Event fired when selection changes.
    /// </summary>
    public event Action<UIRadioButton?>? OnSelectionChanged;

    /// <summary>
    /// Registers a radio button with this group.
    /// </summary>
    internal void RegisterButton(UIRadioButton button)
    {
        if (!_buttons.Contains(button))
        {
            _buttons.Add(button);
        }
    }

    /// <summary>
    /// Unregisters a radio button from this group.
    /// </summary>
    internal void UnregisterButton(UIRadioButton button)
    {
        _buttons.Remove(button);
        if (_selectedButton == button)
        {
            _selectedButton = null;
        }
    }

    /// <summary>
    /// Selects a button in the group (deselects all others).
    /// </summary>
    internal void SelectButton(UIRadioButton button)
    {
        if (_selectedButton == button) return;

        // Deselect previous button
        if (_selectedButton != null)
        {
            _selectedButton.IsChecked = false;
        }

        // Select new button
        _selectedButton = button;
        OnSelectionChanged?.Invoke(_selectedButton);
    }

    /// <summary>
    /// Gets all buttons in this group.
    /// </summary>
    public IReadOnlyList<UIRadioButton> GetButtons() => _buttons.AsReadOnly();
}