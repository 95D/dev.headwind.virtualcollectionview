using NUnit.Framework;

namespace Headwind.VirtualCollectionView.Tests.UIToolkit
{
    public class CollectionViewAdapterTests
    {
        [Test]
        public void NotifyMethods_RaiseMatchingEvents()
        {
            var adapter = new StringAdapter(0);
            var dataSetChanged = 0;
            (int index, int count) inserted = (-1, -1);
            (int index, int count) removed = (-1, -1);
            (int from, int to) moved = (-1, -1);
            (int index, int count) changed = (-1, -1);

            adapter.DataSetChanged += () => dataSetChanged++;
            adapter.ItemRangeInserted += (i, c) => inserted = (i, c);
            adapter.ItemRangeRemoved += (i, c) => removed = (i, c);
            adapter.ItemMoved += (f, t) => moved = (f, t);
            adapter.ItemRangeChanged += (i, c) => changed = (i, c);

            adapter.NotifyDataSetChanged();
            adapter.NotifyItemRangeInserted(3, 2);
            adapter.NotifyItemRangeRemoved(4, 5);
            adapter.NotifyItemMoved(1, 6);
            adapter.NotifyItemRangeChanged(7, 8);

            Assert.That(dataSetChanged, Is.EqualTo(1));
            Assert.That(inserted, Is.EqualTo((3, 2)));
            Assert.That(removed, Is.EqualTo((4, 5)));
            Assert.That(moved, Is.EqualTo((1, 6)));
            Assert.That(changed, Is.EqualTo((7, 8)));
        }

        [Test]
        public void SingleItemNotifies_ForwardAsRangeOfOne()
        {
            var adapter = new StringAdapter(0);
            (int index, int count) inserted = (-1, -1);
            (int index, int count) removed = (-1, -1);
            (int index, int count) changed = (-1, -1);

            adapter.ItemRangeInserted += (i, c) => inserted = (i, c);
            adapter.ItemRangeRemoved += (i, c) => removed = (i, c);
            adapter.ItemRangeChanged += (i, c) => changed = (i, c);

            adapter.NotifyItemInserted(3);
            adapter.NotifyItemRemoved(4);
            adapter.NotifyItemChanged(5);

            Assert.That(inserted, Is.EqualTo((3, 1)));
            Assert.That(removed, Is.EqualTo((4, 1)));
            Assert.That(changed, Is.EqualTo((5, 1)));
        }

        [Test]
        public void TypedAdapter_UsesHolderTypeAsPoolKey()
        {
            var adapter = new StringAdapter(3);

            Assert.That(adapter.GetViewType(0), Is.EqualTo(typeof(RecordingHolder)));
            Assert.That(adapter.GetViewType(2), Is.EqualTo(typeof(RecordingHolder)));

            var holder = adapter.CreateViewHolder(typeof(RecordingHolder));
            Assert.That(holder, Is.InstanceOf<RecordingHolder>());
            Assert.That(adapter.CreatedHolders, Has.Count.EqualTo(1));
        }
    }
}
