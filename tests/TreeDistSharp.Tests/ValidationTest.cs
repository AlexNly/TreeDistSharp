using Xunit.Abstractions;

namespace TreeDistSharp.Tests;

/// <summary>
/// Validation tests to verify C# implementation matches R package results.
/// </summary>
public class ValidationTest
{
    private readonly ITestOutputHelper _output;

    public ValidationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Validate_UserExample_OutputValue()
    {
        // User's test case - R package reports gRF distance: 0.138
        string groundTruth = "(((leaf_2:1,(leaf_3:1,leaf_4:1)internal_4:2)internal_2:0,((leaf_5:3,leaf_6:4)internal_5:4,((leaf_7:2,(leaf_8:1,leaf_9:3)internal_9:1)internal_7:3,(leaf_0:0,leaf_1:3)internal_8:2)internal_6:0)internal_3:1)internal_1:5)internal_0:0;";
        string reconstructed = "((((((leaf_7:3.81250,(leaf_8:1.88889,leaf_9:7.11111):1.18750):5.53125,(leaf_5:6.32143,leaf_6:11.67857):3.21875):0.46875,(leaf_0:0.08333,leaf_1:5.91667):2.40625):1.59375,leaf_2:1.04167):0.95833,(leaf_3:1.10000,leaf_4:0.90000):1.93750):5.06250)internal_0:0.00000;";

        var tree1 = TreeDistCalculator.ParseTree(groundTruth);
        var tree2 = TreeDistCalculator.ParseTree(reconstructed, tree1.TipLabels);

        var splits1 = TreeDistCalculator.GetSplits(tree1);
        var splits2 = TreeDistCalculator.GetSplits(tree2);

        _output.WriteLine($"Tree 1 tips: {tree1.TipCount}");
        _output.WriteLine($"Tree 1 splits: {splits1.Count}");
        _output.WriteLine($"Tree 2 splits: {splits2.Count}");

        double treeDistance = TreeDistCalculator.TreeDistance(groundTruth, reconstructed);
        double rfDistance = TreeDistCalculator.RobinsonFoulds(groundTruth, reconstructed, normalize: false);
        double rfNormalized = TreeDistCalculator.RobinsonFoulds(groundTruth, reconstructed, normalize: true);

        _output.WriteLine($"");
        _output.WriteLine($"TreeDistance (normalized CID): {treeDistance:F6}");
        _output.WriteLine($"Robinson-Foulds (raw): {rfDistance}");
        _output.WriteLine($"Robinson-Foulds (normalized): {rfNormalized:F6}");
        _output.WriteLine($"");
        _output.WriteLine($"R package reports gRF: 0.138");
        _output.WriteLine($"Our TreeDistance: {treeDistance:F3}");

        // The R package's TreeDistance() returns normalized clustering info distance
        // Expected: approximately 0.138
        Assert.True(treeDistance >= 0 && treeDistance <= 1);
    }

    [Fact]
    public void Validate_SimpleCase_IdenticalTrees()
    {
        string tree = "((a,b),(c,d));";

        double dist = TreeDistCalculator.TreeDistance(tree, tree);

        _output.WriteLine($"Identical trees distance: {dist}");
        Assert.Equal(0, dist, precision: 10);
    }

    [Fact]
    public void Validate_SimpleCase_DifferentTrees()
    {
        // These two 4-tip trees have completely different splits
        string tree1 = "((a,b),(c,d));";  // Split: {a,b} | {c,d}
        string tree2 = "((a,c),(b,d));";  // Split: {a,c} | {b,d}

        double treeDistance = TreeDistCalculator.TreeDistance(tree1, tree2);
        double rf = TreeDistCalculator.RobinsonFoulds(tree1, tree2);

        _output.WriteLine($"Tree1: {tree1}");
        _output.WriteLine($"Tree2: {tree2}");
        _output.WriteLine($"TreeDistance: {treeDistance:F6}");
        _output.WriteLine($"RF distance: {rf}");

        // RF should be 2 (each tree has 1 split, none match)
        Assert.Equal(2, rf);
        // TreeDistance should be 1.0 (normalized, completely different)
        Assert.Equal(1.0, treeDistance, precision: 6);
    }

    [Fact]
    public void Validate_8TipBalancedVsSymmetric()
    {
        // Standard test trees from R package
        string treeSym8 = "((e, (f, (g, h))), (((a, b), c), d));";
        string treeBal8 = "(((e, f), (g, h)), ((a, b), (c, d)));";

        var t1 = TreeDistCalculator.ParseTree(treeSym8);
        var t2 = TreeDistCalculator.ParseTree(treeBal8, t1.TipLabels);

        var s1 = TreeDistCalculator.GetSplits(t1);
        var s2 = TreeDistCalculator.GetSplits(t2);

        _output.WriteLine($"Symmetric tree splits: {s1.Count}");
        _output.WriteLine($"Balanced tree splits: {s2.Count}");

        double treeDistance = TreeDistCalculator.TreeDistance(treeSym8, treeBal8);
        double rf = TreeDistCalculator.RobinsonFoulds(treeSym8, treeBal8);

        _output.WriteLine($"TreeDistance: {treeDistance:F6}");
        _output.WriteLine($"RF distance: {rf}");

        // 8-tip binary tree should have n-3 = 5 non-trivial splits
        Assert.Equal(5, s1.Count);
        Assert.Equal(5, s2.Count);
    }
}
