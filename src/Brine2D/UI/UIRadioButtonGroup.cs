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
    /// Event fired when selection changes. The newly selected button already has
    /// <see cref="UIRadioButton.IsChecked"/> set to <c>true</c> when this fires.
    /// </summary>
    public event Action<UIRadioButton?>? OnSelectionChanged;

    internal void RegisterButton(UIRadioButton button)
    {
        if (!_buttons.Contains(button))
        {
            _buttons.Add(button);
        }
    }

    internal void UnregisterButton(UIRadioButton button)
    {
        _buttons.Remove(button);
        if (_selectedButton == button)
        {
            _selectedButton = null;
        }
    }

    /// <summary>
    /// Selects a button (deselecting all others) and fires <see cref="OnSelectionChanged"/>.
    /// </summary>
    internal void SelectButton(UIRadioButton button)
    {
        if (_selectedButton == button) return;

        if (_selectedButton != null)
        {
            _selectedButton.IsChecked = false;
        }

        _selectedButton = button;
        _selectedButton.IsChecked = true;

        OnSelectionChanged?.Invoke(_selectedButton);
    }

    /// <summary>
    /// Gets all buttons in this group.
    /// </summary>
    public IReadOnlyList<UIRadioButton> GetButtons() => _buttons.AsReadOnly();
}