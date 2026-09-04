using System.Collections.Generic;
using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView.Tests
{
    internal static class TestUtil
    {
        public static List<int> VisibleIndices(LayoutManager layout, ItemRect viewport)
        {
            var result = new List<int>();
            layout.GetVisibleIndices(viewport, result);
            return result;
        }

        public static List<int> Range(int start, int countInclusiveEnd)
        {
            var list = new List<int>();
            for (var i = start; i <= countInclusiveEnd; i++)
                list.Add(i);
            return list;
        }
    }
}
