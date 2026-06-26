namespace Brine2D.UI;

/// <summary>
/// A single node in a <see cref="UITreeView"/> hierarchy.
/// Nodes can have children, be expanded/collapsed, and carry an arbitrary
/// <see cref="Tag"/> object for application data.
/// </summary>
public class UITreeNode
{
    private readonly List<UITreeNode> _children = new();

    /// <summary>Text displayed for this node in the tree view.</summary>
    public string Text { get; set; }

    /// <summary>Optional application-defined data associated with this node.</summary>
    public object? Tag { get; set; }

    /// <summary>Whether this node's children are currently visible.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>Read-only view of this node's children.</summary>
    public IReadOnlyList<UITreeNode> Children => _children;

    /// <summary>Returns <c>true</c> when this node has at least one child.</summary>
    public bool HasChildren => _children.Count > 0;

    /// <param name="text">Display text for the node.</param>
    /// <param name="tag">Optional application data.</param>
    /// <param name="isExpanded">Whether the node starts expanded. Defaults to <c>false</c>.</param>
    public UITreeNode(string text, object? tag = null, bool isExpanded = false)
    {
        Text = text;
        Tag = tag;
        IsExpanded = isExpanded;
    }

    /// <summary>
    /// Adds a child node with the specified <paramref name="text"/> and returns <c>this</c>
    /// to allow fluent chaining.
    /// </summary>
    public UITreeNode Add(string text, object? tag = null, bool isExpanded = false)
    {
        _children.Add(new UITreeNode(text, tag, isExpanded));
        return this;
    }

    /// <summary>Adds a pre-built <paramref name="child"/> node and returns <c>this</c>.</summary>
    public UITreeNode Add(UITreeNode child)
    {
        _children.Add(child);
        return this;
    }

    /// <summary>Removes a direct child. Returns <c>true</c> if the child was found and removed.</summary>
    public bool Remove(UITreeNode child) => _children.Remove(child);

    /// <summary>Removes all children from this node.</summary>
    public void ClearChildren() => _children.Clear();
}
