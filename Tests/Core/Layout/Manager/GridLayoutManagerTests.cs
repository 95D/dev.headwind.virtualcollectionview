using Headwind.VirtualCollectionView.Core;
using NUnit.Framework;

namespace Headwind.VirtualCollectionView.Tests
{
    public class GridLayoutManagerTests
    {
        private static GridLayoutManager MeasuredGrid()
        {
            // Span 2; heights 10, 20, 30, 15, 25 (widths ignored by the grid);
            // padding 4; main spacing 5; cross spacing 6; viewport 100x100.
            var sizes = new VariableSizeProvider(
                new LayoutVector2(50f, 10f),
                new LayoutVector2(50f, 20f),
                new LayoutVector2(50f, 30f),
                new LayoutVector2(50f, 15f),
                new LayoutVector2(50f, 25f));
            var layout = new GridLayoutManager(spanCount: 2)
            {
                Padding = 4f,
                MainSpacing = 5f,
                CrossSpacing = 6f
            };
            layout.Measure(sizes.Count, sizes, new LayoutVector2(100f, 100f));
            return layout;
        }

        [Test]
        public void ContentSize_UsesMaxItemPerRow()
        {
            var layout = MeasuredGrid();

            // Rows: max(10,20)=20, max(30,15)=30, 25.
            // 4 + 20 + 5 + 30 + 5 + 25 + 4 = 93.
            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(100f, 93f)));
        }

        [Test]
        public void GetItemRect_ComputesLaneAndRowPlacement()
        {
            var layout = MeasuredGrid();

            // Cell width = (100 - 2*4 - 6) / 2 = 43.
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(4f, 4f, 43f, 10f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(53f, 4f, 43f, 20f)));
            Assert.That(layout.GetItemRect(2), Is.EqualTo(new ItemRect(4f, 29f, 43f, 30f)));
            Assert.That(layout.GetItemRect(3), Is.EqualTo(new ItemRect(53f, 29f, 43f, 15f)));
            // Last, partial row.
            Assert.That(layout.GetItemRect(4), Is.EqualTo(new ItemRect(4f, 64f, 43f, 25f)));
        }

        [Test]
        public void GetVisibleIndices_ReturnsWholeIntersectingRows()
        {
            var layout = MeasuredGrid();

            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 25f, 100f, 10f)),
                Is.EqualTo(new[] { 2, 3 }));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 0f, 100f, 100f)),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 60f, 100f, 100f)),
                Is.EqualTo(new[] { 4 }));
        }

        [Test]
        public void Horizontal_LaysRowsAlongX()
        {
            var sizes = new FixedSizeProvider(30f, 50f);
            var layout = new GridLayoutManager(spanCount: 2, orientation: Orientation.Horizontal);
            layout.Measure(4, sizes, new LayoutVector2(100f, 100f));

            // Two columns of two items; cell height = 100 / 2 = 50.
            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(60f, 100f)));
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(0f, 0f, 30f, 50f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(0f, 50f, 30f, 50f)));
            Assert.That(layout.GetItemRect(2), Is.EqualTo(new ItemRect(30f, 0f, 30f, 50f)));
            Assert.That(layout.CanScrollHorizontally, Is.True);
            Assert.That(layout.CanScrollVertically, Is.False);
        }

        [Test]
        public void SpanCountBelowOne_Throws()
        {
            Assert.That(() => new GridLayoutManager(0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void EmptyDataSet_ProducesPaddingOnlyContent()
        {
            var layout = new GridLayoutManager(3) { Padding = 4f };
            layout.Measure(0, new FixedSizeProvider(10f, 10f), new LayoutVector2(100f, 100f));

            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(100f, 8f)));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 0f, 100f, 100f)),
                Is.Empty);
        }
    }
}
