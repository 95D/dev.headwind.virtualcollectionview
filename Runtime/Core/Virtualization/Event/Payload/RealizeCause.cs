namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Why an item was realized.
    /// </summary>
    /// <remarks>
    /// Animation systems use this to decide whether an appear effect is
    /// warranted: only <see cref="Inserted"/> represents a data-level
    /// appearance; the rest are plain placement.
    /// </remarks>
    public enum RealizeCause
    {
        /// <summary>Entered the (buffered) viewport through scrolling or relayout.</summary>
        ScrolledIn,

        /// <summary>Newly inserted into the data set at a visible position.</summary>
        Inserted,

        /// <summary>Re-realized by a full data invalidation; never animate.</summary>
        Invalidated
    }
}
