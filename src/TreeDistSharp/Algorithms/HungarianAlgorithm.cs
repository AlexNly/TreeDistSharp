namespace TreeDistSharp.Algorithms;

/// <summary>
/// Solves the Linear Assignment Problem using the Jonker-Volgenant algorithm.
/// Based on TreeDist/src/lap.cpp by Roy Jonker, Martin R. Smith, Yong Yang, Yi Cao.
///
/// Original paper: "A Shortest Augmenting Path Algorithm for Dense and Sparse
/// Linear Assignment Problems," Computing 38, 325-340, 1987
/// by R. Jonker and A. Volgenant, University of Amsterdam.
/// </summary>
public static class HungarianAlgorithm
{
    private const long Big = long.MaxValue / 4096; // Prevent overflow
    private const long RoundPrecision = 2048 * 2048;

    /// <summary>
    /// Solve the assignment problem (minimization).
    /// </summary>
    /// <param name="costMatrix">Cost matrix (row-major, can be non-square)</param>
    /// <param name="nRows">Number of rows</param>
    /// <param name="nCols">Number of columns</param>
    /// <param name="rowSolution">Output: column assigned to each row (-1 if unassigned)</param>
    /// <returns>Total cost of optimal assignment</returns>
    public static long Solve(long[,] costMatrix, int nRows, int nCols, out int[] rowSolution)
    {
        int dim = System.Math.Max(nRows, nCols);

        // Pad to square matrix if needed
        var paddedCost = new long[dim, dim];
        long maxCost = Big / dim;

        for (int i = 0; i < dim; i++)
        {
            for (int j = 0; j < dim; j++)
            {
                if (i < nRows && j < nCols)
                {
                    paddedCost[i, j] = costMatrix[i, j];
                }
                else
                {
                    paddedCost[i, j] = maxCost; // Padding value
                }
            }
        }

        var rowsol = new int[dim];
        var colsol = new int[dim];

        long totalCost = Lap(dim, paddedCost, rowsol, colsol);

        // Extract row solution, filtering out padding
        rowSolution = new int[nRows];
        for (int i = 0; i < nRows; i++)
        {
            rowSolution[i] = rowsol[i] < nCols ? rowsol[i] : -1;
        }

        return totalCost;
    }

    /// <summary>
    /// Solve the assignment problem for a square matrix.
    /// </summary>
    public static long Solve(long[,] costMatrix, out int[] rowSolution)
    {
        int dim = costMatrix.GetLength(0);
        if (dim != costMatrix.GetLength(1))
        {
            throw new ArgumentException("Cost matrix must be square for this overload");
        }

        var rowsol = new int[dim];
        var colsol = new int[dim];

        long totalCost = Lap(dim, costMatrix, rowsol, colsol);
        rowSolution = rowsol;
        return totalCost;
    }

    private static bool NontriviallyLessThan(long a, long b)
    {
        return a + (a > RoundPrecision ? 8 : 0) < b;
    }

