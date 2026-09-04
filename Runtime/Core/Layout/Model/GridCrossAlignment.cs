namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Cross-axis placement for grid layouts (a lane block per line).
    /// </summary>
    /// <remarks>
    /// <see cref="Stretch"/> divides the available space between lanes; the
    /// other values size cells from the items' preferred cross extent and
    /// distribute the leftover — around the block (Start/Center/End) or into
    /// the gutters between lanes (<see cref="SpaceBetween"/>, which degrades
    /// to Start when the span is 1).
    /// </remarks>
    public enum GridCrossAlignment
    {
        Stretch,
        Start,
        Center,
        End,
        SpaceBetween
    }
}
