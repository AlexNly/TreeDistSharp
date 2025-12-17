using TreeDistSharp.Core;
using TreeDistSharp.Math;

namespace TreeDistSharp.Algorithms;

/// <summary>
/// Robinson-Foulds tree distance calculations.
/// Based on TreeDist/src/tree_distances.cpp by Martin R. Smith.
/// </summary>
public static class RobinsonFoulds
{
    /// <summary>
    /// Calculate the Robinson-Foulds distance between two trees.
    /// RF = number of splits in A not in B + number of splits in B not in A.
    /// </summary>
    /// <param name="a">First tree's splits</param>
    /// <param name="b">Second tree's splits</param>
    /// <returns>RF distance (non-negative integer)</returns>
    public static int Distance(SplitList a, SplitList b)
    {
        if (a.TipCount != b.TipCount)
        {
            throw new ArgumentException("Trees must have the same number of tips");
        }

        // For RF, we work with unique bipartitions (complements are the same)
        int uniqueA = CountUniqueBipartitions(a);
        int uniqueB = CountUniqueBipartitions(b);
        int matchCount = CountMatches(a, b);

        // RF = total unique bipartitions - 2 * matches
        return uniqueA + uniqueB - 2 * matchCount;
    }

    /// <summary>
    /// Calculate the normalized Robinson-Foulds distance (0 to 1).
    /// </summary>
    public static double NormalizedDistance(SplitList a, SplitList b)
    {
        if (a.TipCount != b.TipCount)
        {
            throw new ArgumentException("Trees must have the same number of tips");
        }

        int uniqueA = CountUniqueBipartitions(a);
        int uniqueB = CountUniqueBipartitions(b);
        int rf = Distance(a, b);
        int maxPossible = uniqueA + uniqueB;

        return maxPossible > 0 ? (double)rf / maxPossible : 0.0;
    }

    /// <summary>
    /// Calculate information-weighted Robinson-Foulds distance.
    /// Splits are weighted by their phylogenetic information content.
    /// </summary>
    public static double InfoDistance(SplitList a, SplitList b)
    {
        if (a.TipCount != b.TipCount)
        {
            throw new ArgumentException("Trees must have the same number of tips");
        }

        int nTips = a.TipCount;
        double lg2UnrootedN = InfoTables.Log2Unrooted(nTips);
        double matchedInfo = 0;

        // For each split in A, find matching split in B
        for (int ai = 0; ai < a.Count; ai++)
        {
            ref readonly var splitA = ref a[ai];

            for (int bi = 0; bi < b.Count; bi++)
            {
                ref readonly var splitB = ref b[bi];

                if (splitA.EqualsOrComplement(splitB))
                {
                    // Information content of the matching split
                    int leavesInSplit = splitA.LeafCount;
                    matchedInfo += lg2UnrootedN
                        - InfoTables.Log2Rooted(leavesInSplit)
                        - InfoTables.Log2Rooted(nTips - leavesInSplit);
                    break; // Only one match possible per split
                }
            }
        }

        // Total info in both trees minus twice the matched info
        double totalInfoA = a.TotalPhylogeneticInfo();
        double totalInfoB = b.TotalPhylogeneticInfo();

        return totalInfoA + totalInfoB - 2 * matchedInfo;
    }

    /// <summary>
    /// Calculate normalized information-weighted Robinson-Foulds distance (0 to 1).
    /// </summary>
    public static double NormalizedInfoDistance(SplitList a, SplitList b)
    {
        double infoDistance = InfoDistance(a, b);
        double totalInfo = a.TotalPhylogeneticInfo() + b.TotalPhylogeneticInfo();

        return totalInfo > 0 ? infoDistance / totalInfo : 0.0;
    }

    /// <summary>
    /// Get the number of unique bipartitions in a split list.
    /// Complements are considered the same bipartition.
    /// </summary>
    private static int CountUniqueBipartitions(SplitList splits)
    {
        var unique = new List<Split>();
        for (int i = 0; i < splits.Count; i++)
        {
            ref readonly var split = ref splits[i];
            bool isDuplicate = false;
            foreach (var existing in unique)
            {
                if (split.EqualsOrComplement(existing))
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
            {
                unique.Add(split);
            }
        }
        return unique.Count;
    }

    /// <summary>
    /// Count the number of matching bipartitions between two trees.
    /// Each bipartition in A is matched with at most one in B.
    /// Complements are considered the same bipartition.
    /// </summary>
    private static int CountMatches(SplitList a, SplitList b)
    {
        // First, reduce both lists to unique bipartitions
        var uniqueA = new List<Split>();
        for (int i = 0; i < a.Count; i++)
        {
            ref readonly var split = ref a[i];
            bool isDuplicate = false;
            foreach (var existing in uniqueA)
            {
                if (split.EqualsOrComplement(existing))
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
            {
                uniqueA.Add(split);
            }
        }

        var uniqueB = new List<Split>();
        for (int i = 0; i < b.Count; i++)
        {
            ref readonly var split = ref b[i];
            bool isDuplicate = false;
            foreach (var existing in uniqueB)
            {
                if (split.EqualsOrComplement(existing))
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
            {
                uniqueB.Add(split);
            }
        }

        // Count matches between unique bipartitions
        int matchCount = 0;
        var matchedB = new bool[uniqueB.Count];

        for (int ai = 0; ai < uniqueA.Count; ai++)
        {
            var splitA = uniqueA[ai];

            for (int bi = 0; bi < uniqueB.Count; bi++)
            {
                if (matchedB[bi]) continue;

                var splitB = uniqueB[bi];

                if (splitA.EqualsOrComplement(splitB))
                {
                    matchCount++;
                    matchedB[bi] = true;
                    break;
                }
            }
        }

        return matchCount;
    }

    /// <summary>
    /// Get detailed matching information between two trees.
    /// </summary>
    public static (int rfDistance, int[] matching) DistanceWithMatching(SplitList a, SplitList b)
    {
        if (a.TipCount != b.TipCount)
        {
            throw new ArgumentException("Trees must have the same number of tips");
        }

        var matching = new int[a.Count];
        Array.Fill(matching, -1); // -1 = no match

        int matchCount = 0;
        var matchedB = new bool[b.Count];

        for (int ai = 0; ai < a.Count; ai++)
        {
            ref readonly var splitA = ref a[ai];

            for (int bi = 0; bi < b.Count; bi++)
            {
                if (matchedB[bi]) continue;

                ref readonly var splitB = ref b[bi];

                if (splitA.EqualsOrComplement(splitB))
                {
                    matching[ai] = bi;
                    matchedB[bi] = true;
                    matchCount++;
                    break;
                }
            }
        }

        int rf = a.Count + b.Count - 2 * matchCount;
        return (rf, matching);
    }
}
