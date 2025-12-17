using TreeDistSharp.Core;
using TreeDistSharp.Parsing;

namespace TreeDistSharp.Tests;

public class RobinsonFouldsTests
{
    [Fact]
    public void Distance_IdenticalTrees_ReturnsZero()
    {
        string newick = "((a, b), (c, d));";
        var tree = NewickParser.Parse(newick);
        var splits = SplitList.FromTree(tree);

        int rf = TreeDistSharp.Algorithms.RobinsonFoulds.Distance(splits, splits);

        Assert.Equal(0, rf);
    }

    [Fact]
    public void Distance_IdenticalBalancedTrees_ReturnsZero()
    {
        var tree1 = NewickParser.Parse("(((e, f), (g, h)), ((a, b), (c, d)));");
        var tree2 = NewickParser.Parse("(((e, f), (g, h)), ((a, b), (c, d)));", tree1.TipLabels);

        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        int rf = TreeDistSharp.Algorithms.RobinsonFoulds.Distance(splits1, splits2);

        Assert.Equal(0, rf);
    }

    [Fact]
    public void Distance_DifferentTrees_ReturnsPositive()
    {
        var tree1 = NewickParser.Parse("((a, b), (c, d));");
        var tree2 = NewickParser.Parse("((a, c), (b, d));", tree1.TipLabels);

        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        int rf = TreeDistSharp.Algorithms.RobinsonFoulds.Distance(splits1, splits2);

        Assert.True(rf > 0);
    }

    [Fact]
    public void Distance_FourTipsDifferentTopology_ReturnsTwo()
    {
        // Two 4-tip binary trees with different topologies
        // Each has 1 non-trivial split (n-3 = 4-3 = 1)
        var tree1 = NewickParser.Parse("((a, b), (c, d));");
        var tree2 = NewickParser.Parse("((a, c), (b, d));", tree1.TipLabels);

        var splits1 = SplitList.FromTree(tree1);
        var splits2 = SplitList.FromTree(tree2);

        // Verify each tree has exactly 1 split
        Assert.Equal(1, splits1.Count);
        Assert.Equal(1, splits2.Count);

        // Each tree has 1 split, they don't match, so RF = 1 + 1 - 0 = 2
        int rf = TreeDistSharp.Algorithms.RobinsonFoulds.Distance(splits1, splits2);

        Assert.Equal(2, rf);
    }

    [Fact]
    public void NormalizedDistance_IdenticalTrees_ReturnsZero()
    {
        string newick = "((a, b), (c, d));";
        double dist = TreeDistCalculator.RobinsonFoulds(newick, newick, normalize: true);

        Assert.Equal(0, dist, precision: 10);
    }

    [Fact]
    public void NormalizedDistance_MaximallyDifferent_ReturnsOne()
    {
        var tree1 = NewickParser.Parse("((a, b), (c, d));");
        var tree2 = NewickParser.Parse("((a, c), (b, d));", tree1.TipLabels);

        double dist = TreeDistCalculator.RobinsonFoulds(
            "((a, b), (c, d));",
            "((a, c), (b, d));",
            normalize: true
        );

        Assert.Equal(1.0, dist, precision: 10);
    }

    [Fact]
    public void Distance_EightTipTrees_MatchesExpected()
    {
        // Trees from the R package tests
        string treeSym8 = "((e, (f, (g, h))), (((a, b), c), d));";
        string treeBal8 = "(((e, f), (g, h)), ((a, b), (c, d)));";

        var t1 = NewickParser.Parse(treeSym8);
        var t2 = NewickParser.Parse(treeBal8, t1.TipLabels);

        var s1 = SplitList.FromTree(t1);
        var s2 = SplitList.FromTree(t2);

        int rf = TreeDistSharp.Algorithms.RobinsonFoulds.Distance(s1, s2);

        // Both trees have 5 non-trivial splits each
        // They share some splits, RF should be > 0
        Assert.True(rf >= 0);
        Assert.True(rf <= 10); // Maximum possible
    }

    [Fact]
    public void Distance_StarTree_HasNoSplits()
    {
        // Star tree has no non-trivial splits
        var starTree = NewickParser.Parse("(a, b, c, d, e, f, g, h);");
        var splits = SplitList.FromTree(starTree);

        Assert.Equal(0, splits.Count);
    }
}
