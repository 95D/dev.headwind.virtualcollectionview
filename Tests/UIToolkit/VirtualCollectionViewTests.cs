using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView.Tests.UIToolkit
{
    /// <summary>
    /// View-layer mechanics driven without a live panel.
    /// </summary>
    /// <remarks>
    /// The virtualizer's viewport is set directly (no GeometryChangedEvent),
    /// so realize/recycle/pooling and adapter-notification routing run exactly
    /// as in play mode. Input (wheel/drag/fling) and animator timing need a
    /// live panel and are intentionally not covered here.
    /// </remarks>
    public class VirtualCollectionViewTests
    {
        // Viewport 100×100, items 100×20, no buffer → exactly items 0..4 visible.
        private static VirtualCollectionView<string> NewView(StringAdapter adapter)
        {
            var view = new VirtualCollectionView<string> { BufferPixels = 0f };
            view.SetLayoutManager(new ColumnLayoutManager());
            view.SetAdapter(adapter);
            view.Virtualizer.SetViewportSize(new LayoutVector2(100f, 100f));
            return view;
        }

        [Test]
        public void RealizesOnlyTheVisibleRange()
        {
            var adapter = new StringAdapter(100);
            NewView(adapter);

            Assert.That(adapter.CreatedHolders, Has.Count.EqualTo(5));
            CollectionAssert.AreEquivalent(
                new[] { "item0", "item1", "item2", "item3", "item4" },
                adapter.CreatedHolders.Select(h => h.Bound.Last()));
            CollectionAssert.AreEquivalent(
                new[] { 0, 1, 2, 3, 4 },
                adapter.CreatedHolders.Select(h => h.BoundIndex));
        }

        [Test]
        public void Scrolling_RecyclesAndReusesHolders()
        {
            var adapter = new StringAdapter(100);
            var view = NewView(adapter);

            view.ScrollOffset = new Vector2(0f, 200f);

            Assert.That(adapter.CreatedHolders, Has.Count.EqualTo(5),
                "scrolled-in items must reuse the recycled holders, not create new ones");
            CollectionAssert.AreEquivalent(
                new[] { "item10", "item11", "item12", "item13", "item14" },
                adapter.CreatedHolders.Select(h => h.Bound.Last()));
            Assert.That(adapter.CreatedHolders.All(h => h.RecycledCount >= 1), Is.True);
        }

        [Test]
        public void RecycledView_IsHiddenNotDetached()
        {
            var adapter = new StringAdapter(100);
            var view = NewView(adapter);
            var content = view.Q<VisualElement>("vcv-content");

            // Shrink the viewport so 3 of the 5 holders go back to the pool.
            view.Virtualizer.SetViewportSize(new LayoutVector2(100f, 40f));

            Assert.That(adapter.CreatedHolders, Has.Count.EqualTo(5));
            Assert.That(adapter.CreatedHolders
                    .Count(h => h.ItemView.style.display.value == DisplayStyle.Flex),
                Is.EqualTo(2));
            Assert.That(adapter.CreatedHolders
                    .Count(h => h.ItemView.style.display.value == DisplayStyle.None),
                Is.EqualTo(3));
            Assert.That(adapter.CreatedHolders.All(h => h.ItemView.parent == content), Is.True,
                "pooled views stay attached to avoid attach/detach churn");
        }

        [Test]
        public void ItemChanged_RebindsInPlaceWithoutRecycling()
        {
            var adapter = new StringAdapter(100);
            NewView(adapter);
            var holder = adapter.HolderOf("item2");

            adapter.Set(2, "updated");

            Assert.That(holder.Bound.Last(), Is.EqualTo("updated"));
            Assert.That(holder.RecycledCount, Is.EqualTo(0));
            Assert.That(adapter.CreatedHolders, Has.Count.EqualTo(5));
        }

        [Test]
        public void InsertBeforeVisibleRange_ShiftsIndicesWithoutRebinding()
        {
            var adapter = new StringAdapter(100);
            NewView(adapter);
            var holder = adapter.HolderOf("item0");
            var bindsBefore = holder.Bound.Count;

            adapter.InsertAt(0, "new-head");

            Assert.That(holder.BoundIndex, Is.EqualTo(1),
                "surviving holder follows its item to the shifted index");
            Assert.That(holder.Bound.Count, Is.EqualTo(bindsBefore), "shift must not rebind");
            Assert.That(adapter.CreatedHolders, Has.Count.EqualTo(5));
        }

        [Test]
        public void RemoveVisibleItem_RecyclesItsHolderForTheExposedItem()
        {
            var adapter = new StringAdapter(100);
            NewView(adapter);
            var removedHolder = adapter.HolderOf("item2");

            adapter.RemoveAt(2);

            Assert.That(adapter.CreatedHolders, Has.Count.EqualTo(5));
            Assert.That(removedHolder.RecycledCount, Is.EqualTo(1));
            Assert.That(removedHolder.Bound.Last(), Is.EqualTo("item5"),
                "the freed holder is reused for the item scrolled in from below");
        }

        [Test]
        public void SetAdapterNull_RecyclesEverythingAndPurgesThePool()
        {
            var adapter = new StringAdapter(100);
            var view = NewView(adapter);
            var content = view.Q<VisualElement>("vcv-content");

            view.SetAdapter(null);

            Assert.That(content.childCount, Is.EqualTo(0));
            Assert.That(adapter.CreatedHolders.All(h => h.RecycledCount == 1), Is.True);
            Assert.That(adapter.CreatedHolders.All(h => h.BoundIndex == -1), Is.True);
        }
    }
}
