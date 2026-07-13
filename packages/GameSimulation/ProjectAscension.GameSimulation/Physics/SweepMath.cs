using System;
using System.Numerics;

namespace ProjectAscension.GameSimulation.Physics
{
    /// <summary>
    /// The actual maths behind <see cref="CollisionWorld"/>'s two questions. Sphere-vs-sphere and
    /// sphere-vs-capsule are solved exactly (closest point on the capsule's segment); sphere-vs-box
    /// is the standard "ray against the box expanded by the radius" approximation — slightly
    /// generous at the corners, invisible at the radii combat uses, and written down here rather
    /// than left to be rediscovered as a bug (ADR 0013).
    /// </summary>
    internal static class SweepMath
    {
        private const float Epsilon = 1e-8f;

        public static bool TrySweep(HitBody body, Vector3 from, Vector3 to, float radius, out float t, out Vector3 point, out Vector3 normal)
        {
            switch (body.Kind)
            {
                case HitBodyKind.Sphere:
                    return SweepVsSphere(from, to, radius, body.Center, body.Radius, out t, out point, out normal);
                case HitBodyKind.Capsule:
                    return SweepVsCapsule(from, to, radius, body, out t, out point, out normal);
                default:
                    return SweepVsBox(from, to, radius, body, out t, out point, out normal);
            }
        }

        public static bool Overlaps(HitBody body, Vector3 centre, float radius)
        {
            switch (body.Kind)
            {
                case HitBodyKind.Sphere:
                    return Vector3.DistanceSquared(centre, body.Center) <= Square(radius + body.Radius);

                case HitBodyKind.Capsule:
                    {
                        var (a, b) = body.CapsuleSegment;
                        var closest = ClosestPointOnSegment(centre, a, b);
                        return Vector3.DistanceSquared(centre, closest) <= Square(radius + body.Radius);
                    }

                default:
                    {
                        var invRot = Quaternion.Conjugate(body.BoxRotation);
                        var local = Vector3.Transform(centre - body.Center, invRot);
                        var clamped = new Vector3(
                            Math.Clamp(local.X, -body.BoxHalfExtents.X, body.BoxHalfExtents.X),
                            Math.Clamp(local.Y, -body.BoxHalfExtents.Y, body.BoxHalfExtents.Y),
                            Math.Clamp(local.Z, -body.BoxHalfExtents.Z, body.BoxHalfExtents.Z));
                        return Vector3.DistanceSquared(local, clamped) <= radius * radius;
                    }
            }
        }

        // --- sphere vs sphere (also IS the ray test, at radius 0 either side) ------------------

        public static bool SweepVsSphere(Vector3 from, Vector3 to, float movingRadius, Vector3 center, float bodyRadius,
            out float t, out Vector3 point, out Vector3 normal)
        {
            t = 0f;
            point = default;
            normal = Vector3.UnitY;

            var d = to - from;
            float rSum = movingRadius + bodyRadius;
            var f = from - center;
            float a = Vector3.Dot(d, d);

            if (a < Epsilon)
            {
                float distSq0 = Vector3.Dot(f, f);
                if (distSq0 > rSum * rSum) return false;
                normal = distSq0 > Epsilon ? Vector3.Normalize(f) : Vector3.UnitY;
                point = center + normal * bodyRadius;
                return true;
            }

            float b = 2f * Vector3.Dot(f, d);
            float c = Vector3.Dot(f, f) - rSum * rSum;

            if (c <= 0f)
            {
                // Already overlapping when the sweep starts — the muzzle is inside this body.
                normal = f.LengthSquared() > Epsilon ? Vector3.Normalize(f) : Vector3.UnitY;
                point = center + normal * bodyRadius;
                return true;
            }

            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;

            float tHit = (-b - MathF.Sqrt(disc)) / (2f * a); // a > 0 here, so this is the entering root
            if (tHit < 0f || tHit > 1f) return false;

            t = tHit;
            var hitPos = from + d * t;
            var n = hitPos - center;
            normal = n.LengthSquared() > Epsilon ? Vector3.Normalize(n) : Vector3.UnitY;
            point = center + normal * bodyRadius;
            return true;
        }

        // --- sphere vs capsule (exact — three regimes: the cylinder body, or either end cap) ---

