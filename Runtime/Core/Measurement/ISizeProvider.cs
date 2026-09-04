namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Supplier of each item's preferred size, up front.
    /// </summary>
    /// <remarks>
    /// The measurement contract: sizes are known before layout, keeping it a
    /// pure, synchronous computation — the virtualizer never measures realized
    /// views. A layout manager reads the axis it cares about and may override
    /// the other (e.g. a vertical list stretches item width to the viewport,
    /// a grid computes cell width from its span).
    /// </remarks>
    public interface ISizeProvider
    {
        LayoutVector2 GetItemSize(int index);
    }
}
