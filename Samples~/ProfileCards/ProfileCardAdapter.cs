using System.Collections.Generic;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView.Samples
{
    /// <summary>
    /// Single-view-type adapter over an in-memory profile list.
    /// </summary>
    /// <remarks>
    /// The preferred size feeds every layout manager: a column reads the
    /// height (88) and stretches the width, a row reads the width (240), a
    /// grid reads the height and derives cell width from its span.
    /// </remarks>
    public sealed class ProfileCardAdapter : CollectionViewAdapter<ProfileCardViewHolder, ProfileModel>
    {
        private readonly List<ProfileModel> _items;

        public ProfileCardAdapter(List<ProfileModel> items) => _items = items;

        public override int Count => _items.Count;

        public override ProfileModel GetItem(int index) => _items[index];

        public override LayoutVector2 GetItemSize(int index) => new LayoutVector2(240f, 88f);

        protected override ProfileCardViewHolder CreateViewHolder() => new ProfileCardViewHolder();

        public void Shuffle(System.Random random)
        {
            for (var i = _items.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (_items[i], _items[j]) = (_items[j], _items[i]);
            }

            NotifyDataSetChanged();
        }

        public void InsertAt(int index, ProfileModel item)
        {
            _items.Insert(index, item);
            NotifyItemInserted(index);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            NotifyItemRemoved(index);
        }

        public void Move(int fromIndex, int toIndex)
        {
            var item = _items[fromIndex];
            _items.RemoveAt(fromIndex);
            _items.Insert(toIndex, item);
            NotifyItemMoved(fromIndex, toIndex);
        }
    }
}