        public static bool SweepVsCapsule(Vector3 from, Vector3 to, float movingRadius, HitBody body,
            out float t, out Vector3 point, out Vector3 normal)
        {
            t = 0f;
            point = default;
            normal = Vector3.UnitY;

            var (capA, capB) = body.CapsuleSegment;
            var axisVec = capB - capA;
            float segLenSq = axisVec.LengthSquared();
            if (segLenSq < Epsilon)
                return SweepVsSphere(from, to, movingRadius, body.Center, body.Radius, out t, out point, out normal);

            float length = MathF.Sqrt(segLenSq);
            var u = axisVec / length;
            float rSum = movingRadius + body.Radius;

            var d = to - from;
            var w0 = from - capA;
            float s0 = Vector3.Dot(w0, u);
            float s1 = s0 + Vector3.Dot(d, u);

            var lineDom = IntersectDomain(s0, s1, 0f, length);
            var aDom = IntersectDomain(s0, s1, float.NegativeInfinity, 0f);
            var bDom = IntersectDomain(s0, s1, length, float.PositiveInfinity);

            float bestT = float.MaxValue;
            int regime = -1; // 0 = the cylindrical body, 1 = cap at A, 2 = cap at B

            if (lineDom.lo <= lineDom.hi)
            {
                float sud = s1 - s0;
                float dd = Vector3.Dot(d, d);
                float wd = Vector3.Dot(w0, d);
                float ww = Vector3.Dot(w0, w0);
                float a2 = MathF.Max(0f, dd - sud * sud); // >= 0 by Cauchy-Schwarz; clamp fp noise
                float b2 = 2f * (wd - s0 * sud);
                float c2 = ww - s0 * s0 - rSum * rSum;
                if (TryQuadraticEntry(a2, b2, c2, lineDom.lo, lineDom.hi, out float tl) && tl < bestT)
                {
                    bestT = tl;
                    regime = 0;
                }
            }
            if (aDom.lo <= aDom.hi && TryEndpointEntry(from, d, capA, rSum, aDom.lo, aDom.hi, out float ta) && ta < bestT)
            {
                bestT = ta;
                regime = 1;
            }
            if (bDom.lo <= bDom.hi && TryEndpointEntry(from, d, capB, rSum, bDom.lo, bDom.hi, out float tb) && tb < bestT)
            {
                bestT = tb;
                regime = 2;
            }

            if (regime < 0) return false;

            t = bestT;
            var pAt = from + d * t;
            var axisPoint = regime switch
            {
                1 => capA,
                2 => capB,
                _ => capA + u * Math.Clamp(s0 + (s1 - s0) * t, 0f, length),
            };
            var n = pAt - axisPoint;
            normal = n.LengthSquared() > Epsilon ? Vector3.Normalize(n) : Vector3.UnitY;
            point = axisPoint + normal * body.Radius;
            return true;
        }

        private static bool TryEndpointEntry(Vector3 from, Vector3 d, Vector3 endpoint, float rSum, float domainLo, float domainHi, out float t)
        {
            var f = from - endpoint;
            float a = Vector3.Dot(d, d);
            float b = 2f * Vector3.Dot(f, d);
            float c = Vector3.Dot(f, f) - rSum * rSum;
            return TryQuadraticEntry(a, b, c, domainLo, domainHi, out t);
        }

        /// <summary>Smallest t in [domainLo, domainHi] where a*t^2 + b*t + c &lt;= 0 — i.e. distance
        /// squared first drops to (or starts at) the target radius. Assumes a &gt;= 0 (always true
        /// for these distance-squared-along-a-line quadratics).</summary>
        private static bool TryQuadraticEntry(float a, float b, float c, float domainLo, float domainHi, out float t)
        {
            t = 0f;
            if (domainLo > domainHi) return false;

            float valueAtLo = a * domainLo * domainLo + b * domainLo + c;
            if (valueAtLo <= 0f)
            {
                t = domainLo;
                return true;
            }

            if (a < Epsilon)
            {
                if (MathF.Abs(b) < Epsilon) return false;
                float root = -c / b;
                if (root > domainLo && root <= domainHi)
                {
                    t = root;
                    return true;
                }
                return false;
            }

            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;
            float t1 = (-b - MathF.Sqrt(disc)) / (2f * a); // a >= 0, so this is the smaller (entering) root
            if (t1 > domainLo && t1 <= domainHi)
            {
                t = t1;
                return true;
            }
            return false;
        }

