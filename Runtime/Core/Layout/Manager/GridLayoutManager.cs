using System;
using System.Collections.Generic;

namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Placement rule that lays items in fixed-span rows (or columns when horizontal) along the main axis.
    /// </summary>
    /// <remarks>
    /// Each row's main-axis extent is the max of its items' main sizes, with
    /// items keeping their own main size (start-aligned within the row).
    /// Visibility is O(log rows) via binary search.
    ///
    /// The cross axis never scrolls: with <see cref="GridCrossAlignment.Stretch"/>
    /// (default) the available cross space is divided between lanes, otherwise
    /// the cell takes the items' preferred cross size (clamped to an equal
    /// share) and the leftover is distributed around the lane block
    /// (Start/Center/End) or into the gutters between lanes (SpaceBetween).
    /// <see cref="MainAlignment"/> positions the row run when the content is
    /// shorter than the viewport.
    /// </remarks>
    public class GridLayoutManager : LayoutManager
    {
        private readonly AxisHelper _axis;
        private float[] _rowStarts = Array.Empty<float>();
        private float[] _rowEnds = Array.Empty<float>();
        private float _cellCross;
        private float _laneStep;
        private float _blockCrossStart;
        private int _rowCount;
        private ISizeProvider _sizes;

        public GridLayoutManager(int spanCount, Orientation orientation = Orientation.Vertical)
        {
            if (spanCount < 1)
                throw new ArgumentOutOfRangeException(nameof(spanCount), "Span count must be at least 1.");
            SpanCount = spanCount;
            _axis = new AxisHelper(orientation);
        }

        public int SpanCount { get; }
        public Orientation Orientation => _axis.Orientation;

        /// <summary>Gap between consecutive rows along the main axis.</summary>
        public float MainSpacing { get; set; }

        /// <summary>Gap between lanes across the main axis.</summary>
        public float CrossSpacing { get; set; }

        /// <summary>Uniform padding on all four sides of the content.</summary>
        public float Padding { get; set; }

        /// <summary>Content-block placement when shorter than the viewport.</summary>
        public MainAlignment MainAlignment { get; set; } = MainAlignment.Start;

        /// <summary>Lane-block placement across the main axis.</summary>
        public GridCrossAlignment CrossAlignment { get; set; } = GridCrossAlignment.Stretch;

        public override bool CanScrollHorizontally => Orientation == Orientation.Horizontal;
        public override bool CanScrollVertically => Orientation == Orientation.Vertical;

        protected override void OnMeasure(int itemCount, ISizeProvider sizeProvider, LayoutVector2 viewportSize)
        {
            _sizes = sizeProvider;
            _rowCount = (itemCount + SpanCount - 1) / SpanCount;
            if (_rowStarts.Length < _rowCount)
            {
                _rowStarts = new float[_rowCount];
                _rowEnds = new float[_rowCount];
            }

            var pos = Padding;
            var maxPreferredCross = 0f;
            for (var row = 0; row < _rowCount; row++)
            {
                if (row > 0)
                    pos += MainSpacing;
                _rowStarts[row] = pos;

                var rowMain = 0f;
                var end = Math.Min(itemCount, (row + 1) * SpanCount);
                for (var i = row * SpanCount; i < end; i++)
                {
                    var size = sizeProvider.GetItemSize(i);
                    rowMain = Math.Max(rowMain, _axis.Main(size));
                    maxPreferredCross = Math.Max(maxPreferredCross, _axis.Cross(size));
                }

                pos += rowMain;
                _rowEnds[row] = pos;
            }

            var contentMain = pos + Padding;

            var lead = AlignmentMath.Factor(MainAlignment) *
                       Math.Max(0f, _axis.Main(viewportSize) - contentMain);
            if (lead > 0f)
            {
                for (var row = 0; row < _rowCount; row++)
                {
                    _rowStarts[row] += lead;
                    _rowEnds[row] += lead;
                }
            }

            var availableCross = Math.Max(0f, _axis.Cross(viewportSize) - Padding * 2f);
            var equalShare = Math.Max(0f, (availableCross - CrossSpacing * (SpanCount - 1)) / SpanCount);
            if (CrossAlignment == GridCrossAlignment.Stretch)
            {
                _cellCross = equalShare;
                _laneStep = _cellCross + CrossSpacing;
                _blockCrossStart = Padding;
            }
            else
            {
                _cellCross = Math.Min(equalShare, maxPreferredCross);
                var leftover = Math.Max(0f,
                    availableCross - (_cellCross * SpanCount + CrossSpacing * (SpanCount - 1)));
                if (CrossAlignment == GridCrossAlignment.SpaceBetween && SpanCount > 1)
                {
                    _laneStep = _cellCross + CrossSpacing + leftover / (SpanCount - 1);
                    _blockCrossStart = Padding;
                }
                else
                {
                    // SpaceBetween with a single lane degrades to Start here
                    // (its alignment factor is 0).
                    _laneStep = _cellCross + CrossSpacing;
                    _blockCrossStart = Padding + AlignmentMath.Factor(CrossAlignment) * leftover;
                }
            }

            ContentSize = _axis.MakeVec(contentMain, _axis.Cross(viewportSize));
        }

        public override ItemRect GetItemRect(int index)
        {
            var row = index / SpanCount;
            var lane = index % SpanCount;
            var crossPos = _blockCrossStart + lane * _laneStep;
            var mainSize = Math.Min(_axis.Main(_sizes.GetItemSize(index)), _rowEnds[row] - _rowStarts[row]);
            return _axis.MakeRect(_rowStarts[row], crossPos, mainSize, _cellCross);
        }

        public override void GetVisibleIndices(in ItemRect viewport, List<int> result)
        {
            if (ItemCount == 0)
                return;

            var min = _axis.MainMin(viewport);
            var max = _axis.MainMax(viewport);

            for (var row = FirstRowEndingAfter(min); row < _rowCount && _rowStarts[row] < max; row++)
            {
                var end = Math.Min(ItemCount, (row + 1) * SpanCount);
                for (var i = row * SpanCount; i < end; i++)
                    result.Add(i);
            }
        }

        private int FirstRowEndingAfter(float min)
        {
            int lo = 0, hi = _rowCount;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (_rowEnds[mid] > min)
                    hi = mid;
                else
                    lo = mid + 1;
            }

            return lo;
        }
    }
}
