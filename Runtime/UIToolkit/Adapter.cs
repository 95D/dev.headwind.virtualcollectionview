using System;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// Supplier of data, per-item sizes, and view holders to a <see cref="VirtualCollectionView{T}"/>.
    /// </summary>
    /// <remarks>
    /// Multiple view types are supported by returning different holder
    /// <see cref="Type"/>s from <see cref="GetViewType"/>: the Type is the
    /// recycling-pool key, so a holder is only ever rebound to indices of its
    /// own view type.
    /// </remarks>
    public abstract class Adapter<T> : ISizeProvider
    {
        public abstract int Count { get; }

        public abstract T GetItem(int index);

        /// <summary>
        /// Preferred item size. The layout manager reads the axis it needs and
        /// may override the other (lists stretch the cross axis, grids derive
        /// cell width from the span).
        /// </summary>
        public abstract LayoutVector2 GetItemSize(int index);

        /// <summary>
        /// Pool key for the item at <paramref name="index"/>. Return the
        /// concrete holder type. Defaults to a single shared key.
        /// </summary>
        public virtual Type GetViewType(int index) => typeof(CollectionItemViewHolder<T>);

        /// <summary>Creates a fresh holder for the given pool key.</summary>
        public abstract CollectionItemViewHolder<T> CreateViewHolder(Type viewType);

        /// <summary>Raised by <see cref="NotifyDataSetChanged"/>; observed by the view.</summary>
        public event Action DataSetChanged;

        /// <summary>Raised by the granular notify methods; observed by the view. Args: index, count (or from, to).</summary>
        public event Action<int, int> ItemRangeInserted;
        public event Action<int, int> ItemRangeRemoved;
        public event Action<int, int> ItemMoved;
        public event Action<int, int> ItemRangeChanged;

        /// <summary>
        /// Full invalidation: the view virtualizes everything, remeasures, and
        /// rebinds the visible range. Item identity is lost, so nothing
        /// animates — mutate the data first, then prefer the granular
        /// notifications below when an <see cref="IItemAnimator{T}"/> is set.
        /// </summary>
        public void NotifyDataSetChanged() => DataSetChanged?.Invoke();

        /// <summary>Call after inserting into the data. Visible inserted items appear-animate; survivors slide.</summary>
        public void NotifyItemRangeInserted(int index, int count) => ItemRangeInserted?.Invoke(index, count);

        public void NotifyItemInserted(int index) => NotifyItemRangeInserted(index, 1);

        /// <summary>Call after removing from the data. Visible removed items exit-animate; survivors slide.</summary>
        public void NotifyItemRangeRemoved(int index, int count) => ItemRangeRemoved?.Invoke(index, count);

        public void NotifyItemRemoved(int index) => NotifyItemRangeRemoved(index, 1);

        /// <summary>Call after moving an item. Identity is preserved: the same holder slides, no rebind.</summary>
        public void NotifyItemMoved(int fromIndex, int toIndex) => ItemMoved?.Invoke(fromIndex, toIndex);

        /// <summary>Call after mutating items in place. Visible items rebind without recycling.</summary>
        public void NotifyItemRangeChanged(int index, int count) => ItemRangeChanged?.Invoke(index, count);

        public void NotifyItemChanged(int index) => NotifyItemRangeChanged(index, 1);
    }

    /// <summary>
    /// Convenience base for the common single-view-type case: the holder type is the pool key.
    /// </summary>
    public abstract class Adapter<TVH, T> : Adapter<T> where TVH : CollectionItemViewHolder<T>
    {
        public sealed override Type GetViewType(int index) => typeof(TVH);

        public sealed override CollectionItemViewHolder<T> CreateViewHolder(Type viewType) => CreateViewHolder();

        protected abstract TVH CreateViewHolder();
    }
}
