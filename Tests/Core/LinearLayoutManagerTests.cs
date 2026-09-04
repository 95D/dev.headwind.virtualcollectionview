using Headwind.VirtualCollectionView.Core;
using NUnit.Framework;

namespace Headwind.VirtualCollectionView.Tests
{
    public class LinearLayoutManagerTests
    {
        private static LinearLayoutManager MeasuredColumn(out VariableSizeProvider sizes)
        {
            // Heights 10, 20, 30; spacing 5; padding 4; viewport 100x100.
            sizes = new VariableSizeProvider(
                new LayoutVector2(50f, 10f),
                new LayoutVector2(50f, 20f),
                new LayoutVector2(50f, 30f));
            var layout = new ColumnLayoutManager { Spacing = 5f, Padding = 4f };
            layout.Measure(sizes.Count, sizes, new LayoutVector2(100f, 100f));
            return layout;
        }

        [Test]
        public void ContentSize_AccumulatesSizesSpacingAndPadding()
        {
            var layout = MeasuredColumn(out _);

            // 4 + 10 + 5 + 20 + 5 + 30 + 4 = 78 on the main axis,
            // cross tracks the viewport so it never scrolls.
            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(100f, 78f)));
        }

        [Test]
        public void GetItemRect_StretchesCrossAxisInsidePadding()
        {
            var layout = MeasuredColumn(out _);

            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(4f, 4f, 92f, 10f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(4f, 19f, 92f, 20f)));
            Assert.That(layout.GetItemRect(2), Is.EqualTo(new ItemRect(4f, 44f, 92f, 30f)));
        }

        [Test]
        public void GetVisibleIndices_ReturnsOnlyIntersectingItems()
        {
            var layout = MeasuredColumn(out _);

            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 19f, 100f, 20f)),
                Is.EqualTo(new[] { 1 }));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 0f, 100f, 100f)),
                Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void GetVisibleIndices_SpacingGapContainsNoItems()
        {
            var layout = MeasuredColumn(out _);

            // Item 0 ends at y=14, item 1 starts at y=19; the window [14, 19)
            // lies entirely inside the spacing gap.
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 14f, 100f, 5f)),
                Is.Empty);
        }

        [Test]
        public void GetVisibleIndices_ViewportBeyondContent_IsEmpty()
        {
            var layout = MeasuredColumn(out _);

            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 1000f, 100f, 100f)),
                Is.Empty);
        }

        [Test]
        public void Horizontal_UsesXAsMainAxis()
        {
            var sizes = new VariableSizeProvider(
                new LayoutVector2(10f, 50f),
                new LayoutVector2(20f, 50f),
                new LayoutVector2(30f, 50f));
            var layout = new RowLayoutManager { Spacing = 5f, Padding = 4f };
            layout.Measure(sizes.Count, sizes, new LayoutVector2(100f, 100f));

            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(78f, 100f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(19f, 4f, 20f, 92f)));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(19f, 0f, 20f, 100f)),
                Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void ScrollCapabilities_FollowOrientation()
        {
            Assert.That(new ColumnLayoutManager().CanScrollVertically, Is.True);
            Assert.That(new ColumnLayoutManager().CanScrollHorizontally, Is.False);
            Assert.That(new RowLayoutManager().CanScrollHorizontally, Is.True);
            Assert.That(new RowLayoutManager().CanScrollVertically, Is.False);
        }

        [Test]
        public void EmptyDataSet_ProducesPaddingOnlyContent()
        {
            var layout = new ColumnLayoutManager { Padding = 4f };
            layout.Measure(0, new FixedSizeProvider(10f, 10f), new LayoutVector2(100f, 100f));

            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(100f, 8f)));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 0f, 100f, 100f)),
                Is.Empty);
        }

        [Test]
        public void Remeasure_WithLargerCount_GrowsBuffers()
        {
            var layout = new ColumnLayoutManager();
            layout.Measure(2, new FixedSizeProvider(10f, 10f), new LayoutVector2(100f, 100f));
            layout.Measure(1000, new FixedSizeProvider(10f, 10f), new LayoutVector2(100f, 100f));

            Assert.That(layout.GetItemRect(999).Y, Is.EqualTo(9990f));
            Assert.That(layout.ContentSize.Y, Is.EqualTo(10000f));
        }
    }
}
