using TreeDistSharp.Parsing;

namespace TreeDistSharp.Tests;

public class NewickParserTests
{
    [Fact]
    public void Parse_SimpleTree_ReturnsCorrectStructure()
    {
        var tree = NewickParser.Parse("(a, b);");

        Assert.Equal(2, tree.TipCount);
        Assert.Contains("a", tree.TipLabels);
        Assert.Contains("b", tree.TipLabels);
    }

    [Fact]
    public void Parse_NestedTree_ReturnsCorrectStructure()
    {
        var tree = NewickParser.Parse("((a, b), (c, d));");

        Assert.Equal(4, tree.TipCount);
        Assert.Equal(new[] { "a", "b", "c", "d" }, tree.TipLabels);
    }

    [Fact]
    public void Parse_Polytomy_ReturnsCorrectStructure()
    {
        var tree = NewickParser.Parse("(a, b, c, d);");

        Assert.Equal(4, tree.TipCount);
        Assert.Equal(4, tree.Root.Children.Count);
    }

    [Fact]
    public void Parse_WithBranchLengths_IgnoresLengths()
    {
        var tree = NewickParser.Parse("((a:0.1, b:0.2):0.3, (c:0.4, d:0.5):0.6);");

        Assert.Equal(4, tree.TipCount);
        Assert.Equal(new[] { "a", "b", "c", "d" }, tree.TipLabels);
    }

    [Fact]
    public void Parse_WithInternalLabels_ParsesCorrectly()
    {
        var tree = NewickParser.Parse("((a, b)X, (c, d)Y)Z;");

        Assert.Equal(4, tree.TipCount);
        Assert.Equal("Z", tree.Root.Label);
    }

    [Fact]
    public void Parse_DeeplyNested_ReturnsCorrectStructure()
    {
        var tree = NewickParser.Parse("((e, (f, (g, h))), (((a, b), c), d));");

        Assert.Equal(8, tree.TipCount);
        Assert.False(tree.Root.IsLeaf);
    }

    [Fact]
    public void Parse_WithWhitespace_HandlesCorrectly()
    {
        var tree = NewickParser.Parse("( ( a , b ) , ( c , d ) ) ;");

        Assert.Equal(4, tree.TipCount);
    }

    [Fact]
    public void Parse_EmptyString_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => NewickParser.Parse(""));
    }

    [Fact]
    public void Parse_WithOrderedTipLabels_AssignsCorrectIndices()
    {
        var tipLabels = new[] { "d", "c", "b", "a" };
        var tree = NewickParser.Parse("((a, b), (c, d));", tipLabels);

        Assert.Equal(3, tree.GetTipIndex("a"));
        Assert.Equal(2, tree.GetTipIndex("b"));
        Assert.Equal(1, tree.GetTipIndex("c"));
        Assert.Equal(0, tree.GetTipIndex("d"));
    }
}
