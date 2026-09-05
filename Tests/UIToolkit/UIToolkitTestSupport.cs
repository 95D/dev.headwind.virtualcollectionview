using System.Collections.Generic;
using UnityEngine.UIElements;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView.Tests.UIToolkit
{
    /// <summary>Holder that records every bound item and recycle call.</summary>
    internal sealed class RecordingHolder : CollectionItemViewHolder<string>
    {
        public readonly List<string> Bound = new List<string>();
        public int RecycledCount;

        public RecordingHolder() : base(new VisualElement())
        {
        }

        public override void Bind(string item) => Bound.Add(item);

        public override void OnRecycled() => RecycledCount++;
    }

    /// <summary>In-memory string adapter with fixed 100×20 items, tracking created holders.</summary>
    internal sealed class StringAdapter : CollectionViewAdapter<RecordingHolder, string>
    {
        public readonly List<RecordingHolder> CreatedHolders = new List<RecordingHolder>();
        private readonly List<string> _items;

        public StringAdapter(int count)
        {
            _items = new List<string>(count);
            for (var i = 0; i < count; i++)
                _items.Add("item" + i);
        }

        public override int Count => _items.Count;

        public override string GetItem(int index) => _items[index];

        public override LayoutVector2 GetItemSize(int index) => new LayoutVector2(100f, 20f);

        protected override RecordingHolder CreateViewHolder()
        {
            var holder = new RecordingHolder();
            CreatedHolders.Add(holder);
            return holder;
        }

        public void InsertAt(int index, string value)
        {
            _items.Insert(index, value);
            NotifyItemInserted(index);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            NotifyItemRemoved(index);
        }

        public void Set(int index, string value)
        {
            _items[index] = value;
            NotifyItemChanged(index);
        }

        /// <summary>The holder currently or last bound to the given value, or null.</summary>
        public RecordingHolder HolderOf(string value)
        {
            foreach (var holder in CreatedHolders)
            {
                if (holder.Bound.Count > 0 && holder.Bound[holder.Bound.Count - 1] == value)
                    return holder;
            }

            return null;
        }
    }
}
