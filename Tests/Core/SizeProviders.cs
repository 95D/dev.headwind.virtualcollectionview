using Headwind.VirtualCollectionView.Core;

namespace Headwind.VirtualCollectionView.Tests
{
    /// <summary>Every item reports the same preferred size.</summary>
    internal sealed class FixedSizeProvider : ISizeProvider
    {
        private readonly LayoutVector2 _size;

        public FixedSizeProvider(float width, float height) => _size = new LayoutVector2(width, height);

        public LayoutVector2 GetItemSize(int index) => _size;
    }

    /// <summary>Per-item sizes supplied up front.</summary>
    internal sealed class VariableSizeProvider : ISizeProvider
    {
        private readonly LayoutVector2[] _sizes;

        public VariableSizeProvider(params LayoutVector2[] sizes) => _sizes = sizes;

        public int Count => _sizes.Length;

        public LayoutVector2 GetItemSize(int index) => _sizes[index];
    }
}
