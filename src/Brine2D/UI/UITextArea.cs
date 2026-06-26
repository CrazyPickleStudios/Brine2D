using Brine2D.Core;
using System.Numerics;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Multiline text area with cursor, selection, scrolling, clipboard, and undo/redo.
/// Use <see cref="UITextInput"/> for single-line input.
/// </summary>
public class UITextArea : IUIComponent, IAnchoredUIComponent
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
    /// Current text value. Lines are separated by '\n'.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            _cursorPosition = Math.Clamp(_cursorPosition, 0, _text.Length);
            ClearSelection();
        }
    }

    /// <summary>
    /// Font to use for rendering (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Current cursor position (character index into <see cref="Text"/>, 0 = before first character).
    /// </summary>
    public int CursorPosition
    {
        get => _cursorPosition;
        set => _cursorPosition = Math.Clamp(value, 0, _text.Length);
    }

    /// <summary>
    /// Placeholder text shown when empty and unfocused.
    /// </summary>
    public string Placeholder { get; set; } = "Enter text...";

    /// <summary>
    /// Maximum character length (0 = unlimited).
    /// </summary>
    public int MaxLength { get; set; } = 0;

    /// <summary>
    /// Whether this text area is currently focused.
    /// </summary>
    public bool IsFocused { get; private set; }

    public Color TextColor { get; set; } = Color.White;
    public Color PlaceholderColor { get; set; } = new Color(150, 150, 150);
    public Color BackgroundColor { get; set; } = new Color(40, 40, 40);
    public Color FocusedBackgroundColor { get; set; } = new Color(50, 50, 50);
    public Color BorderColor { get; set; } = new Color(100, 100, 100);
    public Color FocusedBorderColor { get; set; } = new Color(120, 180, 255);
    public Color SelectionColor { get; set; } = new Color(80, 120, 200, 180);

    /// <summary>
    /// Line height in pixels. Defaults to 20. Set this to match the cap-height of a custom <see cref="Font"/>.
    /// </summary>
    public float LineHeight { get; set; } = 20f;

    /// <summary>
    /// Maximum number of undo steps retained. Defaults to 100.
    /// </summary>
    public int UndoStackLimit { get; set; } = 100;

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
    /// When true, pressing Tab inserts <c>'\t'</c> instead of advancing focus.
    /// Defaults to false.
    /// </summary>
    public bool TabInsertsTab { get; set; } = false;

    /// <summary>
    /// Event fired when text changes.
    /// </summary>
    public event Action<string>? OnTextChanged;

    /// <summary>
    /// Event fired when this text area gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this text area loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    private string _text = string.Empty;
    private int _cursorPosition;
    private float _cursorBlinkTime;
    private bool _cursorVisible = true;
    private float _verticalScrollOffset;
    private const float CursorBlinkInterval = 0.5f;
    private const float TextPadding = 6f;

    private int _selectionAnchor = -1;
    private int _selectionActive = -1;

    private bool _isMouseDragging;
    private float? _pendingClickX;
    private float? _pendingClickY;
    private float? _pendingDragX;
    private float? _pendingDragY;

    private readonly Stack<(string text, int cursor)> _undoStack = new();
    private readonly Stack<(string text, int cursor)> _redoStack = new();

    public UITextArea(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
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

        if (_pendingClickX.HasValue && _pendingClickY.HasValue)
        {
            _cursorPosition = HitTestCharacterIndex(renderer, _pendingClickX.Value, _pendingClickY.Value);
            _pendingClickX = null;
            _pendingClickY = null;
            _cursorBlinkTime = 0;
            _cursorVisible = true;
        }

        if (_pendingDragX.HasValue && _pendingDragY.HasValue)
        {
            int dragIndex = HitTestCharacterIndex(renderer, _pendingDragX.Value, _pendingDragY.Value);
            _pendingDragX = null;
            _pendingDragY = null;
            if (!HasSelection()) StartSelectionAt(_cursorPosition);
            _cursorPosition = dragIndex;
            _selectionActive = dragIndex;
        }

        const float borderThickness = 2f;

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

        if (string.IsNullOrEmpty(_text) && !IsFocused)
        {
            renderer.DrawText(Placeholder, Position.X + TextPadding, Position.Y + TextPadding, new TextRenderOptions { Color = PlaceholderColor, Font = Font });
        }
        else
        {
            var lines = _text.Split('\n');
            var (selStart, selEnd) = HasSelection() ? GetSelectionRange() : (-1, -1);

            // Clamp vertical scroll
            float totalContentHeight = lines.Length * LineHeight;
            float maxScroll = Math.Max(0f, totalContentHeight - (Size.Y - TextPadding * 2f));
            _verticalScrollOffset = Math.Clamp(_verticalScrollOffset, 0f, maxScroll);

            // Ensure cursor is visible
            if (IsFocused)
            {
                var (cursorLine, _) = GetLineAndColumn(_cursorPosition);
                float cursorTop = TextPadding + cursorLine * LineHeight - _verticalScrollOffset;
                float cursorBottom = cursorTop + LineHeight;
                float visibleTop = 0f;
                float visibleBottom = Size.Y - TextPadding;

                if (cursorTop < visibleTop)
                    _verticalScrollOffset -= visibleTop - cursorTop;
                else if (cursorBottom > visibleBottom)
                    _verticalScrollOffset += cursorBottom - visibleBottom;

                _verticalScrollOffset = Math.Clamp(_verticalScrollOffset, 0f, maxScroll);
            }

            int charIndex = 0;
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                float lineY = Position.Y + TextPadding + lineIndex * LineHeight - _verticalScrollOffset;
                float textX = Position.X + TextPadding;
                string line = lines[lineIndex];
                int lineStart = charIndex;
                int lineEnd = charIndex + line.Length;

                // Draw selection highlight for this line
                if (selStart >= 0 && selEnd > selStart)
                {
                    int overlapStart = Math.Max(selStart, lineStart);
                    int overlapEnd = Math.Min(selEnd, lineEnd);

                    if (overlapStart < overlapEnd || (selStart <= lineEnd && selEnd >= lineStart))
                    {
                        float selStartX = textX;
                        if (overlapStart > lineStart)
                            selStartX = textX + MeasureLinePrefix(renderer, line, overlapStart - lineStart);

                        float selEndX;
                        if (selEnd > lineEnd)
                            selEndX = textX + MeasureLinePrefix(renderer, line, line.Length) + 4f;
                        else
                            selEndX = textX + MeasureLinePrefix(renderer, line, overlapEnd - lineStart);

                        if (selEndX > selStartX)
                            renderer.DrawRectangleFilled(selStartX, lineY, selEndX - selStartX, LineHeight, SelectionColor);
                    }
                }

                if (!string.IsNullOrEmpty(line))
                {
                    var activeColor = Enabled ? TextColor : new Color(100, 100, 100);
                    renderer.DrawText(line, textX, lineY, new TextRenderOptions { Color = activeColor, Font = Font });
                }

                // Advance charIndex past the line text and its newline (except last line)
                charIndex += line.Length + (lineIndex < lines.Length - 1 ? 1 : 0);
            }

            // Draw cursor
            if (IsFocused && _cursorVisible)
            {
                var (cursorLine, cursorCol) = GetLineAndColumn(_cursorPosition);
                float cursorLineY = Position.Y + TextPadding + cursorLine * LineHeight - _verticalScrollOffset;
                string cursorLineText = lines[cursorLine];
                float cursorX = Position.X + TextPadding + MeasureLinePrefix(renderer, cursorLineText, cursorCol);
                renderer.DrawRectangleFilled(cursorX, cursorLineY, 2f, LineHeight, TextColor);
            }
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

    internal void SetFocused(bool focused, IInputContext input)
    {
        SetFocused(focused, input, null, null);
    }

    internal void SetFocused(bool focused, IInputContext input, float? clickX, float? clickY)
    {
        if (IsFocused == focused) return;

        IsFocused = focused;
        _cursorBlinkTime = 0;
        _cursorVisible = true;

        if (focused)
        {
            if (clickX.HasValue && clickY.HasValue)
            {
                _pendingClickX = clickX;
                _pendingClickY = clickY;
            }
            else
            {
                _cursorPosition = _text.Length;
            }
            input.StartTextInput();
            OnFocusGained?.Invoke();
        }
        else
        {
            _pendingClickX = null;
            _pendingClickY = null;
            _verticalScrollOffset = 0f;
            input.StopTextInput();
            ClearSelection();
            OnFocusLost?.Invoke();
        }
    }

    internal void StartMouseDrag(float screenX, float screenY)
    {
        if (!IsFocused || !Enabled) return;
        _isMouseDragging = true;
        ClearSelection();
        _pendingClickX = screenX;
        _pendingClickY = screenY;
    }

    internal void UpdateMouseDrag(float screenX, float screenY)
    {
        if (!_isMouseDragging || !IsFocused || !Enabled) return;
        _pendingDragX = screenX;
        _pendingDragY = screenY;
    }

    internal void EndMouseDrag()
    {
        _isMouseDragging = false;
    }

    internal void HandleScroll(float scrollDelta)
    {
        _verticalScrollOffset -= scrollDelta * 20f;
    }

    /// <summary>
    /// Inserts a tab character ('\t') at the current cursor position.
    /// Called by <see cref="UICanvas"/> when <see cref="TabInsertsTab"/> is true and Tab is pressed.
    /// No-op when <see cref="ReadOnly"/> is true.
    /// </summary>
    internal void InsertTab()
    {
        if (ReadOnly || !Enabled) return;
        if (MaxLength > 0 && _text.Length >= MaxLength) return;
        PushUndoState();
        if (HasSelection())
        {
            var (s, e) = GetSelectionRange();
            _text = _text.Remove(s, e - s);
            _cursorPosition = s;
            ClearSelection();
        }
        _text = _text.Insert(_cursorPosition, "\t");
        _cursorPosition++;
        OnTextChanged?.Invoke(_text);
    }

    internal void HandleTextInput(IInputContext input)
    {
        if (!IsFocused || !Enabled) return;

        _cursorBlinkTime = 0;
        _cursorVisible = true;

        bool ctrl = input.IsKeyDown(Key.LeftControl) || input.IsKeyDown(Key.RightControl);
        bool shift = input.IsKeyDown(Key.LeftShift) || input.IsKeyDown(Key.RightShift);

        if (ctrl && input.IsKeyPressed(Key.A))
        {
            if (_text.Length > 0)
            {
                _selectionAnchor = 0;
                _selectionActive = _text.Length;
                _cursorPosition = _text.Length;
            }
            return;
        }

        if (ctrl && input.IsKeyPressed(Key.C))
        {
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                input.SetClipboardText(_text[s..e]);
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
                input.SetClipboardText(_text[s..e]);
                _text = _text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
                OnTextChanged?.Invoke(_text);
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
                    _text = _text.Remove(s, e - s);
                    _cursorPosition = s;
                    ClearSelection();
                }
                foreach (char c in paste)
                {
                    if ((MaxLength == 0 || _text.Length < MaxLength) && (CharacterFilter == null || CharacterFilter(c)))
                    {
                        _text = _text.Insert(_cursorPosition, c.ToString());
                        _cursorPosition++;
                    }
                }
                OnTextChanged?.Invoke(_text);
            }
            return;
        }

        if (ctrl && input.IsKeyPressed(Key.Z)) { Undo(); return; }
        if (ctrl && input.IsKeyPressed(Key.Y)) { Redo(); return; }

        if (input.IsKeyPressed(Key.Left))
        {
            if (ctrl)
            {
                int dest = FindWordBoundaryBackward(_cursorPosition);
                if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition = dest; _selectionActive = _cursorPosition; }
                else { _cursorPosition = dest; ClearSelection(); }
            }
            else if (_cursorPosition > 0)
            {
                if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition--; _selectionActive = _cursorPosition; }
                else { _cursorPosition--; ClearSelection(); }
            }
            return;
        }

        if (input.IsKeyPressed(Key.Right))
        {
            if (ctrl)
            {
                int dest = FindWordBoundaryForward(_cursorPosition);
                if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition = dest; _selectionActive = _cursorPosition; }
                else { _cursorPosition = dest; ClearSelection(); }
            }
            else if (_cursorPosition < _text.Length)
            {
                if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition++; _selectionActive = _cursorPosition; }
                else { _cursorPosition++; ClearSelection(); }
            }
            return;
        }

        if (input.IsKeyPressed(Key.Up))
        {
            int dest = MoveCursorVertically(_cursorPosition, -1);
            if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition = dest; _selectionActive = _cursorPosition; }
            else { _cursorPosition = dest; ClearSelection(); }
            return;
        }

        if (input.IsKeyPressed(Key.Down))
        {
            int dest = MoveCursorVertically(_cursorPosition, 1);
            if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition = dest; _selectionActive = _cursorPosition; }
            else { _cursorPosition = dest; ClearSelection(); }
            return;
        }

        if (input.IsKeyPressed(Key.Home))
        {
            int dest = ctrl ? 0 : GetLineStart(_cursorPosition);
            if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition = dest; _selectionActive = _cursorPosition; }
            else { _cursorPosition = dest; ClearSelection(); }
            return;
        }

        if (input.IsKeyPressed(Key.End))
        {
            int dest = ctrl ? _text.Length : GetLineEnd(_cursorPosition);
            if (shift) { if (!HasSelection()) StartSelectionAt(_cursorPosition); _cursorPosition = dest; _selectionActive = _cursorPosition; }
            else { _cursorPosition = dest; ClearSelection(); }
            return;
        }

        if (input.IsBackspacePressed())
        {
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                PushUndoState();
                _text = _text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
                OnTextChanged?.Invoke(_text);
            }
            else if (_cursorPosition > 0)
            {
                PushUndoState();
                _text = _text.Remove(_cursorPosition - 1, 1);
                _cursorPosition--;
                OnTextChanged?.Invoke(_text);
            }
            return;
        }

        if (input.IsDeletePressed())
        {
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                PushUndoState();
                _text = _text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
                OnTextChanged?.Invoke(_text);
            }
            else if (_cursorPosition < _text.Length)
            {
                PushUndoState();
                _text = _text.Remove(_cursorPosition, 1);
                OnTextChanged?.Invoke(_text);
            }
            return;
        }

        if (input.IsReturnPressed())
        {
            if (MaxLength == 0 || _text.Length < MaxLength)
            {
                PushUndoState();
                if (HasSelection())
                {
                    var (s, e) = GetSelectionRange();
                    _text = _text.Remove(s, e - s);
                    _cursorPosition = s;
                    ClearSelection();
                }
                _text = _text.Insert(_cursorPosition, "\n");
                _cursorPosition++;
                OnTextChanged?.Invoke(_text);
            }
            return;
        }

        var textInput = input.GetTextInput();
        if (!string.IsNullOrEmpty(textInput))
        {
            PushUndoState();
            if (HasSelection())
            {
                var (s, e) = GetSelectionRange();
                _text = _text.Remove(s, e - s);
                _cursorPosition = s;
                ClearSelection();
            }
            foreach (char c in textInput)
            {
                if ((MaxLength == 0 || _text.Length < MaxLength) && (CharacterFilter == null || CharacterFilter(c)))
                {
                    _text = _text.Insert(_cursorPosition, c.ToString());
                    _cursorPosition++;
                }
            }
            OnTextChanged?.Invoke(_text);
        }
    }

    private int HitTestCharacterIndex(IRenderer renderer, float screenX, float screenY)
    {
        var lines = _text.Split('\n');
        float contentTop = Position.Y + TextPadding - _verticalScrollOffset;

        int clickedLine = Math.Clamp((int)((screenY - contentTop) / LineHeight), 0, lines.Length - 1);

        // Find the start of the clicked line in the flat text
        int lineStart = 0;
        for (int i = 0; i < clickedLine; i++)
            lineStart += lines[i].Length + 1;

        string line = lines[clickedLine];
        float textX = Position.X + TextPadding;
        float relativeX = screenX - textX;

        if (relativeX <= 0) return lineStart;

        for (int i = 1; i <= line.Length; i++)
        {
            float w = MeasureLinePrefix(renderer, line, i);
            float prev = MeasureLinePrefix(renderer, line, i - 1);
            if (relativeX < prev + (w - prev) / 2f)
                return lineStart + i - 1;
        }

        return lineStart + line.Length;
    }

    private float MeasureLinePrefix(IRenderer renderer, string line, int charCount)
    {
        if (charCount <= 0 || string.IsNullOrEmpty(line)) return 0f;
        var prefix = line[..Math.Min(charCount, line.Length)];
        return renderer.MeasureText(prefix, new TextRenderOptions { Font = Font }).X;
    }

    private (int line, int col) GetLineAndColumn(int position)
    {
        var lines = _text.Split('\n');
        int charCount = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int lineEnd = charCount + lines[i].Length;
            if (position <= lineEnd || i == lines.Length - 1)
                return (i, position - charCount);
            charCount += lines[i].Length + 1;
        }
        return (0, 0);
    }

    private int GetLineStart(int position)
    {
        for (int i = position - 1; i >= 0; i--)
        {
            if (_text[i] == '\n') return i + 1;
        }
        return 0;
    }

    private int GetLineEnd(int position)
    {
        for (int i = position; i < _text.Length; i++)
        {
            if (_text[i] == '\n') return i;
        }
        return _text.Length;
    }

    private int MoveCursorVertically(int position, int lineDelta)
    {
        var lines = _text.Split('\n');
        var (currentLine, currentCol) = GetLineAndColumn(position);
        int targetLine = Math.Clamp(currentLine + lineDelta, 0, lines.Length - 1);

        int targetLineStart = 0;
        for (int i = 0; i < targetLine; i++)
            targetLineStart += lines[i].Length + 1;

        int targetCol = Math.Min(currentCol, lines[targetLine].Length);
        return targetLineStart + targetCol;
    }

    private bool HasSelection() => _selectionAnchor >= 0 && _selectionActive >= 0 && _selectionAnchor != _selectionActive;

    private (int start, int end) GetSelectionRange()
    {
        int s = Math.Min(_selectionAnchor, _selectionActive);
        int e = Math.Max(_selectionAnchor, _selectionActive);
        return (s, e);
    }

    private void StartSelectionAt(int position)
    {
        _selectionAnchor = position;
        _selectionActive = position;
    }

    private void ClearSelection()
    {
        _selectionAnchor = -1;
        _selectionActive = -1;
    }

    private int FindWordBoundaryBackward(int position)
    {
        if (position <= 0) return 0;
        int i = position - 1;
        while (i > 0 && char.IsWhiteSpace(_text[i - 1])) i--;
        while (i > 0 && !char.IsWhiteSpace(_text[i - 1])) i--;
        return i;
    }

    private int FindWordBoundaryForward(int position)
    {
        int i = position;
        while (i < _text.Length && !char.IsWhiteSpace(_text[i])) i++;
        while (i < _text.Length && char.IsWhiteSpace(_text[i])) i++;
        return i;
    }

    private void PushUndoState()
    {
        _undoStack.Push((_text, _cursorPosition));
        _redoStack.Clear();

        if (UndoStackLimit > 0 && _undoStack.Count > UndoStackLimit)
        {
            var entries = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = entries.Length - 2; i >= 0; i--)
                _undoStack.Push(entries[i]);
        }
    }

    internal void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push((_text, _cursorPosition));
        var (text, cursor) = _undoStack.Pop();
        _text = text;
        _cursorPosition = cursor;
        ClearSelection();
        OnTextChanged?.Invoke(_text);
    }

    internal void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push((_text, _cursorPosition));
        var (text, cursor) = _redoStack.Pop();
        _text = text;
        _cursorPosition = cursor;
        ClearSelection();
        OnTextChanged?.Invoke(_text);
    }
}
