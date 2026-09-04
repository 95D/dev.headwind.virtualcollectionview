using System.Collections.Generic;

namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Placement rule: maps item sizes and a viewport to per-item virtual-space rects and a total content size.
    /// </summary>
    /// <remarks>
    /// Pure math, no view or platform concepts. Scrollable-axis capability
    /// lives here because it is a property of the placement rule, not of
    /// the view.
    /// </remarks>
    public abstract class LayoutManager
    {
        public abstract bool CanScrollHorizontally { get; }
        public abstract bool CanScrollVertically { get; }

        /// <summary>Total virtual content size. Valid after <see cref="Measure"/>.</summary>
        public LayoutVector2 ContentSize { get; protected set; }

        /// <summary>Item count captured by the last <see cref="Measure"/> call.</summary>
        protected int ItemCount { get; private set; }

        /// <summary>
        /// Recomputes all item placements. Called by the virtualizer whenever the
        /// data set, the viewport size, or the layout manager itself changes.
        /// </summary>
        public void Measure(int itemCount, ISizeProvider sizeProvider, LayoutVector2 viewportSize)
        {
            ItemCount = itemCount;
            OnMeasure(itemCount, sizeProvider, viewportSize);
        }

        protected abstract void OnMeasure(int itemCount, ISizeProvider sizeProvider, LayoutVector2 viewportSize);

        /// <summary>Virtual-space rect of an item. Valid after <see cref="Measure"/>.</summary>
        public abstract ItemRect GetItemRect(int index);

        /// <summary>
        /// Appends the indices (ascending) of items intersecting the given
        /// virtual-space rect. The base implementation is a linear scan;
        /// layouts should override with a placement-aware query
        /// (binary search over prefix sums, row arithmetic, per-lane search).
        /// </summary>
        public virtual void GetVisibleIndices(in ItemRect viewport, List<int> result)
        {
            for (var i = 0; i < ItemCount; i++)
            {
                if (GetItemRect(i).Overlaps(viewport))
                    result.Add(i);
            }
        }
    }
}