        /// <summary>The t in [0,1] where the linear param p(t) = lerp(p0,p1,t) lies within
        /// [rangeLo, rangeHi] (either bound may be infinite). Empty domain reported as lo &gt; hi.</summary>
        private static (float lo, float hi) IntersectDomain(float p0, float p1, float rangeLo, float rangeHi)
        {
            if (MathF.Abs(p1 - p0) < Epsilon)
                return p0 >= rangeLo && p0 <= rangeHi ? (0f, 1f) : (1f, 0f);

            float tForLo = (rangeLo - p0) / (p1 - p0);
            float tForHi = (rangeHi - p0) / (p1 - p0);
            float lo = MathF.Max(MathF.Min(tForLo, tForHi), 0f);
            float hi = MathF.Min(MathF.Max(tForLo, tForHi), 1f);
            return (lo, hi);
        }

        // --- sphere vs box (approximation: ray vs the box expanded by the radius, box-local) ----

        public static bool SweepVsBox(Vector3 from, Vector3 to, float movingRadius, HitBody body,
            out float t, out Vector3 point, out Vector3 normal)
        {
            t = 0f;
            point = default;
            normal = Vector3.UnitY;

            var invRot = Quaternion.Conjugate(body.BoxRotation);
            var localFrom = Vector3.Transform(from - body.Center, invRot);
            var localTo = Vector3.Transform(to - body.Center, invRot);
            var dir = localTo - localFrom;
            var half = body.BoxHalfExtents + new Vector3(movingRadius, movingRadius, movingRadius);

            float tmin = 0f, tmax = 1f;
            int enterAxis = -1;
            float enterSign = 1f;

            for (int axis = 0; axis < 3; axis++)
            {
                float o = Component(localFrom, axis);
                float dd = Component(dir, axis);
                float h = Component(half, axis);

                if (MathF.Abs(dd) < Epsilon)
                {
                    if (o < -h || o > h) return false;
                    continue;
                }

                float ood = 1f / dd;
                float tNeg = (-h - o) * ood;
                float tPos = (h - o) * ood;
                float entryT, exitT, sign;
                if (tNeg <= tPos) { entryT = tNeg; exitT = tPos; sign = -1f; }
                else { entryT = tPos; exitT = tNeg; sign = 1f; }

                if (entryT > tmin)
                {
                    tmin = entryT;
                    enterAxis = axis;
                    enterSign = sign;
                }
                if (exitT < tmax) tmax = exitT;
                if (tmin > tmax) return false;
            }

            if (tmin > 1f) return false;

            t = MathF.Max(tmin, 0f);
            var localHitPoint = localFrom + dir * t;
            var localNormal = enterAxis < 0
                ? LeastPenetrationNormal(localHitPoint, half)
                : enterAxis switch
                {
                    0 => new Vector3(enterSign, 0f, 0f),
                    1 => new Vector3(0f, enterSign, 0f),
                    _ => new Vector3(0f, 0f, enterSign),
                };

            normal = Vector3.Normalize(Vector3.Transform(localNormal, body.BoxRotation));
            var worldHit = from + (to - from) * t;
            point = worldHit - normal * movingRadius; // pulled back onto the REAL (unexpanded) box surface
            return true;
        }

        private static Vector3 LeastPenetrationNormal(Vector3 localPoint, Vector3 half)
        {
            float dx = half.X - MathF.Abs(localPoint.X);
            float dy = half.Y - MathF.Abs(localPoint.Y);
            float dz = half.Z - MathF.Abs(localPoint.Z);
            if (dx <= dy && dx <= dz) return new Vector3(MathF.Sign(localPoint.X == 0f ? 1f : localPoint.X), 0f, 0f);
            if (dy <= dx && dy <= dz) return new Vector3(0f, MathF.Sign(localPoint.Y == 0f ? 1f : localPoint.Y), 0f);
            return new Vector3(0f, 0f, MathF.Sign(localPoint.Z == 0f ? 1f : localPoint.Z));
        }

        private static float Component(Vector3 v, int axis) => axis == 0 ? v.X : axis == 1 ? v.Y : v.Z;

        // --- shared helpers ----------------------------------------------------------------------

        private static Vector3 ClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            float lenSq = ab.LengthSquared();
            if (lenSq < Epsilon) return a;
            float t = Math.Clamp(Vector3.Dot(p - a, ab) / lenSq, 0f, 1f);
            return a + ab * t;
        }

        private static float Square(float v) => v * v;
    }
}
