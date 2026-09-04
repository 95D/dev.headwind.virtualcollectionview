using System;
using System.Collections.Generic;

namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Platform-agnostic core that decides which items are realized where.
    /// </summary>
    /// <remarks>
    /// Knows nothing about views: it consumes (item count, size provider,
    /// layout manager, viewport size, scroll offset) and emits
    /// realize/virtualize/move decisions as events keyed by item index.
    /// A platform binding (UI Toolkit, UGUI, Godot, ...) owns input, view
    /// creation, pooling, model binding, and animation.
    ///
    /// Granular data changes (<see cref="NotifyItemRangeInserted"/> etc.)
    /// preserve item identity: surviving realized items are index-remapped
    /// (no rebind) and repositioned via <see cref="ItemMoved"/>, and realize/
    /// virtualize events carry a cause so the platform can distinguish
    /// data-level appearance/disappearance from plain scrolling.
    ///
    /// The virtualizer has no concept of time. Events are timeless statements of
    /// the new truth; interpolation toward it is entirely the platform's job.
    ///
    /// All coordinates are in virtual content space (top-left origin, Y down).
    /// </remarks>
    public sealed partial class CollectionVirtualizer
    {
        private readonly Dictionary<int, ItemRect> _realized = new Dictionary<int, ItemRect>();
        private readonly HashSet<int> _visibleSet = new HashSet<int>();
        private readonly List<int> _visibleScratch = new List<int>();
        private readonly List<int> _removeScratch = new List<int>();
        private readonly List<IndexShift> _shiftScratch = new List<IndexShift>();
        private readonly List<KeyValuePair<int, ItemRect>> _remapScratch =
            new List<KeyValuePair<int, ItemRect>>();

        private LayoutManager _layout;
        private ISizeProvider _sizes;
        private int _itemCount;
        private LayoutVector2 _viewportSize;
        private LayoutVector2 _scrollOffset;
        private float _bufferPixels = 200f;

        // Set only for the duration of an insert refresh so realize events in
        // the inserted range can be tagged with RealizeCause.Inserted.
        private int _insertedStart = -1;
        private int _insertedCount;

        // Anchor captured before a granular data change: the first visible
        // item and where it sat relative to the scroll offset. After the
        // remeasure the offset is compensated so that item stays put on screen
        // ("keep watching what you were watching").
        private int _anchorIndex = -1;
        private LayoutVector2 _anchorLead;

        public LayoutManager LayoutManager => _layout;
        public int ItemCount => _itemCount;
        public LayoutVector2 ViewportSize => _viewportSize;
        public LayoutVector2 ScrollOffset => _scrollOffset;
        public LayoutVector2 ContentSize => _layout?.ContentSize ?? LayoutVector2.Zero;

        public LayoutVector2 MaxScrollOffset =>
            LayoutVector2.Max(LayoutVector2.Zero, ContentSize - _viewportSize);

        /// <summary>
        /// Offset change applied during the current relayout (anchor
        /// compensation and clamping). Valid while this refresh's events are
        /// dispatched, zero otherwise. Platforms compose it with
        /// <see cref="ItemMoved"/>: the visual start of a move is
        /// from + ScrollCompensation, so anchor-preserved items (whose move
        /// delta equals the compensation) stay perfectly still on screen.
        /// </summary>
        public LayoutVector2 ScrollCompensation { get; private set; }

        /// <summary>
        /// Overscan distance (in pixels) added around the viewport before the
        /// visibility query, so items are realized slightly before they become
        /// visible during scrolling.
        /// </summary>
        public float BufferPixels
        {
            get => _bufferPixels;
            set
            {
                _bufferPixels = Math.Max(0f, value);
                Refresh(measure: false);
            }
        }

        public void SetLayoutManager(LayoutManager layoutManager)
        {
            if (_layout == layoutManager)
                return;
            _layout = layoutManager;
            Refresh(measure: true);
        }

        public void SetDataSource(int itemCount, ISizeProvider sizeProvider)
        {
            _sizes = sizeProvider;
            NotifyDataSetChanged(itemCount);
        }

        /// <summary>
        /// Full data invalidation: every realized item is virtualized, layout
        /// is remeasured, and the visible set is re-realized — which guarantees
        /// every visible view is rebound against the new data. Item identity is
        /// lost; use the granular notifications when animations should run.
        /// </summary>
        public void NotifyDataSetChanged(int itemCount)
        {
            if (itemCount < 0)
                throw new ArgumentOutOfRangeException(nameof(itemCount));

            _itemCount = itemCount;
            VirtualizeAll(VirtualizeCause.Invalidated);
            Refresh(measure: true, realizeCause: RealizeCause.Invalidated);
        }

        /// <summary>
        /// Items were inserted at <paramref name="index"/>. Realized items at
        /// or after it are index-shifted (no rebind) and repositioned; newly
        /// visible items in the inserted range realize with
        /// <see cref="RealizeCause.Inserted"/>.
        /// </summary>
        public void NotifyItemRangeInserted(int index, int count)
        {
            if (index < 0 || index > _itemCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            CaptureAnchor();
            _itemCount += count;
            if (_anchorIndex >= index)
                _anchorIndex += count;
            RemapRealized(i => i >= index ? i + count : i);

            _insertedStart = index;
            _insertedCount = count;
            Refresh(measure: true);
            _insertedStart = -1;
            _insertedCount = 0;
        }

        /// <summary>
        /// Items were removed at <paramref name="index"/>. Realized items in
        /// the range virtualize with <see cref="VirtualizeCause.Removed"/>
        /// (exit-animation candidates); survivors are index-shifted and
        /// repositioned.
        /// </summary>
        public void NotifyItemRangeRemoved(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > _itemCount)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            CaptureAnchor();
            _itemCount -= count;
            if (_anchorIndex >= index + count)
                _anchorIndex -= count;
            else if (_anchorIndex >= index)
                _anchorIndex = _itemCount > 0 ? Math.Min(index, _itemCount - 1) : -1;

            _removeScratch.Clear();
            foreach (var realizedIndex in _realized.Keys)
            {
                if (realizedIndex >= index && realizedIndex < index + count)
                    _removeScratch.Add(realizedIndex);
            }

            for (var i = 0; i < _removeScratch.Count; i++)
            {
                _realized.Remove(_removeScratch[i]);
                ItemVirtualized?.Invoke(_removeScratch[i], VirtualizeCause.Removed);
            }

            RemapRealized(i => i >= index + count ? i - count : i);
            Refresh(measure: true);
        }

        /// <summary>
        /// An item moved from one index to another. Identity is preserved:
        /// affected realized items (including the moved one) are index-shifted
        /// without rebind and slide to their new rects via <see cref="ItemMoved"/>.
        /// </summary>
        public void NotifyItemMoved(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _itemCount)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= _itemCount)
                throw new ArgumentOutOfRangeException(nameof(toIndex));
            if (fromIndex == toIndex)
                return;

            // Anchoring on the moved item itself would make the viewport chase
            // it across the list; prefer the next visible item.
            CaptureAnchor(excludeIndex: fromIndex);
            if (_anchorIndex == fromIndex)
            {
                _anchorIndex = toIndex;
            }
            else if (_anchorIndex >= 0)
            {
                if (fromIndex < toIndex && _anchorIndex > fromIndex && _anchorIndex <= toIndex)
                    _anchorIndex--;
                else if (fromIndex > toIndex && _anchorIndex >= toIndex && _anchorIndex < fromIndex)
                    _anchorIndex++;
            }

            RemapRealized(i =>
            {
                if (i == fromIndex)
                    return toIndex;
                if (fromIndex < toIndex)
                    return i > fromIndex && i <= toIndex ? i - 1 : i;
                return i >= toIndex && i < fromIndex ? i + 1 : i;
            });
            Refresh(measure: true);
        }

        /// <summary>
        /// Item models changed in place (same identity, same position in the
        /// list). Realized items in the range rebind via <see cref="ItemUpdated"/>;
        /// a remeasure follows because sizes may have changed.
        /// </summary>
        public void NotifyItemRangeChanged(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > _itemCount)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            _removeScratch.Clear();
            foreach (var realizedIndex in _realized.Keys)
            {
                if (realizedIndex >= index && realizedIndex < index + count)
                    _removeScratch.Add(realizedIndex);
            }

            for (var i = 0; i < _removeScratch.Count; i++)
                ItemUpdated?.Invoke(_removeScratch[i]);

            // Sizes may have changed; keep the watched item still.
            CaptureAnchor();
            Refresh(measure: true);
        }

        public void SetViewportSize(LayoutVector2 size)
        {
            if (_viewportSize == size)
                return;
            _viewportSize = size;
            Refresh(measure: true);
        }

        /// <summary>Remeasure with current data (e.g. after layout parameters changed).</summary>
        public void InvalidateLayout() => Refresh(measure: true);

        public void ScrollBy(LayoutVector2 delta) => SetScrollOffset(_scrollOffset + delta);

        public void SetScrollOffset(LayoutVector2 offset)
        {
            if (ApplyOffset(offset))
                Refresh(measure: false);
        }

        /// <summary>Scrolls so the item's rect starts at the viewport origin (clamped).</summary>
        public void ScrollToItem(int index)
        {
            if (_layout == null || index < 0 || index >= _itemCount)
                return;
            if (ApplyOffset(_layout.GetItemRect(index).Position))
                Refresh(measure: false);
        }

        /// <summary>Clamps and stores the offset. Returns true if it changed.</summary>
        private bool ApplyOffset(LayoutVector2 offset)
        {
            var max = MaxScrollOffset;
            var clamped = LayoutVector2.Clamp(offset, LayoutVector2.Zero, max);
            if (_layout != null)
            {
                clamped = new LayoutVector2(
                    _layout.CanScrollHorizontally ? clamped.X : 0f,
                    _layout.CanScrollVertically ? clamped.Y : 0f);
            }

            if (clamped == _scrollOffset)
                return false;

            _scrollOffset = clamped;
            ScrollOffsetChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Records the first item visible in the (unbuffered) viewport and its
        /// position relative to the scroll offset. The following measure
        /// refresh compensates the offset so this item does not move on screen.
        /// </summary>
        private void CaptureAnchor(int excludeIndex = -1)
        {
            _anchorIndex = -1;
            if (_realized.Count == 0)
                return;

            var viewport = new ItemRect(
                _scrollOffset.X, _scrollOffset.Y, _viewportSize.X, _viewportSize.Y);

            var best = int.MaxValue;
            foreach (var kv in _realized)
            {
                if (kv.Key != excludeIndex && kv.Key < best && kv.Value.Overlaps(viewport))
                    best = kv.Key;
            }

            // Fall back to the excluded item when it is the only visible one.
            if (best == int.MaxValue && excludeIndex >= 0 &&
                _realized.TryGetValue(excludeIndex, out var excludedRect) &&
                excludedRect.Overlaps(viewport))
            {
                best = excludeIndex;
            }

            if (best == int.MaxValue)
                return;

            _anchorIndex = best;
            _anchorLead = _realized[best].Position - _scrollOffset;
        }

        /// <summary>
        /// Rewrites realized keys through <paramref name="map"/> and reports
        /// the changes as one collision-free <see cref="ItemsShifted"/> batch.
        /// Rect values ride along unchanged: they are the pre-relayout truth
        /// the following refresh diffs against to produce move geometry.
        /// </summary>
        private void RemapRealized(Func<int, int> map)
        {
            if (_realized.Count == 0)
                return;

            _shiftScratch.Clear();
            _remapScratch.Clear();
            foreach (var kv in _realized)
            {
                var to = map(kv.Key);
                _remapScratch.Add(new KeyValuePair<int, ItemRect>(to, kv.Value));
                if (to != kv.Key)
                    _shiftScratch.Add(new IndexShift(kv.Key, to));
            }

            if (_shiftScratch.Count == 0)
                return;

            _realized.Clear();
            for (var i = 0; i < _remapScratch.Count; i++)
                _realized.Add(_remapScratch[i].Key, _remapScratch[i].Value);

            ItemsShifted?.Invoke(_shiftScratch);
        }

        private void Refresh(bool measure, RealizeCause realizeCause = RealizeCause.ScrolledIn)
        {
            if (_layout == null || _sizes == null)
            {
                _anchorIndex = -1;
                VirtualizeAll(VirtualizeCause.Invalidated);
                return;
            }

            if (measure)
            {
                var offsetBefore = _scrollOffset;
                _layout.Measure(_itemCount, _sizes, _viewportSize);
                if (_anchorIndex >= 0 && _anchorIndex < _itemCount)
                    ApplyOffset(_layout.GetItemRect(_anchorIndex).Position - _anchorLead);
                else
                    ApplyOffset(_scrollOffset); // content may have shrunk under the offset
                _anchorIndex = -1;
                ScrollCompensation = _scrollOffset - offsetBefore;
            }

            var viewport = new ItemRect(
                _scrollOffset.X - _bufferPixels,
                _scrollOffset.Y - _bufferPixels,
                _viewportSize.X + _bufferPixels * 2f,
                _viewportSize.Y + _bufferPixels * 2f);

            _visibleScratch.Clear();
            _layout.GetVisibleIndices(viewport, _visibleScratch);

            _visibleSet.Clear();
            for (var i = 0; i < _visibleScratch.Count; i++)
                _visibleSet.Add(_visibleScratch[i]);

            _removeScratch.Clear();
            foreach (var index in _realized.Keys)
            {
                if (!_visibleSet.Contains(index))
                    _removeScratch.Add(index);
            }

            for (var i = 0; i < _removeScratch.Count; i++)
            {
                _realized.Remove(_removeScratch[i]);
                ItemVirtualized?.Invoke(_removeScratch[i], VirtualizeCause.ScrolledOut);
            }

            for (var i = 0; i < _visibleScratch.Count; i++)
            {
                var index = _visibleScratch[i];
                var rect = _layout.GetItemRect(index);
                if (_realized.TryGetValue(index, out var previous))
                {
                    if (measure && previous != rect)
                    {
                        _realized[index] = rect;
                        ItemMoved?.Invoke(index, previous, rect);
                    }
                }
                else
                {
                    _realized.Add(index, rect);
                    var cause = index >= _insertedStart && index < _insertedStart + _insertedCount
                        ? RealizeCause.Inserted
                        : realizeCause;
                    ItemRealized?.Invoke(index, rect, cause);
                }
            }

            ScrollCompensation = LayoutVector2.Zero;
        }

        private void VirtualizeAll(VirtualizeCause cause)
        {
            if (_realized.Count == 0)
                return;

            _removeScratch.Clear();
            _removeScratch.AddRange(_realized.Keys);
            _realized.Clear();
            for (var i = 0; i < _removeScratch.Count; i++)
                ItemVirtualized?.Invoke(_removeScratch[i], cause);
        }
    }
}
