using TreeDistSharp.Algorithms;
using TreeDistSharp.Core;
using TreeDistSharp.Parsing;

namespace TreeDistSharp;

/// <summary>
/// Main entry point for calculating phylogenetic tree distances.
///
/// This is a C# port of the R TreeDist package by Martin R. Smith.
/// Reference: Smith (2020) "Information theoretic Generalized Robinson-Foulds
/// metrics for comparing phylogenetic trees" Bioinformatics 36: 5007-5013.
/// </summary>
public static class TreeDistCalculator
{
    /// <summary>
    /// Calculate the normalized Clustering Information Distance between two trees.
    /// This is equivalent to TreeDistance() in the R package.
    /// </summary>
    /// <param name="newick1">First tree in Newick format</param>
    /// <param name="newick2">Second tree in Newick format</param>
    /// <returns>Normalized distance (0 = identical topology, approaches 1 for very different)</returns>
    public static double TreeDistance(string newick1, string newick2)
    {
        var tree1 = NewickParser.Parse(newick1);
        var tree2 = NewickParser.Parse(newick2, tree1.TipLabels);

        return TreeDistance(tree1, tree2);
    }

    /// <summary>
    /// Calculate the normalized Clustering Information Distance between two trees.
    /// </summary>
    public static double TreeDistance(Tree tree1, Tree tree2)
    {
        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        return TreeDistance(splits1, splits2);
    }

    /// <summary>
    /// Calculate TreeDistance from pre-parsed split lists.
    /// Useful for batch comparisons to avoid re-parsing.
    /// </summary>
    public static double TreeDistance(SplitList splits1, SplitList splits2)
    {
        if (splits1.TipCount != splits2.TipCount)
        {
            throw new ArgumentException("Trees must have the same number of tips");
        }

        return ClusteringInfo.ClusteringInfoDistance(splits1, splits2, splits1.TipCount, normalize: true);
    }

    /// <summary>
    /// Calculate the Robinson-Foulds distance between two trees.
    /// </summary>
    /// <param name="newick1">First tree in Newick format</param>
    /// <param name="newick2">Second tree in Newick format</param>
    /// <param name="normalize">If true, normalize to 0-1 range</param>
    /// <returns>RF distance</returns>
    public static double RobinsonFoulds(string newick1, string newick2, bool normalize = false)
    {
        var tree1 = NewickParser.Parse(newick1);
        var tree2 = NewickParser.Parse(newick2, tree1.TipLabels);

        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        return normalize
            ? Algorithms.RobinsonFoulds.NormalizedDistance(splits1, splits2)
            : Algorithms.RobinsonFoulds.Distance(splits1, splits2);
    }

    /// <summary>
    /// Calculate Robinson-Foulds distance from pre-parsed split lists.
    /// </summary>
    public static double RobinsonFoulds(SplitList splits1, SplitList splits2, bool normalize = false)
    {
        return normalize
            ? Algorithms.RobinsonFoulds.NormalizedDistance(splits1, splits2)
            : Algorithms.RobinsonFoulds.Distance(splits1, splits2);
    }

    /// <summary>
    /// Calculate information-weighted Robinson-Foulds distance.
    /// </summary>
    /// <param name="newick1">First tree in Newick format</param>
    /// <param name="newick2">Second tree in Newick format</param>
    /// <param name="normalize">If true, normalize by total information content</param>
    public static double InfoRobinsonFoulds(string newick1, string newick2, bool normalize = false)
    {
        var tree1 = NewickParser.Parse(newick1);
        var tree2 = NewickParser.Parse(newick2, tree1.TipLabels);

        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        return normalize
            ? Algorithms.RobinsonFoulds.NormalizedInfoDistance(splits1, splits2)
            : Algorithms.RobinsonFoulds.InfoDistance(splits1, splits2);
    }

    /// <summary>
    /// Parse a Newick string into a Tree.
    /// </summary>
    public static Tree ParseTree(string newick)
    {
        return NewickParser.Parse(newick);
    }

    /// <summary>
    /// Parse a Newick string into a Tree, using the specified tip label order.
    /// </summary>
    public static Tree ParseTree(string newick, IReadOnlyList<string> tipLabels)
    {
        return NewickParser.Parse(newick, tipLabels);
    }

    /// <summary>
    /// Extract splits from a tree (useful for batch distance calculations).
    /// </summary>
    public static SplitList GetSplits(Tree tree)
    {
        return SplitList.FromTree(tree);
    }

    /// <summary>
    /// Calculate a pairwise distance matrix for a collection of trees.
    /// </summary>
    /// <param name="newicks">Collection of trees in Newick format</param>
    /// <param name="metric">Distance metric to use</param>
    /// <returns>Symmetric distance matrix</returns>
    public static double[,] PairwiseDistances(IReadOnlyList<string> newicks, DistanceMetric metric = DistanceMetric.TreeDistance)
    {
        int n = newicks.Count;
        if (n == 0) return new double[0, 0];

        // Parse first tree to get tip labels
        var firstTree = NewickParser.Parse(newicks[0]);
        var tipLabels = firstTree.TipLabels;

        // Parse all trees with consistent tip ordering
        var splits = new SplitList[n];
        splits[0] = SplitList.FromTree(firstTree);

        for (int i = 1; i < n; i++)
        {
            var tree = NewickParser.Parse(newicks[i], tipLabels);
            splits[i] = SplitList.FromTree(tree);
        }

        // Calculate pairwise distances
        var distances = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                double dist = metric switch
                {
                    DistanceMetric.TreeDistance => TreeDistance(splits[i], splits[j]),
                    DistanceMetric.RobinsonFoulds => Algorithms.RobinsonFoulds.Distance(splits[i], splits[j]),
                    DistanceMetric.NormalizedRobinsonFoulds => Algorithms.RobinsonFoulds.NormalizedDistance(splits[i], splits[j]),
                    DistanceMetric.InfoRobinsonFoulds => Algorithms.RobinsonFoulds.InfoDistance(splits[i], splits[j]),
                    _ => throw new ArgumentException($"Unknown metric: {metric}")
                };

                distances[i, j] = dist;
                distances[j, i] = dist; // Symmetric
            }
        }

        return distances;
    }
}

/// <summary>
/// Available distance metrics for tree comparison.
/// </summary>
public enum DistanceMetric
{
    /// <summary>
    /// Normalized Clustering Information Distance (recommended).
    /// </summary>
    TreeDistance,

    /// <summary>
    /// Standard Robinson-Foulds distance (count of differing splits).
    /// </summary>
    RobinsonFoulds,

    /// <summary>
    /// Robinson-Foulds distance normalized to 0-1 range.
    /// </summary>
    NormalizedRobinsonFoulds,

    /// <summary>
    /// Information-weighted Robinson-Foulds distance.
    /// </summary>
    InfoRobinsonFoulds
}
