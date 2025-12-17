namespace TreeDistSharp.Math;

/// <summary>
/// Pre-computed lookup tables for information-theoretic calculations.
/// Based on TreeDist/src/information.h by Martin R. Smith.
/// </summary>
public static class InfoTables
{
    /// <summary>
    /// Maximum number of tree tips supported (matches R package CT_MAX_LEAVES).
    /// </summary>
    public const int MaxTips = 2048;

    private const int LogMax = 2048;

    // Pre-computed log2 values for integers 1 to LogMax-1
    private static readonly double[] Log2Table;

    // Log2 of double factorials: ldfact[n] = log2(n!!)
    // n!! = n * (n-2) * (n-4) * ... * (1 or 2)
    private static readonly double[] Log2DoubleFactorial;

    // Log2 of number of rooted binary trees with n tips: (2n-3)!!
    private static readonly double[] Log2RootedTrees;

    static InfoTables()
    {
        // Initialize log2 table
        Log2Table = new double[LogMax];
        for (int i = 1; i < LogMax; i++)
        {
            Log2Table[i] = System.Math.Log2(i);
        }

        // Initialize double factorial table
        // Size = MaxTips * 2 + 5 + 1 (matches R package FACT_MAX)
        int factMax = MaxTips * 2 + 6;
        Log2DoubleFactorial = new double[factMax];
        Log2DoubleFactorial[0] = 0;
        Log2DoubleFactorial[1] = 0;

        for (int i = 2; i < factMax; i++)
        {
            Log2DoubleFactorial[i] = Log2DoubleFactorial[i - 2] + System.Math.Log2(i);
        }

        // Initialize rooted tree count table
        // l2rooted[n] = log2((2n-3)!!) for n >= 3
        Log2RootedTrees = new double[MaxTips];
        Log2RootedTrees[0] = 0;
        Log2RootedTrees[1] = 0;
        Log2RootedTrees[2] = 0;

        for (int i = 3; i < MaxTips; i++)
        {
            // (2i - 3) = 2*i - 3
            Log2RootedTrees[i] = Log2DoubleFactorial[2 * i - 3];
        }
    }

    /// <summary>
    /// Fast log2 lookup for small integers.
    /// </summary>
    public static double Log2(int n)
    {
        if (n > 0 && n < LogMax)
        {
            return Log2Table[n];
        }
        return System.Math.Log2(n);
    }

    /// <summary>
    /// Log2 of double factorial: log2(n!!)
    /// </summary>
    public static double Log2DoubleFactorialOf(int n)
    {
        if (n >= 0 && n < Log2DoubleFactorial.Length)
        {
            return Log2DoubleFactorial[n];
        }
        // Compute directly for large n
        double result = 0;
        for (int i = n; i > 1; i -= 2)
        {
            result += System.Math.Log2(i);
        }
        return result;
    }

    /// <summary>
    /// Log2 of number of rooted binary trees with n tips: log2((2n-3)!!)
    /// </summary>
    public static double Log2Rooted(int n)
    {
        if (n >= 0 && n < MaxTips)
        {
            return Log2RootedTrees[n];
        }
        // Compute for large n
        return Log2DoubleFactorialOf(2 * n - 3);
    }

    /// <summary>
    /// Log2 of number of unrooted binary trees with n tips: log2((2n-5)!!)
    /// This equals Log2Rooted(n-1).
    /// </summary>
    public static double Log2Unrooted(int n)
    {
        // l2unrooted[n] = l2rooted[n-1] in the R code
        return Log2Rooted(n - 1);
    }

    /// <summary>
    /// Calculate entropy of a set of counts.
    /// H = log2(N) - sum(n_i * log2(n_i)) / N
    /// </summary>
    public static double Entropy(ReadOnlySpan<int> counts)
    {
        int total = 0;
        double sum = 0;

        foreach (int count in counts)
        {
            if (count > 0)
            {
                sum += count * Log2(count);
                total += count;
            }
        }

        return total == 0 ? 0.0 : Log2(total) - sum / total;
    }

    /// <summary>
    /// Calculate clustering entropy of a split.
    /// H(S) = -(a/n)*log2(a/n) - (b/n)*log2(b/n)
    /// </summary>
    public static double SplitClusteringEntropy(int inSplit, int totalTips)
    {
        if (inSplit <= 0 || inSplit >= totalTips)
        {
            return 0;
        }

        int outSplit = totalTips - inSplit;
        double n = totalTips;

        // Using the formula from split_clust_info in information.h
        // H = -((a * log2(a) + b * log2(b)) / n - log2(n))
        // Which simplifies to standard entropy formula
        double pIn = inSplit / n;
        double pOut = outSplit / n;

        return -(pIn * System.Math.Log2(pIn) + pOut * System.Math.Log2(pOut));
    }

    /// <summary>
    /// Calculate phylogenetic information content of a split.
    /// IC = log2(unrooted trees with n tips) - log2(rooted trees with a tips) - log2(rooted trees with b tips)
    /// </summary>
    public static double SplitPhylogeneticInfo(int inSplit, int totalTips)
    {
        if (inSplit <= 1 || totalTips - inSplit <= 1)
        {
            return 0;
        }

        int outSplit = totalTips - inSplit;
        return Log2Unrooted(totalTips) - Log2Rooted(inSplit) - Log2Rooted(outSplit);
    }
}
