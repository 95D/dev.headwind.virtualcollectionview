namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Why an item was virtualized.
    /// </summary>
    /// <remarks>
    /// Only <see cref="Removed"/> represents a data-level disappearance
    /// (exit-animation candidate); the rest must recycle immediately.
    /// </remarks>
    public enum VirtualizeCause
    {
        /// <summary>Left the (buffered) viewport through scrolling or relayout.</summary>
        ScrolledOut,

        /// <summary>Removed from the data set while visible.</summary>
        Removed,

        /// <summary>Virtualized by a full data invalidation; never animate.</summary>
        Invalidated
    }
}
