using Brine2D.Core;
using System.Numerics;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Single-line text input with cursor, horizontal scrolling, and selection.
/// </summary>
public class UITextInput : IUIComponent, IAnchoredUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    public UITooltip? Tooltip { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }

    /// <summary>
    /// Current text value.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            _cursorPosition = Math.Clamp(_cursorPosition, 0, _text.Length);
            _selectionAnchor = -1;
            _selectionActive = -1;
        }
    }

    /// <summary>
    /// Font to use for rendering (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Current cursor position (character index, 0 = before first character).
    /// </summary>
    public int CursorPosition
    {
        get => _cursorPosition;
        set => _cursorPosition = Math.Clamp(value, 0, _text.Length);
    }

    /// <summary>
    /// Placeholder text shown when empty.
    /// </summary>
    public string Placeholder { get; set; } = "Enter text...";

    /// <summary>
    /// Maximum character length (0 = unlimited).
    /// </summary>
    public int MaxLength { get; set; } = 0;

    /// <summary>
    /// Whether this input is currently focused.
    /// </summary>
    public bool IsFocused { get; private set; }

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Placeholder text color.
    /// </summary>
    public Color PlaceholderColor { get; set; } = new Color(150, 150, 150);

    /// <summary>
    /// Background color when unfocused.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(40, 40, 40);

    /// <summary>
    /// Background color when focused.
    /// </summary>
    public Color FocusedBackgroundColor { get; set; } = new Color(50, 50, 50);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Focused border color.
    /// </summary>
    public Color FocusedBorderColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Selection highlight color.
    /// </summary>
    public Color SelectionColor { get; set; } = new Color(80, 120, 200, 180);

    /// <summary>
    /// When true, the text is hidden behind <see cref="MaskChar"/> characters.
    /// </summary>
    public bool IsPassword { get; set; } = false;

    /// <summary>
    /// Character used to mask text when <see cref="IsPassword"/> is true. Defaults to '●'.
    /// </summary>
    public char MaskChar { get; set; } = '●';

    /// <summary>
    /// When true, the field displays its text but rejects all edits.
    /// Navigation and copy (Ctrl+C) still work.
    /// </summary>
    public bool ReadOnly { get; set; } = false;

    /// <summary>
    /// Optional per-character filter. When set, only characters for which the delegate
    /// returns <c>true</c> are accepted — both from direct typing and from paste.
    /// Leave <c>null</c> to accept all characters.
    /// </summary>
    public Func<char, bool>? CharacterFilter { get; set; }

    /// <summary>
    /// Height in pixels of the cursor bar and text selection highlight.
    /// Defaults to 16. Set this to match the cap-height of a custom <see cref="Font"/>.
    /// </summary>
    public float CursorHeight { get; set; } = 16f;

    /// <summary>
    /// Maximum undo steps retained. Older steps are discarded when exceeded.
    /// Set to 0 for unlimited history. Defaults to 100.
    /// </summary>
    public int UndoStackLimit { get; set; } = 100;

    /// <summary>
    /// Event fired when text changes.
    /// </summary>
    public event Action<string>? OnTextChanged;

    /// <summary>
    /// Event fired when Enter key is pressed.
    /// </summary>
    public event Action<string>? OnSubmit;

    /// <summary>
    /// Event fired when this field gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this field loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    private string _text = string.Empty;
    private int _cursorPosition;
    private float _cursorBlinkTime;
    private bool _cursorVisible = true;
    private float _textScrollOffset;
    private float? _pendingClickX;
    private float? _pendingDoubleClickX;
    private const float CursorBlinkInterval = 0.5f;
    private const float TextPadding = 10f;

    // Selection uses a stable anchor + moving active end.
    // _selectionAnchor is where the selection started; _selectionActive is where it currently ends.
    // The rendered/returned range is always [min, max] of the two.
    // -1 means no selection.
    private int _selectionAnchor = -1;
    private int _selectionActive = -1;

    // Mouse drag-to-select state.
    private bool _isMouseDragging;
    private float? _pendingDragX;

    // Double-click tracking for word selection.
    private float _lastClickTime = -1f;
    private float _elapsedTime;
    private float? _lastClickX;
    private const float DoubleClickInterval = 0.4f;

    // Undo/redo stacks. Each entry is (text, cursorPosition).
    private readonly Stack<(string text, int cursor)> _undoStack = new();
    private readonly Stack<(string text, int cursor)> _redoStack = new();

    private string GetDisplayText() =>
        IsPassword ? new string(MaskChar, _text.Length) : _text;

    public UITextInput(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        _elapsedTime += deltaTime;

        if (!IsFocused) return;

        _cursorBlinkTime += deltaTime;
        if (_cursorBlinkTime >= CursorBlinkInterval)
        {
            _cursorBlinkTime = 0;
            _cursorVisible = !_cursorVisible;
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        var display = GetDisplayText();

        // Resolve a deferred click position to a character index now that we have a renderer.
        if (_pendingClickX.HasValue)
        {
            _cursorPosition = HitTestCharacterIndex(renderer, _pendingClickX.Value, display);
            _pendingClickX = null;
            _cursorBlinkTime = 0;
            _cursorVisible = true;
        }

        // Resolve a pending double-click: select the word under the cursor.
        if (_pendingDoubleClickX.HasValue)
        {
            int clickIndex = HitTestCharacterIndex(renderer, _pendingDoubleClickX.Value, display);
            _pendingDoubleClickX = null;
            _isMouseDragging = false;

            if (!IsPassword && Text.Length > 0)
            {
                int wordStart = clickIndex;
                while (wordStart > 0 && !char.IsWhiteSpace(Text[wordStart - 1]))
                    wordStart--;
                int wordEnd = clickIndex;
                while (wordEnd < Text.Length && !char.IsWhiteSpace(Text[wordEnd]))
                    wordEnd++;

                if (wordEnd > wordStart)
                {
                    _selectionAnchor = wordStart;
                    _selectionActive = wordEnd;
                    _cursorPosition = wordEnd;
                }
            }

            _cursorBlinkTime = 0;
            _cursorVisible = true;
        }

        // Resolve a pending drag update.
        if (_pendingDragX.HasValue)
        {
            int dragIndex = HitTestCharacterIndex(renderer, _pendingDragX.Value, display);
            _pendingDragX = null;
            if (!HasSelection()) StartSelectionAt(_cursorPosition);
            _cursorPosition = dragIndex;
            _selectionActive = dragIndex;
        }

        const float borderThickness = 2f;
        float textAreaWidth = Size.X - TextPadding * 2f;
        float textX = Position.X + TextPadding;
        float textY = Position.Y + (Size.Y / 2f) - 8f;

        var bgColor = IsFocused ? FocusedBackgroundColor : BackgroundColor;
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, bgColor);

        var borderColor = IsFocused ? FocusedBorderColor : BorderColor;
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, borderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, borderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, borderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, borderColor);

        var textClip = new Rectangle(
            Position.X + borderThickness,
            Position.Y + borderThickness,
            Size.X - borderThickness * 2f,
            Size.Y - borderThickness * 2f);

        renderer.PushScissorRect(textClip);

        if (string.IsNullOrEmpty(display))
        {
            _textScrollOffset = 0f;
            if (Font == null)
                renderer.DrawText(Placeholder, textX, textY, PlaceholderColor);
            else
                renderer.DrawText(Placeholder, textX, textY, new TextRenderOptions { Color = PlaceholderColor, Font = Font });
        }
        else
        {
            float totalTextWidth = Font == null
                ? renderer.MeasureText(display).X
                : renderer.MeasureText(display, new TextRenderOptions { Font = Font }).X;
            float maxScroll = Math.Max(0f, totalTextWidth - textAreaWidth);
            _textScrollOffset = Math.Clamp(_textScrollOffset, 0f, maxScroll);

            if (IsFocused)
            {
                float cursorPixelOffset = _cursorPosition > 0
                    ? (Font == null
                        ? renderer.MeasureText(display[.._cursorPosition]).X
                        : renderer.MeasureText(display[.._cursorPosition], new TextRenderOptions { Font = Font }).X)
                    : 0f;

                if (cursorPixelOffset - _textScrollOffset < 0f)
                    _textScrollOffset = cursorPixelOffset;
                else if (cursorPixelOffset - _textScrollOffset > textAreaWidth)
                    _textScrollOffset = cursorPixelOffset - textAreaWidth;
            }

            if (IsFocused && HasSelection())
            {
                var (sStart, sEnd) = GetSelectionRange();
                float startPx = sStart > 0
                    ? (Font == null ? renderer.MeasureText(display[..sStart]).X : renderer.MeasureText(display[..sStart], new TextRenderOptions { Font = Font }).X)
                    : 0f;
                float endPx = Font == null
                    ? renderer.MeasureText(display[..sEnd]).X
                    : renderer.MeasureText(display[..sEnd], new TextRenderOptions { Font = Font }).X;

                float selX = textX + startPx - _textScrollOffset;
                float selW = Math.Max(0f, endPx - startPx);
                renderer.DrawRectangleFilled(selX, textY, selW, CursorHeight, SelectionColor);
            }

            var activeTextColor = Enabled ? TextColor : new Color(100, 100, 100);
            if (Font == null)
                renderer.DrawText(display, textX - _textScrollOffset, textY, activeTextColor);
            else
                renderer.DrawText(display, textX - _textScrollOffset, textY, new TextRenderOptions { Color = activeTextColor, Font = Font });
        }

        if (IsFocused && _cursorVisible)
        {
            float cursorPixelOffset = _cursorPosition > 0
                ? (Font == null
                    ? renderer.MeasureText(display[.._cursorPosition]).X
                    : renderer.MeasureText(display[.._cursorPosition], new TextRenderOptions { Font = Font }).X)
                : 0f;
            float cursorX = textX + cursorPixelOffset - _textScrollOffset;
            renderer.DrawRectangleFilled(cursorX, textY, 2f, CursorHeight, TextColor);
        }

        renderer.PopScissorRect();
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Called by UICanvas when input is focused or unfocused.
    /// </summary>
    internal void SetFocused(bool focused, IInputContext input)
    {
        SetFocused(focused, input, clickX: null);
    }

    /// <summary>
    /// Called by UICanvas when the field is focused by a mouse click.
    /// <paramref name="clickX"/> is the screen X of the click; the cursor is placed
    /// at the nearest character boundary on the next Render call.
    /// </summary>
    internal void SetFocused(bool focused, IInputContext input, float? clickX)
    {
        // Idempotent: if state already matches, do nothing.
        if (IsFocused == focused) return;

        IsFocused = focused;
        _cursorBlinkTime = 0;
        _cursorVisible = true;

        if (focused)
        {
            if (clickX.HasValue)
                _pendingClickX = clickX.Value;
            else
            {
                _cursorPosition = Text.Length;
                _textScrollOffset = 0f;
            }
            input.StartTextInput();
            OnFocusGained?.Invoke();
        }
        else
        {
            _pendingClickX = null;
            _textScrollOffset = 0f;
            _lastClickTime = -1f;
            _lastClickX = null;
            input.StopTextInput();
            ClearSelection();
            OnFocusLost?.Invoke();
        }
    }

    /// <summary>
    /// Called by UICanvas when the mouse button is pressed inside the field (drag-to-select start).
    /// Returns true when the click was a double-click and word selection was applied (no drag
    /// should begin in that case).
    /// </summary>
    internal bool StartMouseDrag(float screenX)
    {
        if (!IsFocused || !Enabled) return false;

        bool isDoubleClick = _lastClickTime >= 0f &&
                             (_elapsedTime - _lastClickTime) <= DoubleClickInterval &&
                             _lastClickX.HasValue &&
                             Math.Abs(screenX - _lastClickX.Value) < 5f;

        _lastClickTime = _elapsedTime;
        _lastClickX = screenX;

        if (isDoubleClick)
        {
            _pendingDoubleClickX = screenX;
            return true;
        }

        _isMouseDragging = true;
        ClearSelection();
        _pendingClickX = screenX;
        return false;
    }

    /// <summary>
    /// Called by UICanvas each frame the mouse is held and moving (drag-to-select update).
    /// </summary>
    internal void UpdateMouseDrag(float screenX)
    {
        if (!_isMouseDragging || !IsFocused || !Enabled) return;
        _pendingDragX = screenX;
    }

    /// <summary>
    /// Called by UICanvas when the mouse button is released (drag-to-select end).
    /// </summary>
    internal void EndMouseDrag()
    {
        _isMouseDragging = false;
    }

    /// <summary>
    /// Called by UICanvas to handle text input.
    /// </summary>
    internal void HandleTextInput(IInputContext input)
    {
        if (!IsFocused || !Enabled) return;

        _cursorBlinkTime = 0;
        _cursorVisible = true;

        bool ctrl = input.IsKeyDown(Key.LeftControl) || input.IsKeyDown(Key.RightControl);
        bool shift = input.IsKeyDown(Key.LeftShift) || input.IsKeyDown(Key.RightShift);

        // Ctrl+A select all
        if (ctrl && input.IsKeyPressed(Key.A))
        {
            if (Text.Length > 0)
            {
                _selectionAnchor = 0;
                _selectionActive = Text.Length;
                _cursorPosition = _selectionActive;
            }
            return;
        }

        // Clipboard operations
        if (ctrl && input.IsKeyPressed(Key.C))
        {
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                input.SetClipboardText(Text[s..e]);
            }
            return;
        }

        if (ReadOnly) return;

        if (ctrl && input.IsKeyPressed(Key.X))
        {
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                PushUndoState();
                input.SetClipboardText(Text[s..e]);
                Text = Text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
                OnTextChanged?.Invoke(Text);
            }
            return;
        }

        if (ctrl && input.IsKeyPressed(Key.V))
        {
            var paste = input.GetClipboardText() ?? string.Empty;
            if (!string.IsNullOrEmpty(paste))
            {
                PushUndoState();
                if (HasSelection())
                {
                    var (s, e) = GetSelectionRange();
                    Text = Text.Remove(s, e - s);
                    _cursorPosition = s;
                    ClearSelection();
                }
                    foreach (char c in paste)
                    {
                        if ((MaxLength == 0 || Text.Length < MaxLength) && (CharacterFilter == null || CharacterFilter(c)))
                        {
                            Text = Text.Insert(_cursorPosition, c.ToString());
                            _cursorPosition++;
                        }
                    }
                    OnTextChanged?.Invoke(Text);
                }
                return;
                }

                if (ctrl && input.IsKeyPressed(Key.Z))
        {
            Undo();
            return;
        }

        if (ctrl && input.IsKeyPressed(Key.Y))
        {
            Redo();
            return;
        }

        // Navigation and selection with Shift
        if (input.IsKeyPressed(Key.Left))
        {
            if (ctrl)
            {
                int dest = FindWordBoundaryBackward(_cursorPosition);
                if (shift)
                {
                    if (!HasSelection()) StartSelectionAt(_cursorPosition);
                    _cursorPosition = dest;
                    _selectionActive = _cursorPosition;
                }
                else
                {
                    _cursorPosition = dest;
                    ClearSelection();
                }
            }
            else if (_cursorPosition > 0)
            {
                if (shift)
                {
                    if (!HasSelection()) StartSelectionAt(_cursorPosition);
                    _cursorPosition--;
                    _selectionActive = _cursorPosition;
                }
                else
                {
                    _cursorPosition--;
                    ClearSelection();
                }
            }
        }

        if (input.IsKeyPressed(Key.Right))
        {
            if (ctrl)
            {
                int dest = FindWordBoundaryForward(_cursorPosition);
                if (shift)
                {
                    if (!HasSelection()) StartSelectionAt(_cursorPosition);
                    _cursorPosition = dest;
                    _selectionActive = _cursorPosition;
                }
                else
                {
                    _cursorPosition = dest;
                    ClearSelection();
                }
            }
            else if (_cursorPosition < Text.Length)
            {
                if (shift)
                {
                    if (!HasSelection()) StartSelectionAt(_cursorPosition);
                    _cursorPosition++;
                    _selectionActive = _cursorPosition;
                }
                else
                {
                    _cursorPosition++;
                    ClearSelection();
                }
            }
        }

        if (input.IsKeyPressed(Key.Home))
        {
            if (shift)
            {
                if (!HasSelection()) StartSelectionAt(_cursorPosition);
                _cursorPosition = 0;
                _selectionActive = _cursorPosition;
            }
            else
            {
                _cursorPosition = 0;
                ClearSelection();
            }
        }

        if (input.IsKeyPressed(Key.End))
        {
            if (shift)
            {
                if (!HasSelection()) StartSelectionAt(_cursorPosition);
                _cursorPosition = Text.Length;
                _selectionActive = _cursorPosition;
            }
            else
            {
                _cursorPosition = Text.Length;
                ClearSelection();
            }
        }

        // Backspace/Delete with selection support
        if (input.IsBackspacePressed())
        {
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                PushUndoState();
                Text = Text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
                OnTextChanged?.Invoke(Text);
            }
            else if (_cursorPosition > 0)
            {
                PushUndoState();
                Text = Text.Remove(_cursorPosition - 1, 1);
                _cursorPosition--;
                OnTextChanged?.Invoke(Text);
            }
            return;
        }

        if (input.IsDeletePressed())
        {
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                PushUndoState();
                Text = Text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
                OnTextChanged?.Invoke(Text);
            }
            else if (_cursorPosition < Text.Length)
            {
                PushUndoState();
                Text = Text.Remove(_cursorPosition, 1);
                OnTextChanged?.Invoke(Text);
            }
            return;
        }

        if (input.IsReturnPressed())
        {
            OnSubmit?.Invoke(Text);
            return;
        }

        var textInput = input.GetTextInput();
        if (!string.IsNullOrEmpty(textInput))
        {
            PushUndoState();
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                Text = Text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
            }

            foreach (char c in textInput)
            {
                if ((MaxLength == 0 || Text.Length < MaxLength) && (CharacterFilter == null || CharacterFilter(c)))
                {
                    Text = Text.Insert(_cursorPosition, c.ToString());
                    _cursorPosition++;
                }
            }
            OnTextChanged?.Invoke(Text);
        }
    }

    private void PushUndoState()
    {
        _undoStack.Push((Text, _cursorPosition));
        _redoStack.Clear();

        if (UndoStackLimit > 0 && _undoStack.Count > UndoStackLimit)
        {
            // Stack only supports pop from top; rebuild it dropping the oldest (bottom) entry.
            var entries = _undoStack.ToArray(); // top-first
            _undoStack.Clear();
            for (int i = entries.Length - 2; i >= 0; i--)
                _undoStack.Push(entries[i]);
        }
    }

    /// <summary>
    /// Undoes the last text change. No-op if the undo stack is empty.
    /// </summary>
    internal void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push((Text, _cursorPosition));
        var (text, cursor) = _undoStack.Pop();
        Text = text;
        _cursorPosition = Math.Clamp(cursor, 0, Text.Length);
        ClearSelection();
        OnTextChanged?.Invoke(Text);
    }

    /// <summary>
    /// Redoes the last undone change. No-op if the redo stack is empty.
    /// </summary>
    internal void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push((Text, _cursorPosition));
        var (text, cursor) = _redoStack.Pop();
        Text = text;
        _cursorPosition = Math.Clamp(cursor, 0, Text.Length);
        ClearSelection();
        OnTextChanged?.Invoke(Text);
    }

    /// <summary>
    /// Clears the undo and redo history.
    /// </summary>
    public void ClearUndoHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    internal int UndoStackDepthForTest => _undoStack.Count;
    internal int RedoStackDepthForTest => _redoStack.Count;
    internal bool IsMouseDraggingForTest => _isMouseDragging;
    internal bool HasPendingDoubleClickForTest => _pendingDoubleClickX.HasValue;

    private bool HasSelection() =>
        _selectionAnchor >= 0 && _selectionActive >= 0 && _selectionAnchor != _selectionActive;

    private (int start, int end) GetSelectionRange()
    {
        if (!HasSelection()) return (0, 0);
        return _selectionAnchor < _selectionActive
            ? (_selectionAnchor, _selectionActive)
            : (_selectionActive, _selectionAnchor);
    }

    internal bool HasSelectionForTest() => HasSelection();

    internal (int start, int end) GetSelectionRangeForTest() => GetSelectionRange();

    private void StartSelectionAt(int index)
    {
        _selectionAnchor = index;
        _selectionActive = index;
    }

    private void ClearSelection()
    {
        _selectionAnchor = -1;
        _selectionActive = -1;
    }

    /// <summary>
    /// Returns the character index closest to <paramref name="screenX"/> given the current
    /// scroll offset. Used to position the cursor on mouse click or drag.
    /// <paramref name="displayText"/> should be <see cref="GetDisplayText()"/> so that
    /// password-masked fields are measured correctly.
    /// Widths are accumulated incrementally (O(n) measurements instead of O(n²)).
    /// </summary>
    private int HitTestCharacterIndex(IRenderer renderer, float screenX, string displayText)
    {
        if (string.IsNullOrEmpty(displayText)) return 0;

        float localX = screenX - Position.X - TextPadding + _textScrollOffset;
        localX = Math.Max(0f, localX);

        float prevWidth = 0f;
        for (int i = 1; i <= displayText.Length; i++)
        {
            float width = Font == null
                ? renderer.MeasureText(displayText[..i]).X
                : renderer.MeasureText(displayText[..i], new TextRenderOptions { Font = Font }).X;

            float midpoint = (prevWidth + width) / 2f;
            if (localX < midpoint)
                return i - 1;

            prevWidth = width;
        }

        return displayText.Length;
    }

    /// <summary>
    /// Returns the index of the start of the previous word (or 0 if already at the beginning).
    /// Skips whitespace, then skips the preceding word.
    /// </summary>
    private int FindWordBoundaryBackward(int from)
    {
        if (from <= 0) return 0;
        int i = from - 1;
        while (i > 0 && char.IsWhiteSpace(Text[i]))
            i--;
        while (i > 0 && !char.IsWhiteSpace(Text[i - 1]))
            i--;
        return i;
    }

    /// <summary>
    /// Returns the index just past the end of the next word (or Text.Length if already at the end).
    /// Skips whitespace, then skips the following word.
    /// </summary>
    private int FindWordBoundaryForward(int from)
    {
        if (from >= Text.Length) return Text.Length;
        int i = from;
        while (i < Text.Length && char.IsWhiteSpace(Text[i]))
            i++;
        while (i < Text.Length && !char.IsWhiteSpace(Text[i]))
            i++;
        return i;
    }

}