using System;
using System.Collections.Generic;

namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Placement rule that lays items in a single line along the main axis.
    /// </summary>
    /// <remarks>
    /// Item main-axis extents come from the size provider; visibility queries
    /// are O(log n) via binary search over the accumulated offsets.
    ///
    /// The cross axis never scrolls: with <see cref="LinearCrossAlignment.Stretch"/>
    /// (default) items fill the available cross space, otherwise each item
    /// keeps its preferred cross size (clamped to the available space) and is
    /// aligned within it. <see cref="MainAlignment"/> positions the whole run
    /// when the content is shorter than the viewport.
    ///
    /// Configure properties before the layout is measured (or invalidate the
    /// view afterwards).
    /// </remarks>
    public class LinearLayoutManager : LayoutManager
    {
        private readonly AxisHelper _axis;
        private float[] _starts = Array.Empty<float>();
        private float[] _ends = Array.Empty<float>();
        private ISizeProvider _sizes;
        private float _availableCross;

        public LinearLayoutManager(Orientation orientation)
        {
            _axis = new AxisHelper(orientation);
        }

        public Orientation Orientation => _axis.Orientation;

        /// <summary>Gap between consecutive items along the main axis.</summary>
        public float Spacing { get; set; }

        /// <summary>Uniform padding on all four sides of the content.</summary>
        public float Padding { get; set; }

        /// <summary>Content-block placement when shorter than the viewport.</summary>
        public MainAlignment MainAlignment { get; set; } = MainAlignment.Start;

        /// <summary>Item placement across the main axis.</summary>
        public LinearCrossAlignment CrossAlignment { get; set; } = LinearCrossAlignment.Stretch;

        public override bool CanScrollHorizontally => Orientation == Orientation.Horizontal;
        public override bool CanScrollVertically => Orientation == Orientation.Vertical;

        protected override void OnMeasure(int itemCount, ISizeProvider sizeProvider, LayoutVector2 viewportSize)
        {
            if (_starts.Length < itemCount)
            {
                _starts = new float[itemCount];
                _ends = new float[itemCount];
            }

            _sizes = sizeProvider;
            _availableCross = Math.Max(0f, _axis.Cross(viewportSize) - Padding * 2f);

            var pos = Padding;
            for (var i = 0; i < itemCount; i++)
            {
                if (i > 0)
                    pos += Spacing;
                _starts[i] = pos;
                pos += _axis.Main(sizeProvider.GetItemSize(i));
                _ends[i] = pos;
            }

            var contentMain = pos + Padding;

            var lead = AlignmentMath.Factor(MainAlignment) *
                       Math.Max(0f, _axis.Main(viewportSize) - contentMain);
            if (lead > 0f)
            {
                for (var i = 0; i < itemCount; i++)
                {
                    _starts[i] += lead;
                    _ends[i] += lead;
                }
            }

            // Cross extent tracks the viewport so this axis never scrolls.
            ContentSize = _axis.MakeVec(contentMain, _axis.Cross(viewportSize));
        }

        public override ItemRect GetItemRect(int index)
        {
            var mainSize = _ends[index] - _starts[index];
            if (CrossAlignment == LinearCrossAlignment.Stretch)
                return _axis.MakeRect(_starts[index], Padding, mainSize, _availableCross);

            var crossSize = Math.Min(_axis.Cross(_sizes.GetItemSize(index)), _availableCross);
            var crossPos = Padding + AlignmentMath.Factor(CrossAlignment) * (_availableCross - crossSize);
            return _axis.MakeRect(_starts[index], crossPos, mainSize, crossSize);
        }

        public override void GetVisibleIndices(in ItemRect viewport, List<int> result)
        {
            if (ItemCount == 0)
                return;

            var min = _axis.MainMin(viewport);
            var max = _axis.MainMax(viewport);

            for (var i = FirstIndexEndingAfter(min); i < ItemCount && _starts[i] < max; i++)
                result.Add(i);
        }

        /// <summary>Smallest index whose main-axis end is strictly past <paramref name="min"/>.</summary>
        private int FirstIndexEndingAfter(float min)
        {
            int lo = 0, hi = ItemCount;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (_ends[mid] > min)
                    hi = mid;
                else
                    lo = mid + 1;
            }

            return lo;
        }
    }
}
