using Headwind.VirtualCollectionView.Core;
using NUnit.Framework;

namespace Headwind.VirtualCollectionView.Tests
{
    public class AlignmentTests
    {
        // Heights 10, 20, 30 with preferred cross (width) 50; spacing 5;
        // padding 4; viewport 100x100 → available cross 92, content main 78.
        private static LinearLayoutManager MeasuredColumn(
            LinearCrossAlignment cross = LinearCrossAlignment.Stretch,
            MainAlignment main = MainAlignment.Start,
            float preferredCross = 50f)
        {
            var sizes = new VariableSizeProvider(
                new LayoutVector2(preferredCross, 10f),
                new LayoutVector2(preferredCross, 20f),
                new LayoutVector2(preferredCross, 30f));
            var layout = new ColumnLayoutManager
            {
                Spacing = 5f,
                Padding = 4f,
                CrossAlignment = cross,
                MainAlignment = main
            };
            layout.Measure(sizes.Count, sizes, new LayoutVector2(100f, 100f));
            return layout;
        }

        [Test]
        public void Linear_CrossStart_UsesPreferredSizeAtLeadingEdge()
        {
            var layout = MeasuredColumn(LinearCrossAlignment.Start);

            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(4f, 4f, 50f, 10f)));
        }

        [Test]
        public void Linear_CrossCenter_DistributesLeftoverEvenly()
        {
            var layout = MeasuredColumn(LinearCrossAlignment.Center);

            // 4 + (92 - 50) / 2 = 25.
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(25f, 4f, 50f, 10f)));
        }

        [Test]
        public void Linear_CrossEnd_PlacesAtTrailingEdge()
        {
            var layout = MeasuredColumn(LinearCrossAlignment.End);

            // 4 + (92 - 50) = 46.
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(46f, 4f, 50f, 10f)));
        }

        [Test]
        public void Linear_CrossPreferredLargerThanViewport_IsClamped()
        {
            var layout = MeasuredColumn(LinearCrossAlignment.Center, preferredCross: 200f);

            // Clamped to the available 92; no leftover to distribute.
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(4f, 4f, 92f, 10f)));
        }

        [Test]
        public void Linear_MainCenter_ShiftsWholeRunIntoSlack()
        {
            var layout = MeasuredColumn(main: MainAlignment.Center);

            // Slack = 100 - 78 = 22 → lead 11.
            Assert.That(layout.GetItemRect(0).Y, Is.EqualTo(15f));
            Assert.That(layout.GetItemRect(1).Y, Is.EqualTo(30f));
            // Content size is unchanged: nothing becomes scrollable.
            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(100f, 78f)));
        }

        [Test]
        public void Linear_MainEnd_VisibilityFollowsShiftedPositions()
        {
            var layout = MeasuredColumn(main: MainAlignment.End);

            // Lead 22: first item now spans y [26, 36).
            Assert.That(layout.GetItemRect(0).Y, Is.EqualTo(26f));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 0f, 100f, 12f)),
                Is.Empty);
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 0f, 100f, 100f)),
                Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Linear_MainAlignment_HasNoEffectWhenContentOverflows()
        {
            var sizes = new FixedSizeProvider(50f, 10f);
            var layout = new ColumnLayoutManager { Padding = 4f, MainAlignment = MainAlignment.End };
            layout.Measure(20, sizes, new LayoutVector2(100f, 100f));

            Assert.That(layout.GetItemRect(0).Y, Is.EqualTo(4f));
        }

        // Span 2; preferred cross (width) 30; heights 10, 20, 30, 15, 25;
        // padding 4; main spacing 5; cross spacing 6; viewport 100x100.
        // Equal share = (92 - 6) / 2 = 43 → cell = min(43, 30) = 30,
        // lane block = 30*2 + 6 = 66, leftover = 92 - 66 = 26.
        private static GridLayoutManager MeasuredGrid(
            GridCrossAlignment cross,
            MainAlignment main = MainAlignment.Start)
        {
            var sizes = new VariableSizeProvider(
                new LayoutVector2(30f, 10f),
                new LayoutVector2(30f, 20f),
                new LayoutVector2(30f, 30f),
                new LayoutVector2(30f, 15f),
                new LayoutVector2(30f, 25f));
            var layout = new GridLayoutManager(spanCount: 2)
            {
                Padding = 4f,
                MainSpacing = 5f,
                CrossSpacing = 6f,
                CrossAlignment = cross,
                MainAlignment = main
            };
            layout.Measure(sizes.Count, sizes, new LayoutVector2(100f, 100f));
            return layout;
        }

        [Test]
        public void Grid_CrossStart_UsesPreferredCellSize()
        {
            var layout = MeasuredGrid(GridCrossAlignment.Start);

            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(4f, 4f, 30f, 10f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(40f, 4f, 30f, 20f)));
        }

        [Test]
        public void Grid_CrossCenter_CentersLaneBlock()
        {
            var layout = MeasuredGrid(GridCrossAlignment.Center);

            // Block start = 4 + 26/2 = 17.
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(17f, 4f, 30f, 10f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(53f, 4f, 30f, 20f)));
        }

        [Test]
        public void Grid_CrossEnd_PushesLaneBlockToTrailingEdge()
        {
            var layout = MeasuredGrid(GridCrossAlignment.End);

            // Block start = 4 + 26 = 30.
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(30f, 4f, 30f, 10f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(66f, 4f, 30f, 20f)));
        }

        [Test]
        public void Grid_CrossStretch_IgnoresPreferredSize()
        {
            var layout = MeasuredGrid(GridCrossAlignment.Stretch);

            // Current default behavior: cell = equal share 43.
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(53f, 4f, 43f, 20f)));
        }

        [Test]
        public void Grid_CrossSpaceBetween_DistributesLeftoverIntoGutters()
        {
            var layout = MeasuredGrid(GridCrossAlignment.SpaceBetween);

            // Lane step = 30 + 6 + 26/(2-1) = 62: first lane hugs the leading
            // edge, last lane ends flush at 4 + 92 = 96.
            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(4f, 4f, 30f, 10f)));
            Assert.That(layout.GetItemRect(1), Is.EqualTo(new ItemRect(66f, 4f, 30f, 20f)));
        }

        [Test]
        public void Grid_CrossSpaceBetween_WithSingleSpan_DegradesToStart()
        {
            var sizes = new FixedSizeProvider(30f, 10f);
            var layout = new GridLayoutManager(spanCount: 1)
            {
                Padding = 4f,
                CrossAlignment = GridCrossAlignment.SpaceBetween
            };
            layout.Measure(3, sizes, new LayoutVector2(100f, 100f));

            Assert.That(layout.GetItemRect(0), Is.EqualTo(new ItemRect(4f, 4f, 30f, 10f)));
        }

        [Test]
        public void Grid_MainEnd_ShiftsRowsIntoSlack()
        {
            var layout = MeasuredGrid(GridCrossAlignment.Start, MainAlignment.End);

            // Content main = 93, slack = 7.
            Assert.That(layout.GetItemRect(0).Y, Is.EqualTo(11f));
            Assert.That(layout.ContentSize, Is.EqualTo(new LayoutVector2(100f, 93f)));
            Assert.That(
                TestUtil.VisibleIndices(layout, new ItemRect(0f, 0f, 100f, 100f)),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
        }
    }
}
