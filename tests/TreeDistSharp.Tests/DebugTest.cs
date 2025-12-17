using TreeDistSharp.Core;
using TreeDistSharp.Math;
using TreeDistSharp.Algorithms;
using Xunit.Abstractions;

namespace TreeDistSharp.Tests;

public class DebugTest
{
    private readonly ITestOutputHelper _output;

    public DebugTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_UserExample_DetailedOutput()
    {
        string groundTruth = "(((leaf_2:1,(leaf_3:1,leaf_4:1)internal_4:2)internal_2:0,((leaf_5:3,leaf_6:4)internal_5:4,((leaf_7:2,(leaf_8:1,leaf_9:3)internal_9:1)internal_7:3,(leaf_0:0,leaf_1:3)internal_8:2)internal_6:0)internal_3:1)internal_1:5)internal_0:0;";
        string reconstructed = "((((((leaf_7:3.81250,(leaf_8:1.88889,leaf_9:7.11111):1.18750):5.53125,(leaf_5:6.32143,leaf_6:11.67857):3.21875):0.46875,(leaf_0:0.08333,leaf_1:5.91667):2.40625):1.59375,leaf_2:1.04167):0.95833,(leaf_3:1.10000,leaf_4:0.90000):1.93750):5.06250)internal_0:0.00000;";

        var tree1 = TreeDistCalculator.ParseTree(groundTruth);
        var tree2 = TreeDistCalculator.ParseTree(reconstructed, tree1.TipLabels);

        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        _output.WriteLine("=== Tree 1 ===");
        _output.WriteLine($"Tips: {tree1.TipCount}");
        _output.WriteLine($"Tip labels: {string.Join(", ", tree1.TipLabels)}");
        _output.WriteLine($"Splits count: {splits1.Count}");

        double entropy1 = splits1.TotalClusteringEntropy();
        _output.WriteLine($"Total clustering entropy: {entropy1:F6}");

        for (int i = 0; i < splits1.Count; i++)
        {
            ref readonly var split = ref splits1[i];
            double h = InfoTables.SplitClusteringEntropy(split.LeafCount, tree1.TipCount);
            _output.WriteLine($"  Split {i}: {split.LeafCount} tips, entropy={h:F6}");
        }

        _output.WriteLine("\n=== Tree 2 ===");
        _output.WriteLine($"Splits count: {splits2.Count}");

        double entropy2 = splits2.TotalClusteringEntropy();
        _output.WriteLine($"Total clustering entropy: {entropy2:F6}");

        for (int i = 0; i < splits2.Count; i++)
        {
            ref readonly var split = ref splits2[i];
            double h = InfoTables.SplitClusteringEntropy(split.LeafCount, tree2.TipCount);
            _output.WriteLine($"  Split {i}: {split.LeafCount} tips, entropy={h:F6}");
        }

        _output.WriteLine("\n=== Calculations ===");
        double totalEntropy = entropy1 + entropy2;
        _output.WriteLine($"Total entropy (H1 + H2): {totalEntropy:F6}");

        double mci = ClusteringInfo.MutualClusteringInfo(splits1, splits2, tree1.TipCount);
        _output.WriteLine($"Mutual Clustering Info: {mci:F6}");

        double distance = totalEntropy - 2 * mci;
        _output.WriteLine($"Raw distance (H1+H2 - 2*MCI): {distance:F6}");

        double normalized = distance / totalEntropy;
        _output.WriteLine($"Normalized distance: {normalized:F6}");

        _output.WriteLine($"\n=== Final Result ===");
        _output.WriteLine($"Our TreeDistance: {normalized:F6}");
        _output.WriteLine($"R package reports: 0.138");
    }

    [Fact]
    public void Debug_SimpleFourTip()
    {
        string tree1str = "((a,b),(c,d));";
        string tree2str = "((a,c),(b,d));";

        var tree1 = TreeDistCalculator.ParseTree(tree1str);
        var tree2 = TreeDistCalculator.ParseTree(tree2str, tree1.TipLabels);

        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        _output.WriteLine($"Tree 1 splits: {splits1.Count}");
        _output.WriteLine($"Tree 2 splits: {splits2.Count}");

        for (int i = 0; i < splits1.Count; i++)
        {
            ref readonly var s = ref splits1[i];
            _output.WriteLine($"Split1[{i}]: {s.LeafCount} tips");
        }

        double h1 = splits1.TotalClusteringEntropy();
        double h2 = splits2.TotalClusteringEntropy();
        _output.WriteLine($"H1 = {h1:F6}, H2 = {h2:F6}");

        // For 4-tip tree with split of size 2:
        // H = -(2/4)*log2(2/4) - (2/4)*log2(2/4) = -0.5*(-1) - 0.5*(-1) = 1.0
        double expectedSingleH = 1.0;
        _output.WriteLine($"Expected single split entropy: {expectedSingleH}");

        double mci = ClusteringInfo.MutualClusteringInfo(splits1, splits2, 4);
        _output.WriteLine($"MCI = {mci:F6}");

        // If splits are completely different, MCI = 0
        // distance = h1 + h2 - 2*0 = 1 + 1 = 2
        // normalized = 2 / 2 = 1.0
    }
}
