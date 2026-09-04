using System;

namespace Headwind.VirtualCollectionView.Core
{
    /// <summary>
    /// Core-owned 2D vector.
    /// </summary>
    /// <remarks>
    /// The core assembly has no engine references (UnityEngine, Godot, ...),
    /// so it carries its own math types. Platform bindings convert at the
    /// boundary.
    /// </remarks>
    public readonly struct LayoutVector2 : IEquatable<LayoutVector2>
    {
        public readonly float X;
        public readonly float Y;

        public LayoutVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static LayoutVector2 Zero => default;

        public static LayoutVector2 operator +(LayoutVector2 a, LayoutVector2 b) => new LayoutVector2(a.X + b.X, a.Y + b.Y);
        public static LayoutVector2 operator -(LayoutVector2 a, LayoutVector2 b) => new LayoutVector2(a.X - b.X, a.Y - b.Y);
        public static LayoutVector2 operator -(LayoutVector2 a) => new LayoutVector2(-a.X, -a.Y);
        public static LayoutVector2 operator *(LayoutVector2 a, float s) => new LayoutVector2(a.X * s, a.Y * s);

        public static bool operator ==(LayoutVector2 a, LayoutVector2 b) => a.Equals(b);
        public static bool operator !=(LayoutVector2 a, LayoutVector2 b) => !a.Equals(b);

        public static LayoutVector2 Max(LayoutVector2 a, LayoutVector2 b) =>
            new LayoutVector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

        public static LayoutVector2 Clamp(LayoutVector2 v, LayoutVector2 min, LayoutVector2 max) =>
            new LayoutVector2(
                Math.Min(Math.Max(v.X, min.X), max.X),
                Math.Min(Math.Max(v.Y, min.Y), max.Y));

        public bool Equals(LayoutVector2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is LayoutVector2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";
    }
}
