# TreeDistSharp

C# implementation of phylogenetic tree distance metrics, ported from the R [TreeDist](https://github.com/ms609/TreeDist) package by Martin R. Smith.

## Features

- **TreeDistance**: Normalized Clustering Information Distance (Smith 2020)
- **Robinson-Foulds distance**: Classic split-based tree comparison
- Parses Newick format trees
- Handles trees of any size
- Results match R's TreeDist package exactly

## Installation

```bash
dotnet add package TreeDistSharp
```

Or reference the project directly in your solution.

## Usage

```csharp
using TreeDistSharp;

// Compare two trees
string tree1 = "((a,b),(c,d));";
string tree2 = "((a,c),(b,d));";

// TreeDistance: normalized clustering information distance (0 = identical, 1 = maximally different)
double distance = TreeDistCalculator.TreeDistance(tree1, tree2);

// Robinson-Foulds: count of differing bipartitions
double rf = TreeDistCalculator.RobinsonFoulds(tree1, tree2);
```

### Batch comparisons

When comparing many trees, parse once and reuse:

```csharp
var trees = newicks.Select(n => TreeDistCalculator.ParseTree(n)).ToList();

// Use first tree's tip ordering for all comparisons
var tipLabels = trees[0].TipLabels;
var splits = trees.Select(t => TreeDistCalculator.GetSplits(
    TreeDistCalculator.ParseTree(newick, tipLabels)
)).ToArray();

// Pairwise distances
for (int i = 0; i < splits.Length; i++)
    for (int j = i + 1; j < splits.Length; j++)
        distances[i, j] = TreeDistCalculator.TreeDistance(splits[i], splits[j]);
```

Or use the built-in method:

```csharp
var distanceMatrix = TreeDistCalculator.PairwiseDistances(newicks, DistanceMetric.TreeDistance);
```

## Methods

| Method | Description |
|--------|-------------|
| `TreeDistance(t1, t2)` | Normalized clustering information distance |
| `RobinsonFoulds(t1, t2)` | Standard RF distance |
| `RobinsonFoulds(t1, t2, normalize: true)` | RF normalized to 0-1 |
| `InfoRobinsonFoulds(t1, t2)` | Information-weighted RF |
| `PairwiseDistances(trees, metric)` | Distance matrix for multiple trees |

## Requirements

- .NET 8.0 or later

## References

Smith, M.R. (2020). Information theoretic Generalized Robinson-Foulds metrics for comparing phylogenetic trees. *Bioinformatics* 36: 5007-5013. doi:[10.1093/bioinformatics/btaa614](https://doi.org/10.1093/bioinformatics/btaa614)

## License

GPL-3.0 - see [LICENSE](LICENSE)

This is a derivative work of the R TreeDist package. See [ATTRIBUTION.md](ATTRIBUTION.md) for credits.
