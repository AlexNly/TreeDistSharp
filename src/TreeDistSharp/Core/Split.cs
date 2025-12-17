using System.Numerics;

namespace TreeDistSharp.Core;

/// <summary>
/// Represents a bipartition (split) of tree leaves using bitwise storage.
/// Each bit position corresponds to a leaf index; 1 = in split, 0 = not in split.
/// Based on TreeTools SplitList representation.
/// </summary>
public readonly struct Split : IEquatable<Split>
{
    /// <summary>
    /// Number of bits per bin (ulong = 64 bits).
    /// </summary>
    public const int BitsPerBin = 64;

    private readonly ulong[] _bins;
    private readonly int _leafCount;  // popcount of the split
    private readonly int _totalTips;
    private readonly ulong _lastBinMask;  // mask for valid bits in last bin

    /// <summary>
    /// Number of leaves in this split (the smaller partition).
    /// </summary>
    public int LeafCount => _leafCount;

    /// <summary>
    /// Total number of tips in the tree.
    /// </summary>
    public int TotalTips => _totalTips;

    /// <summary>
    /// Number of bins used to store this split.
    /// </summary>
    public int BinCount => _bins.Length;

    /// <summary>
    /// Access a bin by index (read-only).
    /// </summary>
    public ulong this[int index] => _bins[index];

    /// <summary>
    /// Create a split from bin data.
    /// </summary>
    public Split(ulong[] bins, int totalTips)
    {
        _bins = bins;
        _totalTips = totalTips;

        // Calculate the mask for valid bits in the last bin
        int bitsInLastBin = totalTips % BitsPerBin;
        _lastBinMask = bitsInLastBin == 0 ? ulong.MaxValue : (1UL << bitsInLastBin) - 1;

        // Count leaves in the split
        int count = 0;
        for (int i = 0; i < bins.Length - 1; i++)
        {
            count += BitOperations.PopCount(bins[i]);
        }
        // Apply mask to last bin
        count += BitOperations.PopCount(bins[^1] & _lastBinMask);
        _leafCount = count;
    }

    /// <summary>
    /// Create a split from bin data with a pre-computed leaf count.
    /// </summary>
    public Split(ulong[] bins, int totalTips, int leafCount)
    {
        _bins = bins;
        _totalTips = totalTips;
        _leafCount = leafCount;

        int bitsInLastBin = totalTips % BitsPerBin;
        _lastBinMask = bitsInLastBin == 0 ? ulong.MaxValue : (1UL << bitsInLastBin) - 1;
    }

    /// <summary>
    /// Calculate number of bins needed for the given number of tips.
    /// </summary>
    public static int BinsRequired(int totalTips)
    {
        return (totalTips + BitsPerBin - 1) / BitsPerBin;
    }

    /// <summary>
    /// Get the complement of this split.
    /// </summary>
    public Split Complement()
    {
        var complementBins = new ulong[_bins.Length];
        for (int i = 0; i < _bins.Length - 1; i++)
        {
            complementBins[i] = ~_bins[i];
        }
        // XOR with mask for last bin (as in the R code)
        complementBins[^1] = _bins[^1] ^ _lastBinMask;

        return new Split(complementBins, _totalTips, _totalTips - _leafCount);
    }

    /// <summary>
    /// Count the overlap (intersection) with another split.
    /// </summary>
    public int CountOverlap(in Split other)
    {
        int overlap = 0;
        int minBins = System.Math.Min(_bins.Length, other._bins.Length);

        for (int i = 0; i < minBins - 1; i++)
        {
            overlap += BitOperations.PopCount(_bins[i] & other._bins[i]);
        }

        if (minBins > 0)
        {
            // Apply mask for last bin
            overlap += BitOperations.PopCount((_bins[minBins - 1] & other._bins[minBins - 1]) & _lastBinMask);
        }

        return overlap;
    }

    /// <summary>
    /// Check if this split exactly matches another.
    /// </summary>
    public bool Equals(Split other)
    {
        if (_bins.Length != other._bins.Length || _totalTips != other._totalTips)
        {
            return false;
        }

        for (int i = 0; i < _bins.Length - 1; i++)
        {
            if (_bins[i] != other._bins[i])
            {
                return false;
            }
        }

        // Compare last bin with mask applied
        return (_bins[^1] & _lastBinMask) == (other._bins[^1] & _lastBinMask);
    }

    /// <summary>
    /// Check if this split matches another (either directly or as complement).
    /// This is the key comparison for RF distance.
    /// </summary>
    public bool EqualsOrComplement(in Split other)
    {
        if (_totalTips != other._totalTips || _bins.Length != other._bins.Length)
        {
            return false;
        }

        bool allMatch = true;
        bool allComplement = true;

        for (int i = 0; i < _bins.Length - 1; i++)
        {
            if (_bins[i] != other._bins[i])
            {
                allMatch = false;
            }
            if (_bins[i] != ~other._bins[i])
            {
                allComplement = false;
            }
            if (!allMatch && !allComplement)
            {
                return false;
            }
        }

        // Check last bin with mask
        int lastIdx = _bins.Length - 1;
        ulong aLast = _bins[lastIdx] & _lastBinMask;
        ulong bLast = other._bins[lastIdx] & _lastBinMask;
        ulong bComplementLast = (other._bins[lastIdx] ^ _lastBinMask) & _lastBinMask;

        if (allMatch && aLast != bLast)
        {
            allMatch = false;
        }
        if (allComplement && aLast != bComplementLast)
        {
            allComplement = false;
        }

        return allMatch || allComplement;
    }

    public override bool Equals(object? obj) => obj is Split other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_totalTips);
        foreach (var bin in _bins)
        {
            hash.Add(bin);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(Split left, Split right) => left.Equals(right);
    public static bool operator !=(Split left, Split right) => !left.Equals(right);
}
