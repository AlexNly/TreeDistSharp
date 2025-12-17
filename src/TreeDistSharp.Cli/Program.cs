using TreeDistSharp;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintHelp();
    return 0;
}

if (args.Length < 2)
{
    Console.Error.WriteLine("Error: Need two trees to compare.");
    Console.Error.WriteLine("Run 'treedist --help' for usage.");
    return 1;
}

string tree1 = args[0];
string tree2 = args[1];

// Parse options
string metric = "all";
bool quiet = false;

for (int i = 2; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-m" or "--metric":
            if (i + 1 < args.Length)
                metric = args[++i].ToLower();
            break;
        case "-q" or "--quiet":
            quiet = true;
            break;
    }
}

// Handle file input
if (File.Exists(tree1))
    tree1 = File.ReadAllText(tree1).Trim();
if (File.Exists(tree2))
    tree2 = File.ReadAllText(tree2).Trim();

try
{
    var t1 = TreeDistCalculator.ParseTree(tree1);
    var t2 = TreeDistCalculator.ParseTree(tree2, t1.TipLabels);

    if (t1.TipCount != t2.TipCount)
    {
        Console.Error.WriteLine($"Error: Trees have different tip counts ({t1.TipCount} vs {t2.TipCount})");
        return 1;
    }

    if (quiet)
    {
        // Just output the number
        Console.WriteLine(metric switch
        {
            "cid" or "treedistance" => TreeDistCalculator.TreeDistance(tree1, tree2),
            "rf" => TreeDistCalculator.RobinsonFoulds(tree1, tree2),
            "rfn" => TreeDistCalculator.RobinsonFoulds(tree1, tree2, normalize: true),
            _ => TreeDistCalculator.TreeDistance(tree1, tree2)
        });
    }
    else if (metric == "all")
    {
        Console.WriteLine($"Tips:         {t1.TipCount}");
        Console.WriteLine($"TreeDistance: {TreeDistCalculator.TreeDistance(tree1, tree2):F6}");
        Console.WriteLine($"RF:           {TreeDistCalculator.RobinsonFoulds(tree1, tree2)}");
        Console.WriteLine($"RF (norm):    {TreeDistCalculator.RobinsonFoulds(tree1, tree2, normalize: true):F6}");
    }
    else
    {
        double result = metric switch
        {
            "cid" or "treedistance" => TreeDistCalculator.TreeDistance(tree1, tree2),
            "rf" => TreeDistCalculator.RobinsonFoulds(tree1, tree2),
            "rfn" => TreeDistCalculator.RobinsonFoulds(tree1, tree2, normalize: true),
            _ => throw new ArgumentException($"Unknown metric: {metric}")
        };
        Console.WriteLine($"{metric}: {result}");
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
    treedist - Phylogenetic tree distance calculator

    Usage:
      treedist <tree1> <tree2> [options]
      treedist tree1.nwk tree2.nwk

    Arguments:
      tree1    First tree (Newick string or file path)
      tree2    Second tree (Newick string or file path)

    Options:
      -m, --metric <name>   Metric to compute (default: all)
                            cid, treedistance  Clustering Info Distance
                            rf                 Robinson-Foulds
                            rfn                Robinson-Foulds normalized
      -q, --quiet           Output only the number
      -h, --help            Show this help

    Examples:
      treedist "((a,b),(c,d));" "((a,c),(b,d));"
      treedist tree1.nwk tree2.nwk -m rf
      treedist "((a,b),(c,d));" "((a,c),(b,d));" -m cid -q
    """);
}
