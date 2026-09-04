namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Cross-axis placement for linear layouts (one item per line).
    /// </summary>
    /// <remarks>
    /// <see cref="Stretch"/> ignores the item's preferred cross size and fills
    /// the available space; the other values respect the preferred size and
    /// position the item within the leftover.
    /// </remarks>
    public enum LinearCrossAlignment
    {
        Stretch,
        Start,
        Center,
        End
    }
}
