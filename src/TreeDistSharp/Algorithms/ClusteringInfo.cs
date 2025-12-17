using TreeDistSharp.Core;
using TreeDistSharp.Math;

namespace TreeDistSharp.Algorithms;

/// <summary>
/// Mutual Clustering Information calculations for tree distance.
/// Based on TreeDist/src/tree_distances.cpp by Martin R. Smith.
/// Implements Meila (2007) variation of information.
/// </summary>
public static class ClusteringInfo
{
    private const long MaxScore = long.MaxValue / 8192;

    /// <summary>
    /// Calculate mutual clustering information between two trees' split lists.
    /// Uses optimal matching via Hungarian algorithm.
    /// </summary>
    /// <returns>Mutual clustering information (in bits)</returns>
    public static double MutualClusteringInfo(SplitList a, SplitList b, int nTips)
    {
        if (a.TipCount != nTips || b.TipCount != nTips)
        {
            throw new ArgumentException("Split lists must have the same number of tips");
        }

        if (a.Count == 0 || b.Count == 0 || nTips == 0)
        {
            return 0;
        }

        double nTipsReciprocal = 1.0 / nTips;
        double maxOverTips = (double)MaxScore * nTipsReciprocal;

        // Track exact matches
        var aMatch = new bool[a.Count];
        var bMatch = new bool[b.Count];
        double exactMatchScore = 0;
        int exactMatches = 0;

        // First pass: build full cost matrix for all pairs
        var costMatrix = new long[a.Count, b.Count];

        for (int ai = 0; ai < a.Count; ai++)
        {
            ref readonly var splitA = ref a[ai];
            int na = splitA.LeafCount;
            int nA = nTips - na;

            for (int bi = 0; bi < b.Count; bi++)
            {
                ref readonly var splitB = ref b[bi];
                int nb = splitB.LeafCount;
                int nB = nTips - nb;

                // Count overlap: a AND b
                int aAndB = splitA.CountOverlap(splitB);
                int aAndNotB = na - aAndB;  // a AND B (complement)
                int notAAndB = nb - aAndB;  // A AND b
                int notAAndNotB = nA - notAAndB;  // A AND B

                if (aAndB == notAAndB && aAndB == aAndNotB && aAndB == notAAndNotB)
                {
                    // Equal distribution - maximum cost to avoid rounding errors
                    costMatrix[ai, bi] = MaxScore;
                }
                else
                {
                    // Calculate mutual information elements
                    double icSum = 0;
                    AddIcElement(ref icSum, aAndB, na, nb, nTips);
                    AddIcElement(ref icSum, aAndNotB, na, nB, nTips);
                    AddIcElement(ref icSum, notAAndB, nA, nb, nTips);
                    AddIcElement(ref icSum, notAAndNotB, nA, nB, nTips);

                    // Convert to cost (lower is better for assignment)
                    costMatrix[ai, bi] = MaxScore - (long)(icSum * maxOverTips);
                }
            }
        }

        // Second pass: find exact matches
        // NOTE: Like R's implementation, we DON'T check if bi is already matched.
        // This allows multiple A splits to match the same B split, which is correct
        // when splits are duplicates or complements across trees.
        for (int ai = 0; ai < a.Count; ai++)
        {
            if (aMatch[ai]) continue;

            ref readonly var splitA = ref a[ai];
            int na = splitA.LeafCount;
            int nA = nTips - na;

            for (int bi = 0; bi < b.Count; bi++)
            {
                ref readonly var splitB = ref b[bi];
                int nb = splitB.LeafCount;
                int nB = nTips - nb;

                // Count overlap: a AND b
                int aAndB = splitA.CountOverlap(splitB);
                int aAndNotB = na - aAndB;
                int notAAndB = nb - aAndB;
                int notAAndNotB = nA - notAAndB;

                // Check for exact match (splits are equal or complementary)
                if ((aAndNotB == 0 && notAAndB == 0) || (aAndB == 0 && notAAndNotB == 0))
                {
                    exactMatchScore += IcMatching(na, nA, nTips);
                    exactMatches++;
                    aMatch[ai] = true;
                    bMatch[bi] = true;
                    break;
                }
            }
        }

        // R uses: lap_dim = most_splits - exact_matches
        // This can result in some splits being excluded if duplicate matches occur
        int mostSplits = System.Math.Max(a.Count, b.Count);
        int lapDim = mostSplits - exactMatches;

        // If all splits are exactly matched, return early
        if (lapDim <= 0)
        {
            return exactMatchScore * nTipsReciprocal;
        }

        // Calculate extra splits (for padding when trees have different split counts)
        int aExtraSplits = a.Count > b.Count ? mostSplits - b.Count : 0;
        int bExtraSplits = a.Count <= b.Count ? mostSplits - a.Count : 0;

        // Build reduced matrix excluding exact matches
        var reducedCost = new long[lapDim, lapDim];

        // Initialize with MaxScore (for padding)
        for (int i = 0; i < lapDim; i++)
        {
            for (int j = 0; j < lapDim; j++)
            {
                reducedCost[i, j] = MaxScore;
            }
        }

        int aPos = 0;
        for (int ai = 0; ai < a.Count && aPos < lapDim; ai++)
        {
            if (aMatch[ai]) continue;

            int bPos = 0;
            for (int bi = 0; bi < b.Count && bPos < lapDim - aExtraSplits; bi++)
            {
                if (bMatch[bi]) continue;
                if (bPos < lapDim)
                {
                    reducedCost[aPos, bPos] = costMatrix[ai, bi];
                }
                bPos++;
            }
            aPos++;
        }

        // Solve assignment problem
        long lapCost = HungarianAlgorithm.Solve(reducedCost, out _);

        // Convert LAP result back to information score
        double lapScore = (double)((MaxScore * lapDim) - lapCost) / MaxScore;
        double finalScore = lapScore + (exactMatchScore / nTips);

        return finalScore;
    }

