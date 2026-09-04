using Headwind.VirtualCollectionView.Core;
using NUnit.Framework;

namespace Headwind.VirtualCollectionView.Tests
{
    /// <summary>
    /// Granular data-change notifications: index remapping without rebind,
    /// cause tagging, and move geometry — the core contract the platform-side
    /// animation system is built on.
    /// </summary>
    public class GranularChangeTests
    {
        private CollectionVirtualizer _virtualizer;
        private VirtualizerRecorder _recorder;

        /// <summary>100 items of height 10 in a vertical list, viewport 50x100, no buffer → realized 0..9.</summary>
        private void SetUpColumn()
        {
            _virtualizer = new CollectionVirtualizer { BufferPixels = 0f };
            _recorder = new VirtualizerRecorder(_virtualizer);
            _virtualizer.SetLayoutManager(new ColumnLayoutManager());
            _virtualizer.SetViewportSize(new LayoutVector2(50f, 100f));
            _virtualizer.SetDataSource(100, new FixedSizeProvider(50f, 10f));
            _recorder.ClearLog();
        }

        [Test]
        public void Insert_WithinViewport_TagsInsertedAndShiftsSurvivors()
        {
            SetUpColumn();

            _virtualizer.NotifyItemRangeInserted(3, 2);

            Assert.That(_virtualizer.ItemCount, Is.EqualTo(102));
            // Realized 3..9 shifted to 5..11 in one collision-free batch.
            Assert.That(_recorder.Shifts, Is.EquivalentTo(new[]
            {
                new IndexShift(3, 5), new IndexShift(4, 6), new IndexShift(5, 7),
                new IndexShift(6, 8), new IndexShift(7, 9), new IndexShift(8, 10),
                new IndexShift(9, 11)
            }));
            // The two shifted past the viewport leave as plain scroll-outs.
            Assert.That(_recorder.Virtualized, Is.EquivalentTo(new[] { 10, 11 }));
            Assert.That(_recorder.VirtualizeCauses[10], Is.EqualTo(VirtualizeCause.ScrolledOut));
            // The inserted items appear with the animatable cause.
            Assert.That(_recorder.Realized, Is.EquivalentTo(new[] { 3, 4 }));
            Assert.That(_recorder.RealizeCauses[3], Is.EqualTo(RealizeCause.Inserted));
            Assert.That(_recorder.RealizeCauses[4], Is.EqualTo(RealizeCause.Inserted));
            // Survivors slide down with exact from→to geometry, no rebind.
            Assert.That(_recorder.Moved, Is.EquivalentTo(new[] { 5, 6, 7, 8, 9 }));
            Assert.That(_recorder.MovedFrom[5], Is.EqualTo(new ItemRect(0f, 30f, 50f, 10f)));
            Assert.That(_recorder.Rects[5], Is.EqualTo(new ItemRect(0f, 50f, 50f, 10f)));
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(0, 9)));
        }

        [Test]
        public void Insert_AboveViewport_KeepsWatchedItemsStill()
        {
            SetUpColumn();
            _virtualizer.SetScrollOffset(new LayoutVector2(0f, 500f)); // watching 50..59
            _recorder.ClearLog();

            _virtualizer.NotifyItemRangeInserted(0, 2);

            // Anchor policy: the offset compensates by the inserted extent so
            // the watched items (old 50..59, now 52..61) do not change at all —
            // no realize, no virtualize, identity preserved via shifts.
            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 520f)));
            Assert.That(_recorder.Realized, Is.Empty);
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(52, 61)));
            // Content-space moves all equal the compensation → the platform
            // composes them to zero visual motion.
            Assert.That(_recorder.Moved, Is.EquivalentTo(TestUtil.Range(52, 61)));
            Assert.That(_recorder.LastMoveCompensation, Is.EqualTo(new LayoutVector2(0f, 20f)));
            Assert.That(_recorder.Rects[52].Y - _recorder.MovedFrom[52].Y, Is.EqualTo(20f));
        }

        [Test]
        public void Remove_AboveViewport_KeepsWatchedItemsStill()
        {
            SetUpColumn();
            _virtualizer.SetScrollOffset(new LayoutVector2(0f, 500f)); // watching 50..59
            _recorder.ClearLog();

            _virtualizer.NotifyItemRangeRemoved(0, 3);

            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 470f)));
            Assert.That(_recorder.Realized, Is.Empty);
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(47, 56)));
            Assert.That(_recorder.LastMoveCompensation, Is.EqualTo(new LayoutVector2(0f, -30f)));
        }

        [Test]
        public void Move_AnchorItemAway_ViewStaysOnRemainingItems()
        {
            SetUpColumn();
            _virtualizer.SetScrollOffset(new LayoutVector2(0f, 500f)); // watching 50..59
            _recorder.ClearLog();

            _virtualizer.NotifyItemMoved(50, 0);

            // The viewport does not chase the moved item: the anchor falls to
            // the next visible item and the offset stays put.
            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 500f)));
            Assert.That(_recorder.Shifts, Is.EquivalentTo(new[] { new IndexShift(50, 0) }));
            // The moved holder leaves the window as a plain scroll-out at its
            // new index; the item entering from below is a plain realize.
            Assert.That(_recorder.Virtualized, Is.EquivalentTo(new[] { 0 }));
            Assert.That(_recorder.VirtualizeCauses[0], Is.EqualTo(VirtualizeCause.ScrolledOut));
            Assert.That(_recorder.Realized, Is.EquivalentTo(new[] { 50 }));
            Assert.That(_recorder.RealizeCauses[50], Is.EqualTo(RealizeCause.ScrolledIn));
        }

        [Test]
        public void Changed_SizeGrowthAboveViewport_KeepsWatchedItemsStill()
        {
            var sizes = new MutableSizeProvider();
            _virtualizer = new CollectionVirtualizer { BufferPixels = 0f };
            _recorder = new VirtualizerRecorder(_virtualizer);
            _virtualizer.SetLayoutManager(new ColumnLayoutManager());
            _virtualizer.SetViewportSize(new LayoutVector2(50f, 100f));
            _virtualizer.SetDataSource(100, sizes);
            _virtualizer.SetScrollOffset(new LayoutVector2(0f, 500f)); // watching 50..59
            _recorder.ClearLog();

            sizes.Heights[10] = 30f; // item above the viewport grows by 20
            _virtualizer.NotifyItemRangeChanged(10, 1);

            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 520f)));
            Assert.That(_recorder.Updated, Is.Empty); // item 10 is not realized
            Assert.That(_recorder.Realized, Is.Empty);
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(50, 59)));
            Assert.That(_recorder.LastMoveCompensation, Is.EqualTo(new LayoutVector2(0f, 20f)));
        }

        private sealed class MutableSizeProvider : ISizeProvider
        {
            public readonly System.Collections.Generic.Dictionary<int, float> Heights =
                new System.Collections.Generic.Dictionary<int, float>();

            public LayoutVector2 GetItemSize(int index) =>
                new LayoutVector2(50f, Heights.TryGetValue(index, out var height) ? height : 10f);
        }

        [Test]
        public void Remove_RealizedRange_TagsRemovedAndPullsUpSuccessors()
        {
            SetUpColumn();

            _virtualizer.NotifyItemRangeRemoved(2, 3);

            Assert.That(_virtualizer.ItemCount, Is.EqualTo(97));
            // The removed visible items are exit-animation candidates.
            Assert.That(_recorder.Virtualized, Is.EquivalentTo(new[] { 2, 3, 4 }));
            Assert.That(_recorder.VirtualizeCauses[2], Is.EqualTo(VirtualizeCause.Removed));
            Assert.That(_recorder.VirtualizeCauses[4], Is.EqualTo(VirtualizeCause.Removed));
            // Survivors 5..9 become 2..6 and slide up.
            Assert.That(_recorder.Shifts, Is.EquivalentTo(new[]
            {
                new IndexShift(5, 2), new IndexShift(6, 3), new IndexShift(7, 4),
                new IndexShift(8, 5), new IndexShift(9, 6)
            }));
            Assert.That(_recorder.Moved, Is.EquivalentTo(TestUtil.Range(2, 6)));
            Assert.That(_recorder.MovedFrom[2], Is.EqualTo(new ItemRect(0f, 50f, 50f, 10f)));
            Assert.That(_recorder.Rects[2], Is.EqualTo(new ItemRect(0f, 20f, 50f, 10f)));
            // Items pulled into the window from below are plain realizes.
            Assert.That(_recorder.Realized, Is.EquivalentTo(new[] { 7, 8, 9 }));
            Assert.That(_recorder.RealizeCauses[7], Is.EqualTo(RealizeCause.ScrolledIn));
        }

        [Test]
        public void Move_Forward_PreservesIdentityAndSlidesEveryAffectedItem()
        {
            SetUpColumn();

            _virtualizer.NotifyItemMoved(2, 7);

            Assert.That(_recorder.Shifts, Is.EquivalentTo(new[]
            {
                new IndexShift(2, 7), new IndexShift(3, 2), new IndexShift(4, 3),
                new IndexShift(5, 4), new IndexShift(6, 5), new IndexShift(7, 6)
            }));
            // Nothing appears, disappears, or rebinds — pure geometry.
            Assert.That(_recorder.Realized, Is.Empty);
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Updated, Is.Empty);
            Assert.That(_recorder.Moved, Is.EquivalentTo(TestUtil.Range(2, 7)));
            // The moved holder slides from its old slot to the new one.
            Assert.That(_recorder.MovedFrom[7], Is.EqualTo(new ItemRect(0f, 20f, 50f, 10f)));
            Assert.That(_recorder.Rects[7], Is.EqualTo(new ItemRect(0f, 70f, 50f, 10f)));
        }

        [Test]
        public void Move_Backward_ShiftsTheInterveningRangeUp()
        {
            SetUpColumn();

            _virtualizer.NotifyItemMoved(7, 2);

            Assert.That(_recorder.Shifts, Is.EquivalentTo(new[]
            {
                new IndexShift(7, 2), new IndexShift(2, 3), new IndexShift(3, 4),
                new IndexShift(4, 5), new IndexShift(5, 6), new IndexShift(6, 7)
            }));
            Assert.That(_recorder.MovedFrom[2], Is.EqualTo(new ItemRect(0f, 70f, 50f, 10f)));
            Assert.That(_recorder.Rects[2], Is.EqualTo(new ItemRect(0f, 20f, 50f, 10f)));
        }

        [Test]
        public void RangeChanged_RebindsRealizedItemsInPlace()
        {
            SetUpColumn();

            _virtualizer.NotifyItemRangeChanged(8, 4); // realized ∩ [8, 12) = {8, 9}

            Assert.That(_recorder.Updated, Is.EquivalentTo(new[] { 8, 9 }));
            Assert.That(_recorder.Shifts, Is.Empty);
            Assert.That(_recorder.Realized, Is.Empty);
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Moved, Is.Empty); // sizes unchanged
        }

        [Test]
        public void Insert_OutsideViewport_EmitsNoViewEvents()
        {
            SetUpColumn();

            _virtualizer.NotifyItemRangeInserted(100, 5);

            Assert.That(_virtualizer.ItemCount, Is.EqualTo(105));
            Assert.That(_recorder.Shifts, Is.Empty);
            Assert.That(_recorder.Realized, Is.Empty);
            Assert.That(_recorder.Virtualized, Is.Empty);
            Assert.That(_recorder.Moved, Is.Empty);
            Assert.That(_virtualizer.ContentSize.Y, Is.EqualTo(1050f));
        }

        [Test]
        public void FullInvalidation_UsesInvalidatedCause()
        {
            SetUpColumn();

            _virtualizer.NotifyDataSetChanged(50);

            Assert.That(_recorder.VirtualizeCauses[0], Is.EqualTo(VirtualizeCause.Invalidated));
            Assert.That(_recorder.RealizeCauses[0], Is.EqualTo(RealizeCause.Invalidated));
        }

        [Test]
        public void Remove_ShrinkingPastOffset_ClampsAndRefreshes()
        {
            SetUpColumn();
            _virtualizer.SetScrollOffset(new LayoutVector2(0f, 900f)); // realized 90..99
            _recorder.ClearLog();

            _virtualizer.NotifyItemRangeRemoved(20, 60); // content 1000 → 400, max offset 300

            Assert.That(_virtualizer.ScrollOffset, Is.EqualTo(new LayoutVector2(0f, 300f)));
            Assert.That(_recorder.Active, Is.EqualTo(TestUtil.Range(30, 39)));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            SetUpColumn();

            Assert.That(() => _virtualizer.NotifyItemRangeInserted(-1, 1),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _virtualizer.NotifyItemRangeInserted(101, 1),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _virtualizer.NotifyItemRangeRemoved(95, 10),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _virtualizer.NotifyItemMoved(0, 100),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => _virtualizer.NotifyItemRangeChanged(99, 2),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
