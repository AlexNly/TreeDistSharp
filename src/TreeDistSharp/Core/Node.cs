namespace TreeDistSharp.Core;

/// <summary>
/// Represents a node in a phylogenetic tree.
/// </summary>
public sealed class Node
{
    private readonly List<Node> _children;

    /// <summary>
    /// Optional label for this node (tip name or internal node label).
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Child nodes. Empty for leaf nodes.
    /// </summary>
    public IReadOnlyList<Node> Children => _children;

    /// <summary>
    /// Whether this is a leaf (tip) node.
    /// </summary>
    public bool IsLeaf => _children.Count == 0;

    /// <summary>
    /// Index of this tip in the tip label array. -1 for internal nodes.
    /// </summary>
    public int TipIndex { get; internal set; } = -1;

    /// <summary>
    /// Create a leaf node with the given label.
    /// </summary>
    public Node(string label)
    {
        Label = label;
        _children = new List<Node>();
    }

    /// <summary>
    /// Create an internal node with children and optional label.
    /// </summary>
    public Node(IEnumerable<Node> children, string? label = null)
    {
        Label = label;
        _children = new List<Node>(children);
    }

    /// <summary>
    /// Add a child to this node.
    /// </summary>
    internal void AddChild(Node child)
    {
        _children.Add(child);
    }
}
