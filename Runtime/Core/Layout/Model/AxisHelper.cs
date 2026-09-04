namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Mapper between (main, cross) axis space and (x, y) space.
    /// </summary>
    /// <remarks>
    /// Lets a layout manager be written once and run in either orientation.
    /// A code-reuse tool internal to layout implementations; the virtualizer
    /// itself is orientation-agnostic and only sees 2D rects.
    /// </remarks>
    public readonly struct AxisHelper
    {
        public readonly Orientation Orientation;

        public AxisHelper(Orientation orientation)
        {
            Orientation = orientation;
        }

        public float Main(in LayoutVector2 v) => Orientation == Orientation.Vertical ? v.Y : v.X;
        public float Cross(in LayoutVector2 v) => Orientation == Orientation.Vertical ? v.X : v.Y;

        public float MainMin(in ItemRect r) => Orientation == Orientation.Vertical ? r.Y : r.X;
        public float MainMax(in ItemRect r) => Orientation == Orientation.Vertical ? r.YMax : r.XMax;

        public LayoutVector2 MakeVec(float main, float cross) =>
            Orientation == Orientation.Vertical ? new LayoutVector2(cross, main) : new LayoutVector2(main, cross);

        public ItemRect MakeRect(float mainPos, float crossPos, float mainSize, float crossSize) =>
            Orientation == Orientation.Vertical
                ? new ItemRect(crossPos, mainPos, crossSize, mainSize)
                : new ItemRect(mainPos, crossPos, mainSize, crossSize);
    }
}