    /// <summary>
    /// Calculate clustering entropy for a single split.
    /// H(S) = (a + b) * log2(n) - a * log2(a) - b * log2(b)
    /// Equivalent to -(a/n)*log2(a/n) - (b/n)*log2(b/n) when divided by n.
    /// </summary>
    private static double IcMatching(int a, int b, int n)
    {
        double lg2a = InfoTables.Log2(a);
        double lg2b = InfoTables.Log2(b);
        double lg2n = InfoTables.Log2(n);
        return (a + b) * lg2n - a * lg2a - b * lg2b;
    }

    /// <summary>
    /// Add an IC element according to Meila (2007) equation 16.
    /// </summary>
    private static void AddIcElement(ref double icSum, int nkK, int nk, int nK, int nTips)
    {
        if (nkK != 0 && nk != 0 && nK != 0)
        {
            if (nkK == nk && nkK == nK && nkK * 2 == nTips)
            {
                icSum += nkK;
            }
            else
            {
                int numerator = nkK * nTips;
                int denominator = nk * nK;
                if (numerator != denominator)
                {
                    icSum += nkK * (InfoTables.Log2(numerator) - InfoTables.Log2(denominator));
                }
            }
        }
    }

    /// <summary>
    /// Calculate the normalized clustering information distance (TreeDistance).
    /// </summary>
    public static double ClusteringInfoDistance(SplitList a, SplitList b, int nTips, bool normalize = true)
    {
        double mci = MutualClusteringInfo(a, b, nTips);
        double entropyA = a.TotalClusteringEntropy();
        double entropyB = b.TotalClusteringEntropy();
        double totalEntropy = entropyA + entropyB;

        // Distance = total entropy - 2 * mutual info
        double distance = totalEntropy - 2 * mci;

        // Handle floating point errors
        if (distance < 1e-10)
        {
            distance = 0;
        }

        if (normalize && totalEntropy > 0)
        {
            return distance / totalEntropy;
        }

        return distance;
    }
}
