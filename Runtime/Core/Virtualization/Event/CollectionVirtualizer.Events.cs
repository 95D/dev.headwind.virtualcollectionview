using System;
using System.Collections.Generic;

namespace Headwind.VirtualCollectionView.Core
{
    // Output surface of the virtualizer. The type doc lives on the main
    // partial; a second <summary> here would duplicate it in the doc XML.
    public sealed partial class CollectionVirtualizer
    {
        /// <summary>An item entered the (buffered) viewport: create/reuse a view, bind, place at the rect.</summary>
        public event Action<int, ItemRect, RealizeCause> ItemRealized;

        /// <summary>An item left the (buffered) viewport: recycle its view (after any exit effect when Removed).</summary>
        public event Action<int, VirtualizeCause> ItemVirtualized;

        /// <summary>A realized item's rect changed after a relayout: reposition its view, no rebind. Args: index, from, to.</summary>
        public event Action<int, ItemRect, ItemRect> ItemMoved;

        /// <summary>Realized items' adapter indices changed (insert/remove/move upstream). Bookkeeping only — no rebind.</summary>
        public event Action<IReadOnlyList<IndexShift>> ItemsShifted;

        /// <summary>A realized item's model changed in place: rebind the existing view.</summary>
        public event Action<int> ItemUpdated;

        /// <summary>Scroll offset changed (input, clamping, or ScrollToItem): retranslate the content container.</summary>
        public event Action ScrollOffsetChanged;
    }
}
