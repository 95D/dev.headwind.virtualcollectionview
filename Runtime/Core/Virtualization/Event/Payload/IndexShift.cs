using System;

namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Adapter-index remap of a realized item whose bound model is unchanged.
    /// </summary>
    /// <remarks>
    /// Emitted when items are inserted, removed, or moved before the item:
    /// no rebind, but platform bookkeeping (index→view maps, holder indices)
    /// must follow. Delivered as a batch so consumers can remap collision-free
    /// (remove all <see cref="From"/> keys first, then add at <see cref="To"/>).
    /// </remarks>
    public readonly struct IndexShift : IEquatable<IndexShift>
    {
        public readonly int From;
        public readonly int To;

        public IndexShift(int from, int to)
        {
            From = from;
            To = to;
        }

        public bool Equals(IndexShift other) => From == other.From && To == other.To;
        public override bool Equals(object obj) => obj is IndexShift other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(From, To);
        public override string ToString() => $"{From}→{To}";
    }
}
