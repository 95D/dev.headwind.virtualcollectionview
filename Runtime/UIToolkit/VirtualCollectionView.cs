using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView
{
    /// <summary>
    /// UI Toolkit binding of the virtualization core.
    /// </summary>
    /// <remarks>
    /// Acts as the viewport: owns input (wheel, drag, inertia), the recycling
    /// pool, and view creation/binding, while the platform-agnostic
    /// <see cref="CollectionVirtualizer"/> decides what is realized where.
    ///
    /// Scrolling translates a single content container; realized items are
    /// placed once (via transform) at their virtual-space rect and only touched
    /// again when recycled or relaid out.
    /// </remarks>
    public class VirtualCollectionView<T> : VisualElement
    {
        private const float DragThresholdPixels = 4f;
        private const float MinFlingSpeed = 30f;

        private static readonly Action NoOp = () => { };

        private readonly CollectionVirtualizer _virtualizer = new CollectionVirtualizer();
        private readonly VisualElement _content;
        private readonly Dictionary<int, ViewHolder<T>> _active = new Dictionary<int, ViewHolder<T>>();
        private readonly Dictionary<Type, Stack<ViewHolder<T>>> _pool = new Dictionary<Type, Stack<ViewHolder<T>>>();
        private readonly HashSet<ViewHolder<T>> _disappearing = new HashSet<ViewHolder<T>>();
        private readonly List<KeyValuePair<int, ViewHolder<T>>> _shiftScratch =
            new List<KeyValuePair<int, ViewHolder<T>>>();

        private Adapter<T> _adapter;

        private int _pointerId = -1;
        private bool _dragging;
        private Vector2 _downPosition;
        private Vector3 _lastPointerPosition;
        private long _lastPointerTime;
        private Vector2 _velocity;
        private IVisualElementScheduledItem _fling;
        private long _lastFlingTime;

        public VirtualCollectionView()
        {
            style.overflow = Overflow.Hidden;

            _content = new VisualElement { name = "vcv-content", pickingMode = PickingMode.Ignore };
            _content.style.position = Position.Absolute;
            _content.style.left = 0f;
            _content.style.top = 0f;
            _content.usageHints |= UsageHints.DynamicTransform;
            hierarchy.Add(_content);

            _virtualizer.ItemRealized += OnItemRealized;
            _virtualizer.ItemVirtualized += OnItemVirtualized;
            _virtualizer.ItemMoved += OnItemMoved;
            _virtualizer.ItemsShifted += OnItemsShifted;
            _virtualizer.ItemUpdated += OnItemUpdated;
            _virtualizer.ScrollOffsetChanged += OnScrollOffsetChanged;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
        }

        /// <summary>The platform-agnostic virtualizer, exposed for advanced control.</summary>
        public CollectionVirtualizer Virtualizer => _virtualizer;

        public Adapter<T> Adapter => _adapter;

        /// <summary>
        /// Optional expression layer for insert/remove/move effects. Null (the
        /// default) snaps items instantly. The view keeps the invariants —
        /// deferred recycling, cause routing, interruption bookkeeping —
        /// regardless of the animator implementation.
        /// </summary>
        public IItemAnimator<T> ItemAnimator { get; set; }

        /// <summary>Wheel-scroll pixels per delta unit.</summary>
        public float WheelScrollSpeed { get; set; } = 20f;

        /// <summary>Exponential fling friction; higher stops sooner.</summary>
        public float FlingDeceleration { get; set; } = 4f;

        /// <summary>Overscan around the viewport, in pixels.</summary>
        public float BufferPixels
        {
            get => _virtualizer.BufferPixels;
            set => _virtualizer.BufferPixels = value;
        }

        public Vector2 ScrollOffset
        {
            get => ToVector2(_virtualizer.ScrollOffset);
            set => _virtualizer.SetScrollOffset(ToLayoutVector2(value));
        }

        public void SetLayoutManager(LayoutManager layoutManager) =>
            _virtualizer.SetLayoutManager(layoutManager);

        public void SetAdapter(Adapter<T> adapter)
        {
            if (_adapter == adapter)
                return;

            if (_adapter != null)
            {
                _adapter.DataSetChanged -= OnDataSetChanged;
                _adapter.ItemRangeInserted -= OnAdapterItemRangeInserted;
                _adapter.ItemRangeRemoved -= OnAdapterItemRangeRemoved;
                _adapter.ItemMoved -= OnAdapterItemMoved;
                _adapter.ItemRangeChanged -= OnAdapterItemRangeChanged;
            }

            // Recycle everything owned by the previous adapter, then drop it.
            FinishDisappearing();
            _virtualizer.SetDataSource(0, null);
            PurgePool();

            _adapter = adapter;
            if (_adapter != null)
            {
                _adapter.DataSetChanged += OnDataSetChanged;
                _adapter.ItemRangeInserted += OnAdapterItemRangeInserted;
                _adapter.ItemRangeRemoved += OnAdapterItemRangeRemoved;
                _adapter.ItemMoved += OnAdapterItemMoved;
                _adapter.ItemRangeChanged += OnAdapterItemRangeChanged;
                _virtualizer.SetDataSource(_adapter.Count, _adapter);
            }
        }

        /// <summary>Remeasure after layout-manager parameters changed.</summary>
        public void InvalidateLayout() => _virtualizer.InvalidateLayout();

        public void ScrollToItem(int index)
        {
            StopFling();
            _virtualizer.ScrollToItem(index);
        }

        private void OnDataSetChanged() => _virtualizer.NotifyDataSetChanged(_adapter.Count);

        private void OnAdapterItemRangeInserted(int index, int count) =>
            _virtualizer.NotifyItemRangeInserted(index, count);

        private void OnAdapterItemRangeRemoved(int index, int count) =>
            _virtualizer.NotifyItemRangeRemoved(index, count);

        private void OnAdapterItemMoved(int fromIndex, int toIndex) =>
            _virtualizer.NotifyItemMoved(fromIndex, toIndex);

        private void OnAdapterItemRangeChanged(int index, int count) =>
            _virtualizer.NotifyItemRangeChanged(index, count);

        private void OnGeometryChanged(GeometryChangedEvent evt) =>
            _virtualizer.SetViewportSize(ToLayoutVector2(contentRect.size));

        // ---- Virtualizer → views -------------------------------------------------

        private void OnItemRealized(int index, ItemRect rect, RealizeCause cause)
        {
            var viewType = _adapter.GetViewType(index);
            ViewHolder<T> holder;
            if (_pool.TryGetValue(viewType, out var stack) && stack.Count > 0)
            {
                holder = stack.Pop();
            }
            else
            {
                holder = _adapter.CreateViewHolder(viewType);
                holder.ViewType = viewType;
                _content.Add(holder.ItemView);
            }

            // Neutralize residual animation styles from a previous life
            // (a completed disappear leaves opacity at 0).
            ItemAnimator?.Cancel(holder);
            holder.ItemView.style.display = DisplayStyle.Flex;
            ApplyRect(holder, rect);
            holder.BoundIndex = index;
            holder.Bind(_adapter.GetItem(index));
            _active.Add(index, holder);

            if (cause == RealizeCause.Inserted && ItemAnimator != null)
                ItemAnimator.AnimateAppear(holder, rect, NoOp);
        }

        private void OnItemVirtualized(int index, VirtualizeCause cause)
        {
            if (!_active.TryGetValue(index, out var holder))
                return;
            _active.Remove(index);

            if (cause == VirtualizeCause.Removed && ItemAnimator != null)
            {
                // The holder stays on screen while it plays its exit; recycling
                // is deferred to the animator's completion.
                holder.BoundIndex = -1;
                _disappearing.Add(holder);
                ItemAnimator.AnimateDisappear(holder, () =>
                {
                    if (_disappearing.Remove(holder))
                        Recycle(holder);
                });
            }
            else
            {
                ItemAnimator?.Cancel(holder);
                Recycle(holder);
            }
        }

        private void OnItemMoved(int index, ItemRect from, ItemRect to)
        {
            if (!_active.TryGetValue(index, out var holder))
                return;

            if (ItemAnimator == null)
            {
                ApplyRect(holder, to);
                return;
            }

            // Compose with this relayout's anchor compensation: the content
            // container's translate already jumped by -compensation, so
            // shifting the item's content-space translate by +compensation
            // keeps it visually continuous. The animator then starts from the
            // current visual state, and anchor-preserved items (whose move
            // delta equals the compensation) resolve to a zero-length slide —
            // pinned on screen, exactly as the policy demands.
            var compensation = _virtualizer.ScrollCompensation;
            if (compensation != LayoutVector2.Zero)
                ShiftTranslate(holder.ItemView, compensation);

            ItemAnimator.AnimateMove(holder, from, to, NoOp);
        }

        private static void ShiftTranslate(VisualElement view, LayoutVector2 delta)
        {
            var translate = view.style.translate.value;
            view.style.translate = new Translate(
                translate.x.value + delta.X,
                translate.y.value + delta.Y);
        }

        private void OnItemsShifted(IReadOnlyList<IndexShift> shifts)
        {
            // Two-phase remap: the batch's From/To sets may overlap, so remove
            // every old key before adding any new one.
            _shiftScratch.Clear();
            for (var i = 0; i < shifts.Count; i++)
            {
                if (_active.TryGetValue(shifts[i].From, out var holder))
                {
                    _shiftScratch.Add(new KeyValuePair<int, ViewHolder<T>>(shifts[i].To, holder));
                    _active.Remove(shifts[i].From);
                }
            }

            for (var i = 0; i < _shiftScratch.Count; i++)
            {
                var entry = _shiftScratch[i];
                entry.Value.BoundIndex = entry.Key;
                _active.Add(entry.Key, entry.Value);
            }
        }

        private void OnItemUpdated(int index)
        {
            if (_active.TryGetValue(index, out var holder))
                holder.Bind(_adapter.GetItem(index));
        }

        private void Recycle(ViewHolder<T> holder)
        {
            holder.OnRecycled();
            holder.BoundIndex = -1;
            holder.ItemView.style.display = DisplayStyle.None;

            if (!_pool.TryGetValue(holder.ViewType, out var stack))
            {
                stack = new Stack<ViewHolder<T>>();
                _pool.Add(holder.ViewType, stack);
            }

            stack.Push(holder);
        }

        /// <summary>Force-completes exit animations so their holders return to the pool now.</summary>
        private void FinishDisappearing()
        {
            if (_disappearing.Count == 0)
                return;

            var pending = new List<ViewHolder<T>>(_disappearing);
            for (var i = 0; i < pending.Count; i++)
                ItemAnimator?.Cancel(pending[i]); // completion recycles and removes from the set

            // Safety net if the animator was swapped out mid-flight.
            for (var i = 0; i < pending.Count; i++)
            {
                if (_disappearing.Remove(pending[i]))
                    Recycle(pending[i]);
            }
        }

        private void OnScrollOffsetChanged()
        {
            var offset = _virtualizer.ScrollOffset;
            _content.style.translate = new Translate(-offset.X, -offset.Y);
        }

        private static void ApplyRect(ViewHolder<T> holder, ItemRect rect)
        {
            var view = holder.ItemView;
            view.style.translate = new Translate(rect.X, rect.Y);
            view.style.width = rect.Width;
            view.style.height = rect.Height;
        }

        private void PurgePool()
        {
            foreach (var stack in _pool.Values)
            {
                foreach (var holder in stack)
                    holder.ItemView.RemoveFromHierarchy();
            }

            _pool.Clear();
        }

        // ---- Input ----------------------------------------------------------

        private void OnWheel(WheelEvent evt)
        {
            StopFling();

            var layout = _virtualizer.LayoutManager;
            if (layout == null)
                return;

            var delta = Vector2.zero;
            if (layout.CanScrollVertically)
                delta.y = evt.delta.y * WheelScrollSpeed;
            if (layout.CanScrollHorizontally)
            {
                delta.x = evt.delta.x * WheelScrollSpeed;
                // Plain mouse wheels only emit Y; route it to the only
                // scrollable axis, like ScrollView does.
                if (!layout.CanScrollVertically && delta.x == 0f)
                    delta.x = evt.delta.y * WheelScrollSpeed;
            }

            _virtualizer.ScrollBy(ToLayoutVector2(delta));
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_pointerId != -1)
                return;

            StopFling();
            _pointerId = evt.pointerId;
            _dragging = false;
            _downPosition = evt.position;
            _lastPointerPosition = evt.position;
            _lastPointerTime = evt.timestamp;
            _velocity = Vector2.zero;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId)
                return;

            if (!_dragging)
            {
                if (((Vector2)evt.position - _downPosition).magnitude < DragThresholdPixels)
                    return;
                _dragging = true;
                this.CapturePointer(_pointerId);
            }

            var delta = evt.position - _lastPointerPosition;
            var dt = (evt.timestamp - _lastPointerTime) / 1000f;
            _lastPointerPosition = evt.position;
            _lastPointerTime = evt.timestamp;

            _virtualizer.ScrollBy(ToLayoutVector2(new Vector2(-delta.x, -delta.y)));

            if (dt > 0f)
            {
                var instantaneous = new Vector2(-delta.x / dt, -delta.y / dt);
                _velocity = Vector2.Lerp(_velocity, instantaneous, 0.4f);
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
                return;

            var fling = _dragging && _velocity.magnitude > MinFlingSpeed;
            if (this.HasPointerCapture(_pointerId))
                this.ReleasePointer(_pointerId);
            EndDrag();

            if (fling)
                StartFling();
        }

        private void EndDrag()
        {
            _pointerId = -1;
            _dragging = false;
        }

        private void StartFling()
        {
            _lastFlingTime = -1;
            if (_fling == null)
                _fling = schedule.Execute(FlingStep).Every(16);
            else
                _fling.Resume();
        }

        private void StopFling()
        {
            _fling?.Pause();
            _velocity = Vector2.zero;
        }

        private void FlingStep(TimerState timer)
        {
            if (_lastFlingTime < 0)
            {
                _lastFlingTime = timer.now;
                return;
            }

            var dt = (timer.now - _lastFlingTime) / 1000f;
            _lastFlingTime = timer.now;
            if (dt <= 0f)
                return;

            var before = _virtualizer.ScrollOffset;
            _virtualizer.ScrollBy(ToLayoutVector2(_velocity * dt));
            var after = _virtualizer.ScrollOffset;

            // Kill velocity on axes that hit the content edge (clamped).
            if (Mathf.Approximately(before.X, after.X))
                _velocity.x = 0f;
            if (Mathf.Approximately(before.Y, after.Y))
                _velocity.y = 0f;

            _velocity *= Mathf.Exp(-FlingDeceleration * dt);
            if (_velocity.magnitude < MinFlingSpeed)
                _fling.Pause();
        }

        // ---- Math boundary --------------------------------------------------

        private static LayoutVector2 ToLayoutVector2(Vector2 v) => new LayoutVector2(v.x, v.y);
        private static Vector2 ToVector2(LayoutVector2 v) => new Vector2(v.X, v.Y);
    }
}
