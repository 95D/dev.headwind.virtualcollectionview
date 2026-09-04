using System;

namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Core-owned rectangle in virtual content space.
    /// </summary>
    /// <remarks>
    /// Convention: origin is top-left, Y grows downward. Platform bindings
    /// whose coordinate system differs (e.g. UGUI) convert at the boundary.
    /// </remarks>
    public readonly struct ItemRect : IEquatable<ItemRect>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public ItemRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public ItemRect(LayoutVector2 position, LayoutVector2 size)
            : this(position.X, position.Y, size.X, size.Y)
        {
        }

        public float XMax => X + Width;
        public float YMax => Y + Height;
        public LayoutVector2 Position => new LayoutVector2(X, Y);
        public LayoutVector2 Size => new LayoutVector2(Width, Height);

        public bool Overlaps(in ItemRect other) =>
            other.X < XMax && other.XMax > X &&
            other.Y < YMax && other.YMax > Y;

        public static bool operator ==(ItemRect a, ItemRect b) => a.Equals(b);
        public static bool operator !=(ItemRect a, ItemRect b) => !a.Equals(b);

        public bool Equals(ItemRect other) =>
            X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public override bool Equals(object obj) => obj is ItemRect other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString() => $"(x:{X}, y:{Y}, w:{Width}, h:{Height})";
    }
}
