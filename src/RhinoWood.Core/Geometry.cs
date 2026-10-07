using System;
using System.Globalization;

namespace RhinoWood.Core.Geometry
{
    public enum Axis { X = 0, Y = 1, Z = 2 }

    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public readonly double X, Y, Z;
        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static readonly Vec3 Zero = new Vec3(0, 0, 0);
        public static Vec3 Unit(Axis a, double s = 1) =>
            a == Axis.X ? new Vec3(s, 0, 0) : a == Axis.Y ? new Vec3(0, s, 0) : new Vec3(0, 0, s);
        public double Get(Axis a) => a == Axis.X ? X : a == Axis.Y ? Y : Z;
        public Vec3 With(Axis a, double v) =>
            a == Axis.X ? new Vec3(v, Y, Z) : a == Axis.Y ? new Vec3(X, v, Z) : new Vec3(X, Y, v);
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public double Dot(Vec3 o) => X * o.X + Y * o.Y + Z * o.Z;
        public Vec3 Cross(Vec3 o) => new Vec3(Y * o.Z - Z * o.Y, Z * o.X - X * o.Z, X * o.Y - Y * o.X);
        public double Length => Math.Sqrt(Dot(this));
        public Vec3 Normalized() { var l = Length; return l < 1e-12 ? this : this * (1.0 / l); }
        public bool Equals(Vec3 o) => Math.Abs(X - o.X) < 1e-9 && Math.Abs(Y - o.Y) < 1e-9 && Math.Abs(Z - o.Z) < 1e-9;
        public override bool Equals(object obj) => obj is Vec3 v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(Math.Round(X, 6), Math.Round(Y, 6), Math.Round(Z, 6));
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:0.###},{1:0.###},{2:0.###})", X, Y, Z);
    }

    /// <summary>Axis-aligned box. All furniture parts are axis aligned in V1.</summary>
    public readonly struct Box3
    {
        public readonly Vec3 Min, Max;
        public Box3(Vec3 a, Vec3 b)
        {
            Min = new Vec3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
            Max = new Vec3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }
        public static Box3 FromMinSize(Vec3 min, Vec3 size) => new Box3(min, min + size);
        public Vec3 Size => Max - Min;
        public Vec3 Center => (Min + Max) * 0.5;
        public double Volume => Size.X * Size.Y * Size.Z;
        public Box3 Offset(Vec3 d) => new Box3(Min + d, Max + d);
        public Box3 Inflate(double d) => new Box3(Min - new Vec3(d, d, d), Max + new Vec3(d, d, d));
        public bool Intersects(Box3 o, double eps = 1e-6) =>
            Min.X < o.Max.X - eps && Max.X > o.Min.X + eps &&
            Min.Y < o.Max.Y - eps && Max.Y > o.Min.Y + eps &&
            Min.Z < o.Max.Z - eps && Max.Z > o.Min.Z + eps;
        public Box3 Union(Box3 o) => new Box3(
            new Vec3(Math.Min(Min.X, o.Min.X), Math.Min(Min.Y, o.Min.Y), Math.Min(Min.Z, o.Min.Z)),
            new Vec3(Math.Max(Max.X, o.Max.X), Math.Max(Max.Y, o.Max.Y), Math.Max(Max.Z, o.Max.Z)));
        public override string ToString() => Min + ".." + Max;
    }
}
