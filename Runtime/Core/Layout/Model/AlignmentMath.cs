namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>Interpolation factor (0/0.5/1) an alignment resolves to along its axis.</summary>
    internal static class AlignmentMath
    {
        public static float Factor(MainAlignment alignment) =>
            alignment == MainAlignment.Center ? 0.5f :
            alignment == MainAlignment.End ? 1f : 0f;

        public static float Factor(LinearCrossAlignment alignment) =>
            alignment == LinearCrossAlignment.Center ? 0.5f :
            alignment == LinearCrossAlignment.End ? 1f : 0f;

        public static float Factor(GridCrossAlignment alignment) =>
            alignment == GridCrossAlignment.Center ? 0.5f :
            alignment == GridCrossAlignment.End ? 1f : 0f;
    }
}
