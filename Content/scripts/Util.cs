using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Physics
{
    public static class Util
    {
        public const double Sqrt2 = 1.4142135623730950488016887242096980785696718753769;

        public static float Saturate(this float v)
        {
            return Clamp(v, 0f, 1f);
        }

        public static float Clamp(this float v, float min, float max)
        {
            return MathF.Max(MathF.Min(v, max), min);
        }

        public static float Dot(this Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static float Cross(this Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static Vector2 ProjectOnLine(this Vector2 p, Vector2 normal)
        {
            return p - normal * normal.Dot(p);
        }

        public static Vector2 RotateToLine(this Vector2 p, Vector2 normal)
        {
            return p.Length() * normal * Math.Sign(normal.Dot(p));
        }

        public static Vector2 Normalized(this Vector2 p)
        {
            return p / p.Length();
        }

        public static Vector2 Perpendicular(this Vector2 p)
        {
            return new Vector2(-p.Y, p.X);
        }

        public static float Lerp(this float a, float b, float t)
        {
            return a + (b - a) * t;
        }
        public static Vector2 LerpVector2(this Vector2 a, Vector2 b, float t)
        {
            return a + (b - a) * t;
        }
        public static int LerpInt(this int a, int b, float t)
        {
            return a + (int)((b - a) * t);
        }
        public static Color LerpColor(this Color a, Color b, float t)
        {
            Vector4 av = a.ToVector4();
            return new Color(av + (b.ToVector4() - av) * t);
        }

        public static Vector2 Hermite(this Vector2 p0, Vector2 v0, Vector2 p1, Vector2 v1, float t, float dt)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return p0 * (1 - 3 * t2 + 2 * t3) + v0 * (dt * (t - 2 * t2 + t3)) +
                p1 * (3 * t2 - 2 * t3) + v1 * (dt * (-t2 + t3));
        }

        public static int FiniteLineIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 i, float padding = 0.05f)
        {
            i = Vector2.Zero;
            Vector2[] abBox = LineToBoundingBox(a, b);
            Vector2[] cdBox = LineToBoundingBox(c, d);
            // could be vulnerable as DoBoxesOverlap has no padding for floating point inprecision
            if (!DoBoxesOverlap(abBox, cdBox)) { return -1; } // bounding boxes don't overlap

            Vector2 abDelta = b - a;
            Vector2 cdDelta = d - c;
            float determinant = Cross(abDelta, cdDelta);
            if (determinant == 0) { return -2; } // the two lines are parallel

            i = (Cross(b, a) * cdDelta - Cross(d, c) * abDelta) / determinant;

            Vector2 acDelta = c - a;
            float t = Cross(acDelta, cdDelta) / determinant;
            float u = Cross(acDelta, abDelta) / determinant;

            // check padded for floating point inprecision
            if (t >= -padding && t <= 1 + padding && u >= -padding && u <= 1 + padding)
            {
                i = a + t * abDelta;
                return 0;
            }

            return -3; // intersection not in finite lines
        }

        public static Vector2[] LineToBoundingBox(Vector2 a, Vector2 b)
        {
            return new Vector2[2] { new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y)),
                new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y)) };
        }

        public static bool IsPointInBox(Vector2 point, Vector2[] box)
        {
            return point.X <= box[1].X && box[0].X <= point.X && point.Y <= box[1].Y && box[0].Y <= point.Y;
        }
        public static bool IsPointInBox(Vector2 point, Vector2 boxMin, Vector2 boxMax)
        {
            return IsPointInBox(point, new Vector2[] { boxMin, boxMax });
        }

        public static bool DoBoxesOverlap(Vector2 boxAMin, Vector2 boxAMax, Vector2 boxBMin, Vector2 boxBMax)
        {
            return boxAMin.X <= boxBMax.X && boxBMin.X <= boxAMax.X && boxAMin.Y <= boxBMax.Y && boxBMin.Y <= boxAMax.Y;
        }
        public static bool DoBoxesOverlap(Vector2[] boxA, Vector2[] boxB)
        {
            return boxA[0].X <= boxB[1].X && boxB[0].X <= boxA[1].X && boxA[0].Y <= boxB[1].Y && boxB[0].Y <= boxA[1].Y;
        }

        public static T GetItem<T>(this T[] array, int index)
        {
            if (index >= array.Length)
            {
                if (index < array.Length << 1) { return array[index - array.Length]; }
                return array[index % array.Length];
            }
            if (index < 0)
            {
                if (index >= -array.Length) { return array[index + array.Length]; }
                return array[index % array.Length + array.Length];
            }
            return array[index];
        }

        public static T GetItem<T>(this List<T> list, int index)
        {
            if (index >= list.Count)
            {
                if (index < list.Count << 1) { return list[index - list.Count]; }
                return list[index % list.Count];
            }
            if (index < 0)
            {
                if (index >= -list.Count) { return list[index + list.Count]; }
                return list[index % list.Count + list.Count];
            }
            return list[index];
        }

        
    }

    public struct Matrix2 : IEquatable<Matrix2>
    {
        public static readonly Matrix2 Identity = new(1f, 0f, 0f, 1f);
        public static readonly Matrix2 Zero = new(0f, 0f, 0f, 0f);

        public static readonly Matrix2 Clockwise45Rot = new((float)Util.Sqrt2 / 2f, (float)Util.Sqrt2 / 2f,
            -(float)Util.Sqrt2 / 2f, (float)Util.Sqrt2 / 2f);
        public static readonly Matrix2 Clockwise90Rot = new(0f, 1f, -1f, 0f);
        public static readonly Matrix2 Clockwise135Rot = new(-(float)Util.Sqrt2 / 2f, -(float)Util.Sqrt2 / 2f,
            (float)Util.Sqrt2 / 2f, -(float)Util.Sqrt2 / 2f);
        public static readonly Matrix2 Clockwise180Rot = new(-1f, 0f, 0f, -1f);
        public static readonly Matrix2 Clockwise225Rot = new(-(float)Util.Sqrt2 / 2f, (float)Util.Sqrt2 / 2f,
            -(float)Util.Sqrt2 / 2f, -(float)Util.Sqrt2 / 2f);
        public static readonly Matrix2 Clockwise270Rot = new(0f, -1f, 1f, 0f);
        public static readonly Matrix2 Clockwise315Rot = new((float)Util.Sqrt2 / 2f, -(float)Util.Sqrt2 / 2f,
            (float)Util.Sqrt2 / 2f, (float)Util.Sqrt2 / 2f);

        public Vector2 iHat;
        public Vector2 jHat;
        public readonly float a => iHat.X;
        public readonly float b => jHat.X;
        public readonly float c => iHat.Y;
        public readonly float d => jHat.Y;

        public Matrix2(float a, float b, float c, float d) // top row then bottom row
        {
            iHat = new Vector2(a, c);
            jHat = new Vector2(b, d);
        }

        public Matrix2(Vector2 iHat, Vector2 jHat) // i hat then j hat
        {
            this.iHat = iHat;
            this.jHat = jHat;
        }

        public static Matrix2 operator *(Matrix2 matA, Matrix2 matB)
        {
            return new Matrix2(matA.a * matB.a + matA.b * matB.c, matA.a * matB.b + matA.b * matB.d,
                matA.c * matB.a + matA.d * matB.c, matA.c * matB.b + matA.d * matB.d);
        }
        public static Vector2 operator *(Matrix2 mat, Vector2 p)
        {
            return p.X * mat.iHat + p.Y * mat.jHat;
        }
        public static Vector2 operator *(Vector2 p, Matrix2 mat)
        {
            return mat * p;
        }
        public static bool operator ==(Matrix2 matA, Matrix2 matB)
        {
            return matA.Equals(matB);
        }
        public static bool operator !=(Matrix2 matA, Matrix2 matB)
        {
            return !matA.Equals(matB);
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is Matrix2 mat)
            {
                return Equals(mat);
            }

            return false;
        }
        public readonly bool Equals(Matrix2 other)
        {
            if (iHat == other.iHat)
            {
                return jHat == other.jHat;
            }

            return false;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(a, b, c, d);
        }
    }
}
