using System;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// Owner of one item view (a VisualElement subtree) that binds models of type <typeparamref name="T"/> to it.
    /// </summary>
    /// <remarks>
    /// Platform-layer concept: this is where the UI Toolkit dependency lives,
    /// by design.
    /// </remarks>
    public abstract class CollectionItemViewHolder<T>
    {
        protected CollectionItemViewHolder(VisualElement itemView)
        {
            ItemView = itemView ?? throw new ArgumentNullException(nameof(itemView));
            // Items live in absolute space and are placed via transform
            // (translate), never via left/top, so movement skips layout.
            ItemView.style.position = Position.Absolute;
            ItemView.style.left = 0f;
            ItemView.style.top = 0f;
            ItemView.usageHints |= UsageHints.DynamicTransform;
        }

        public VisualElement ItemView { get; }

        /// <summary>Adapter index currently bound to this holder, or -1 when pooled.</summary>
        public int BoundIndex { get; internal set; } = -1;

        /// <summary>Pool key this holder was created for. Set by the view.</summary>
        internal Type ViewType { get; set; }

        public abstract void Bind(T item);

        /// <summary>Called when the holder is returned to the pool. Release
        /// transient resources here (textures, subscriptions, ...).</summary>
        public virtual void OnRecycled()
        {
        }
    }

    /// <summary>
    /// Convenience holder for a subtype of the adapter's model.
    /// </summary>
    /// <remarks>
    /// The viewType/pool invariant guarantees this holder only ever receives
    /// <typeparamref name="TSub"/> instances, so the single cast here is safe
    /// and user code stays fully typed.
    /// </remarks>
    public abstract class CollectionItemViewHolder<T, TSub> : CollectionItemViewHolder<T> where TSub : T
    {
        protected CollectionItemViewHolder(VisualElement itemView) : base(itemView)
        {
        }

        public sealed override void Bind(T item) => Bind((TSub)item);

        protected abstract void Bind(TSub item);
    }
}
