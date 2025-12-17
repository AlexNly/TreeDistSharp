namespace TreeDistSharp.Core;

/// <summary>
/// Represents a phylogenetic tree.
/// </summary>
public sealed class Tree
{
    /// <summary>
    /// Root node of the tree.
    /// </summary>
    public Node Root { get; }

    /// <summary>
    /// Ordered list of tip (leaf) labels.
    /// </summary>
    public IReadOnlyList<string> TipLabels { get; }

    /// <summary>
    /// Number of tips (leaves) in the tree.
    /// </summary>
    public int TipCount => TipLabels.Count;

    /// <summary>
    /// Create a tree from a root node.
    /// Automatically extracts tip labels and assigns tip indices.
    /// </summary>
    public Tree(Node root)
    {
        Root = root;
        var tipLabels = new List<string>();
        AssignTipIndices(root, tipLabels);
        TipLabels = tipLabels;
    }

    /// <summary>
    /// Create a tree from a root node with pre-ordered tip labels.
    /// </summary>
    public Tree(Node root, IReadOnlyList<string> tipLabels)
    {
        Root = root;
        TipLabels = tipLabels;

        // Build label to index mapping
        var labelToIndex = new Dictionary<string, int>();
        for (int i = 0; i < tipLabels.Count; i++)
        {
            labelToIndex[tipLabels[i]] = i;
        }

        // Assign tip indices based on provided order
        AssignTipIndicesFromMapping(root, labelToIndex);
    }

    private static void AssignTipIndices(Node node, List<string> tipLabels)
    {
        if (node.IsLeaf)
        {
            node.TipIndex = tipLabels.Count;
            tipLabels.Add(node.Label ?? $"tip_{tipLabels.Count}");
        }
        else
        {
            foreach (var child in node.Children)
            {
                AssignTipIndices(child, tipLabels);
            }
        }
    }

    private static void AssignTipIndicesFromMapping(Node node, Dictionary<string, int> labelToIndex)
    {
        if (node.IsLeaf)
        {
            if (node.Label != null && labelToIndex.TryGetValue(node.Label, out int index))
            {
                node.TipIndex = index;
            }
            else
            {
                throw new ArgumentException($"Tip label '{node.Label}' not found in provided tip labels");
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                AssignTipIndicesFromMapping(child, labelToIndex);
            }
        }
    }

    /// <summary>
    /// Get the index of a tip label.
    /// </summary>
    public int GetTipIndex(string label)
    {
        for (int i = 0; i < TipLabels.Count; i++)
        {
            if (TipLabels[i] == label)
            {
                return i;
            }
        }
        return -1;
    }
}
