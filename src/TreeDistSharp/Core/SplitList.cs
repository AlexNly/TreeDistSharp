using System.Numerics;

namespace TreeDistSharp.Core;

/// <summary>
/// Collection of non-trivial splits extracted from a phylogenetic tree.
/// Trivial splits (single leaf or all-but-one leaf) are excluded.
/// </summary>
public sealed class SplitList
{
    private readonly Split[] _splits;
    private readonly int _nTips;

    /// <summary>
    /// Number of (non-trivial) splits.
    /// </summary>
    public int Count => _splits.Length;

    /// <summary>
    /// Number of tips in the tree.
    /// </summary>
    public int TipCount => _nTips;

    /// <summary>
    /// Access a split by index.
    /// </summary>
    public ref readonly Split this[int index] => ref _splits[index];

    /// <summary>
    /// Get all splits as a span.
    /// </summary>
    public ReadOnlySpan<Split> Splits => _splits;

    private SplitList(Split[] splits, int nTips)
    {
        _splits = splits;
        _nTips = nTips;
    }

    /// <summary>
    /// Extract splits from a tree.
    /// </summary>
    public static SplitList FromTree(Tree tree)
    {
        int nTips = tree.TipCount;
        int nBins = Split.BinsRequired(nTips);

        var splits = new List<Split>();
        var rootChildSplits = new List<Split>(); // Splits from root's direct children

        // Determine if root is bifurcating (exactly 2 children)
        bool bifurcatingRoot = tree.Root.Children.Count == 2;

        // Post-order traversal to build splits
        // Each internal node defines a split: the tips in its subtree vs the rest
        foreach (var child in tree.Root.Children)
        {
            var childBins = BuildSplits(child, nTips, nBins, splits, isRootChild: true, rootChildSplits);
        }

        // For bifurcating root, deduplicate only the root's direct children's splits
        // (they are complements of each other)
        if (bifurcatingRoot && rootChildSplits.Count == 2)
        {
            // Keep only the first one, remove the second (its complement)
            if (rootChildSplits[0].EqualsOrComplement(rootChildSplits[1]))
            {
                splits.Remove(rootChildSplits[1]);
            }
        }

        return new SplitList(splits.ToArray(), nTips);
    }

    private static ulong[] BuildSplits(Node node, int nTips, int nBins, List<Split> splits,
        bool isRootChild = false, List<Split>? rootChildSplits = null)
    {
        if (node.IsLeaf)
        {
            // Create a split with just this tip
            var bins = new ulong[nBins];
            int tipIndex = node.TipIndex;
            int binIndex = tipIndex / Split.BitsPerBin;
            int bitIndex = tipIndex % Split.BitsPerBin;
            bins[binIndex] |= 1UL << bitIndex;
            return bins;
        }

        // Internal node: merge child splits
        var mergedBins = new ulong[nBins];

        foreach (var child in node.Children)
        {
            // Children of this node are NOT root children
            var childBins = BuildSplits(child, nTips, nBins, splits, isRootChild: false, rootChildSplits: null);
            for (int i = 0; i < nBins; i++)
            {
                mergedBins[i] |= childBins[i];
            }
        }

        // Count tips in this subtree
        int tipCount = 0;
        for (int i = 0; i < nBins; i++)
        {
            tipCount += BitOperations.PopCount(mergedBins[i]);
        }

        // For rooted trees (like R's TreeDist), we include all internal node splits
        // A split is trivial only if it has 0 or 1 tip on either side
        // Non-trivial: tipCount >= 2 AND complementSize >= 2
        int complementSize = nTips - tipCount;

        if (tipCount >= 2 && complementSize >= 2)
        {
            // Store the subtree as-is (no canonicalization to smaller half)
            // R's TreeDist keeps splits in their original form
            var binsCopy = new ulong[nBins];
            Array.Copy(mergedBins, binsCopy, nBins);
            var split = new Split(binsCopy, nTips, tipCount);
            splits.Add(split);

            // Track splits from root's direct children for deduplication
            if (isRootChild && rootChildSplits != null)
            {
                rootChildSplits.Add(split);
            }
        }

        return mergedBins;
    }

    /// <summary>
    /// Get the entropy of all splits (sum of split entropies).
    /// </summary>
    public double TotalClusteringEntropy()
    {
        double total = 0;
        foreach (ref readonly var split in Splits)
        {
            total += Math.InfoTables.SplitClusteringEntropy(split.LeafCount, _nTips);
        }
        return total;
    }

    /// <summary>
    /// Get the total phylogenetic information content of all splits.
    /// </summary>
    public double TotalPhylogeneticInfo()
    {
        double total = 0;
        foreach (ref readonly var split in Splits)
        {
            total += Math.InfoTables.SplitPhylogeneticInfo(split.LeafCount, _nTips);
        }
        return total;
    }
}