    private static long Lap(int dim, long[,] cost, int[] rowsol, int[] colsol)
    {
        var v = new long[dim];      // Column potentials
        var matches = new int[dim]; // Count of how many times a row could be assigned

        // Initialize to unassigned
        Array.Fill(colsol, -1);

        // COLUMN REDUCTION
        for (int j = dim - 1; j >= 0; j--)
        {
            // Find minimum in column j
            long min = cost[0, j];
            int imin = 0;

            for (int i = 1; i < dim; i++)
            {
                if (cost[i, j] < min)
                {
                    min = cost[i, j];
                    imin = i;
                }
            }

            v[j] = min;
            matches[imin]++;

            if (matches[imin] == 1)
            {
                rowsol[imin] = j;
                colsol[j] = imin;
            }
            else if (v[j] < v[rowsol[imin]])
            {
                int j1 = rowsol[imin];
                rowsol[imin] = j;
                colsol[j] = imin;
                colsol[j1] = -1;
            }
            else
            {
                colsol[j] = -1;
            }
        }

        // REDUCTION TRANSFER
        var freeRows = new int[dim];
        int numFree = 0;

        for (int i = 0; i < dim; i++)
        {
            if (matches[i] == 0)
            {
                freeRows[numFree++] = i;
            }
            else if (matches[i] == 1)
            {
                int j1 = rowsol[i];
                long minCost = long.MaxValue;

                for (int j = 0; j < dim; j++)
                {
                    if (j != j1)
                    {
                        long reducedCost = cost[i, j] - v[j];
                        if (reducedCost < minCost)
                        {
                            minCost = reducedCost;
                        }
                    }
                }

                v[j1] -= minCost;
            }
        }

        // AUGMENTING ROW REDUCTION
        var colList = new int[dim];

        for (int loopcnt = 0; loopcnt < 2; loopcnt++)
        {
            int prevNumFree = numFree;
            numFree = 0;
            int k = 0;

            while (k < prevNumFree)
            {
                int i = freeRows[k++];

                // Find minimum and second minimum reduced cost
                long umin = cost[i, 0] - v[0];
                long usubmin = long.MaxValue;
                int minIdx = 0;
                int j2 = 0;

                for (int j = 1; j < dim; j++)
                {
                    long h = cost[i, j] - v[j];
                    if (h < usubmin)
                    {
                        if (h >= umin)
                        {
                            usubmin = h;
                            j2 = j;
                        }
                        else
                        {
                            usubmin = umin;
                            umin = h;
                            j2 = minIdx;
                            minIdx = j;
                        }
                    }
                }

                int j1 = minIdx;
                int i0 = colsol[j1];

                if (NontriviallyLessThan(umin, usubmin))
                {
                    v[j1] -= (usubmin - umin);
                }
                else if (i0 > -1)
                {
                    j1 = j2;
                    i0 = colsol[j2];
                }

                rowsol[i] = j1;
                colsol[j1] = i;

                if (i0 > -1)
                {
                    if (NontriviallyLessThan(umin, usubmin))
                    {
                        freeRows[--k] = i0;
                    }
                    else
                    {
                        freeRows[numFree++] = i0;
                    }
                }
            }
        }

        // AUGMENT SOLUTION
        var d = new long[dim];           // Cost-distance in augmenting path
        var predecessor = new int[dim];  // Row-predecessor of column

        for (int f = 0; f < numFree; f++)
        {
            int freeRow = freeRows[f];
            int endOfPath = 0;
            int last = 0;

            // Initialize Dijkstra
            for (int j = 0; j < dim; j++)
            {
                d[j] = cost[freeRow, j] - v[j];
                predecessor[j] = freeRow;
                colList[j] = j;
            }

            long min = 0;
            int low = 0;
            int up = 0;
            bool unassignedFound = false;

            do
            {
                if (up == low)
                {
                    last = low - 1;
                    min = d[colList[up++]];

                    for (int k = up; k < dim; k++)
                    {
                        int j = colList[k];
                        long h = d[j];
                        if (h <= min)
                        {
                            if (h < min)
                            {
                                up = low;
                                min = h;
                            }
                            colList[k] = colList[up];
                            colList[up++] = j;
                        }
                    }

                    for (int k = low; k < up; k++)
                    {
                        if (colsol[colList[k]] < 0)
                        {
                            endOfPath = colList[k];
                            unassignedFound = true;
                            break;
                        }
                    }
                }

                if (!unassignedFound)
                {
                    int j1 = colList[low++];
                    int i = colsol[j1];
                    long h = cost[i, j1] - v[j1] - min;

                    for (int k = up; k < dim; k++)
                    {
                        int j = colList[k];
                        long v2 = cost[i, j] - v[j] - h;
                        if (v2 < d[j])
                        {
                            predecessor[j] = i;
                            if (v2 == min)
                            {
                                if (colsol[j] < 0)
                                {
                                    endOfPath = j;
                                    unassignedFound = true;
                                    break;
                                }
                                else
                                {
                                    colList[k] = colList[up];
                                    colList[up++] = j;
                                }
                            }
                            d[j] = v2;
                        }
                    }
                }
            } while (!unassignedFound);

            // Update column prices
            for (int k = 0; k <= last; k++)
            {
                int j1 = colList[k];
                v[j1] += d[j1] - min;
            }

            // Reset assignments along alternating path
            int current = endOfPath;
            do
            {
                int i = predecessor[current];
                colsol[current] = i;
                int temp = rowsol[i];
                rowsol[i] = current;
                current = temp;
            } while (current != freeRow);

            rowsol[freeRow] = endOfPath;
        }

        // Calculate optimal cost
        long lapCost = 0;
        for (int i = 0; i < dim; i++)
        {
            lapCost += cost[i, rowsol[i]];
        }

        return lapCost;
    }
}
