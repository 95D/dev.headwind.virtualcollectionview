using Headwind.VirtualCollectionView.Core;
using NUnit.Framework;

namespace Headwind.VirtualCollectionView.Tests
{
    public class CollectionVirtualizerTests
    {
        private CollectionVirtualizer _virtualizer;
        private VirtualizerRecorder _recorder;

        /// <summary>100 items of height 10 in a vertical list, viewport 50x100, no buffer.</summary>
        private void SetUpColumn(int itemCount = 100, float buffer = 0f)
        {
            _virtualizer = new CollectionVirtualizer { BufferPixels = buffer };
            _recorder = new VirtualizerRecorder(_virtualizer);
            _virtualizer.SetLayoutManager(new ColumnLayoutManager());
            _virtualizer.SetViewportSize(new LayoutVector2(50f, 100f));
            _virtualizer.SetDataSource(itemCount, new FixedSizeProvider(50f, 10f));
        }

        [Test]
        public void InitialRealize_CoversExactlyTheViewport()
        {
            SetUpColumn();

            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(0, 9)));
            Assert.That(_recorder.Rects[3], Is.EqualTo(new ItemRect(0f, 30f, 50f, 10f)));
        }

        [Test]
        public void Scroll_RealizesEnteringAndVirtualizesLeavingItems()
        {
            SetUpColumn();
            _recorder.ClearLog();

            _virtualizer.SetScrollOffset(new LayoutVector2(0f, 20f));

            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(2, 11)));
            Assert.That(_recorder.Virtualized, Is.EquivalentTo(new[] { 0, 1 }));
            Assert.That(_recorder.Realized, Is.EquivalentTo(new[] { 10, 11 }));
            Assert.That(_recorder.OffsetChangedCount, Is.EqualTo(1));
        }

        [Test]
        public void Scroll_ByLessThanOneItem_KeepsOverlappingItemsRealized()
        {
            SetUpColumn();
            _recorder.ClearLog();

            _virtualizer.ScrollBy(new LayoutVector2(0f, 5f));

            // Item 0 still overlaps [5, 105); item 10 enters.
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(0, 10)));
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Realized, Is.EqualTo(new[] { 10 }));
        }

        [Test]
        public void ScrollOffset_IsClampedToContentBounds()
        {
            SetUpColumn();

            _virtualizer.SetScrollOffset(new LayoutVector2(500f, 99999f));

            // Content is 50x1000, viewport 50x100 → max offset (0, 900);
            // the horizontal axis is not scrollable in a column layout.
            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 900f)));
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(90, 99)));

            _virtualizer.SetScrollOffset(new LayoutVector2(0f, -50f));
            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(LayoutVector2.Zero));
        }

        [Test]
        public void Buffer_RealizesItemsAroundTheViewport()
        {
            SetUpColumn(buffer: 25f);

            // Window is [-25, 125): items 0..12.
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(0, 12)));
        }

        [Test]
        public void NotifyDataSetChanged_VirtualizesEverythingThenRerealizes()
        {
            SetUpColumn();
            _recorder.ClearLog();

            _virtualizer.NotifyDataSetChanged(3);

            // A full rebind is guaranteed: every previously realized item is
            // virtualized, then the (new) visible set is realized again.
            Assert.That(_recorder.Virtualized, Is.EquivalentTo(TestUtil.Range(0, 9)));
            Assert.That(_recorder.Realized, Is.EquivalentTo(TestUtil.Range(0, 2)));
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(0, 2)));
        }

        [Test]
        public void ShrinkingContent_ClampsOffsetAndNotifies()
        {
            SetUpColumn();
            _virtualizer.SetScrollOffset(new LayoutVector2(0f, 900f));
            _recorder.ClearLog();

            _virtualizer.NotifyDataSetChanged(20);

            // Content is now 200 tall → max offset (0, 100).
            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 100f)));
            Assert.That(_recorder.OffsetChangedCount, Is.EqualTo(1));
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(10, 19)));
        }

        [Test]
        public void ViewportResize_MovesSurvivingItemsWithoutRerealizing()
        {
            SetUpColumn();
            _recorder.ClearLog();

            _virtualizer.SetViewportSize(new LayoutVector2(80f, 100f));

            // The cross-axis stretch changes every realized rect, but the
            // views survive: moved, not virtualized/realized.
            Assert.That(_recorder.Realized, Is.Empty);
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Moved, Is.EquivalentTo(TestUtil.Range(0, 9)));
            Assert.That(_recorder.Rects[0].Width, Is.EqualTo(80f));
        }

        [Test]
        public void ScrollToItem_BringsItemToViewportStart()
        {
            SetUpColumn();

            _virtualizer.ScrollToItem(50);

            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 500f)));
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(50, 59)));
        }

        [Test]
        public void ScrollToItem_OutOfRange_IsIgnored()
        {
            SetUpColumn();

            _virtualizer.ScrollToItem(-1);
            _virtualizer.ScrollToItem(100);

            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(LayoutVector2.Zero));
        }

        [Test]
        public void SwappingLayoutManager_RepositionsWithoutLosingData()
        {
            SetUpColumn(itemCount: 8);
            _recorder.ClearLog();

            _virtualizer.SetLayoutManager(new GridLayoutManager(2));

            // 4 rows of 10px in a 100px viewport: everything visible, and the
            // still-realized items are repositioned into grid cells.
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(0, 7)));
            Assert.That(_recorder.Rects[1], Is.EqualTo(new ItemRect(25f, 0f, 25f, 10f)));
            Assert.That(_recorder.Rects[2], Is.EqualTo(new ItemRect(0f, 10f, 25f, 10f)));
        }

        [Test]
        public void HorizontalLayout_ScrollsOnXOnly()
        {
            _virtualizer = new CollectionVirtualizer { BufferPixels = 0f };
            _recorder = new VirtualizerRecorder(_virtualizer);
            _virtualizer.SetLayoutManager(new RowLayoutManager());
            _virtualizer.SetViewportSize(new LayoutVector2(100f, 50f));
            _virtualizer.SetDataSource(100, new FixedSizeProvider(10f, 50f));

            _virtualizer.SetScrollOffset(new LayoutVector2(20f, 500f));

            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(20f, 0f)));
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(2, 11)));
        }

        [Test]
        public void EmptyDataSet_RealizesNothing()
        {
            SetUpColumn(itemCount: 0);

            Assert.That(_recorder.Active, Is.Empty);
            Assert.That(_virtualizer.MaxScrollOffset, Is.EqualTo(LayoutVector2.Zero));
        }
    }
}
