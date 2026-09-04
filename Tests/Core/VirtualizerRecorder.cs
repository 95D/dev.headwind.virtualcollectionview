using System.Collections.Generic;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView.Tests
{
    /// <summary>Records virtualizer events for assertions.</summary>
    internal sealed class VirtualizerRecorder
    {
        public readonly List<int> Realized = new List<int>();
        public readonly List<int> Virtualized = new List<int>();
        public readonly List<int> Moved = new List<int>();
        public readonly List<int> Updated = new List<int>();
        public readonly List<IndexShift> Shifts = new List<IndexShift>();
        public readonly Dictionary<int, RealizeCause> RealizeCauses = new Dictionary<int, RealizeCause>();
        public readonly Dictionary<int, VirtualizeCause> VirtualizeCauses = new Dictionary<int, VirtualizeCause>();
        public readonly Dictionary<int, ItemRect> MovedFrom = new Dictionary<int, ItemRect>();
        public readonly Dictionary<int, ItemRect> Rects = new Dictionary<int, ItemRect>();
        public int OffsetChangedCount;
        public LayoutVector2 LastMoveCompensation;

        public VirtualizerRecorder(CollectionVirtualizer virtualizer)
        {
            virtualizer.ItemRealized += (index, rect, cause) =>
            {
                Realized.Add(index);
                RealizeCauses[index] = cause;
                Rects[index] = rect;
            };
            virtualizer.ItemVirtualized += (index, cause) =>
            {
                Virtualized.Add(index);
                VirtualizeCauses[index] = cause;
                Rects.Remove(index);
            };
            virtualizer.ItemMoved += (index, from, to) =>
            {
                Moved.Add(index);
                MovedFrom[index] = from;
                Rects[index] = to;
                LastMoveCompensation = virtualizer.ScrollCompensation;
            };
            virtualizer.ItemsShifted += shifts =>
            {
                Shifts.AddRange(shifts);
                // Two-phase remap, as the batch contract prescribes.
                var carried = new List<KeyValuePair<int, ItemRect>>();
                foreach (var shift in shifts)
                {
                    if (Rects.TryGetValue(shift.From, out var rect))
                    {
                        carried.Add(new KeyValuePair<int, ItemRect>(shift.To, rect));
                        Rects.Remove(shift.From);
                    }
                }

                foreach (var kv in carried)
                    Rects[kv.Key] = kv.Value;
            };
            virtualizer.ItemUpdated += index => Updated.Add(index);
            virtualizer.ScrollOffsetChanged += () => OffsetChangedCount++;
        }

        /// <summary>Indices currently realized according to the recorded event stream.</summary>
        public List<int> Active
        {
            get
            {
                var list = new List<int>(Rects.Keys);
                list.Sort();
                return list;
            }
        }

        public void ClearLog()
        {
            Realized.Clear();
            Virtualized.Clear();
            Moved.Clear();
            Updated.Clear();
            Shifts.Clear();
            RealizeCauses.Clear();
            VirtualizeCauses.Clear();
            MovedFrom.Clear();
            OffsetChangedCount = 0;
            LastMoveCompensation = LayoutVector2.Zero;
        }
    }
}
