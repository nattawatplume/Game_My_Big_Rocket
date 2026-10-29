using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>
    /// Tiny procedural mesh kit for the low-poly art. Shapes are appended in local space through the current
    /// <see cref="Matrix"/> (set it with <c>At(...)</c>). Triangle winding is fixed automatically from an outward
    /// hint (flat faces) or from the vertex normals (smooth faces), so shapes can't end up inside-out.
    /// Averaged normals are baked into UV channel 3 for the toon outline pass.
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();

        Matrix4x4 matrix = Matrix4x4.identity;
        Matrix4x4 normalMatrix = Matrix4x4.identity;

        public int VertexCount => verts.Count;

        /// <summary>Local-to-mesh transform applied to every shape appended after it is set.</summary>
        public Matrix4x4 Matrix
        {
            get => matrix;
            set
            {
                matrix = value;
                normalMatrix = value.inverse.transpose;
            }
        }

        public MeshBuilder Identity()
        {
            Matrix = Matrix4x4.identity;
            return this;
        }

        public MeshBuilder At(Vector3 pos)
        {
            Matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            return this;
        }

        public MeshBuilder At(Vector3 pos, Quaternion rot)
        {
            Matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            return this;
        }

        public MeshBuilder At(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            Matrix = Matrix4x4.TRS(pos, rot, scale);
            return this;
        }

        // ------------------------------------------------------------------ primitives

        Vector3 TP(Vector3 p) => matrix.MultiplyPoint3x4(p);

        /// <summary>Adds one flat-shaded triangle (own vertices). Winding is chosen so the face points along <paramref name="outward"/>.</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Vector2 ua = new Vector2(a.x, a.y), ub = new Vector2(b.x, b.y), uc = new Vector2(c.x, c.y);
            a = TP(a);
            b = TP(b);
            c = TP(c);
            outward = matrix.MultiplyVector(outward);

            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(n, outward) < 0f)
            {
                (b, c) = (c, b);
                (ub, uc) = (uc, ub);
                n = -n;
            }
            n.Normalize();

            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            normals.Add(n); normals.Add(n); normals.Add(n);
            uvs.Add(ua); uvs.Add(ub); uvs.Add(uc);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        /// <summary>Flat quad a-b-c-d (a cycle around the face).</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            Tri(a, b, c, outward);
            Tri(a, c, d, outward);
        }

        /// <summary>Adds a shared (smooth) vertex and returns its index.</summary>
        public int V(Vector3 p, Vector3 n, Vector2 uv)
        {
            verts.Add(TP(p));
            Vector3 wn = normalMatrix.MultiplyVector(n);
            normals.Add(wn.sqrMagnitude > 1e-12f ? wn.normalized : Vector3.up);
            uvs.Add(uv);
            return verts.Count - 1;
        }

        /// <summary>Smooth triangle from shared vertices; winding follows the vertex normals. Degenerate tris are skipped.</summary>
        public void STri(int a, int b, int c)
        {
            Vector3 fn = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (fn.sqrMagnitude < 1e-18f) return;
            Vector3 avg = normals[a] + normals[b] + normals[c];
            if (Vector3.Dot(fn, avg) < 0f) (b, c) = (c, b);
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        public void SQuad(int a, int b, int c, int d)
        {
            STri(a, b, c);
            STri(a, c, d);
        }

        // ------------------------------------------------------------------ shapes

        static readonly int[] QuadU = { -1, 1, 1, -1 };
        static readonly int[] QuadV = { -1, -1, 1, 1 };

        /// <summary>Flat box with chamfered (bevel-ish) edges and corners.</summary>
        public void Box(Vector3 center, Vector3 size, float chamfer = 0f)
        {
            Vector3 h = size * 0.5f;
            chamfer = Mathf.Clamp(chamfer, 0f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.95f);
            var s = new int[3];

            Vector3 Cp(int axis)
            {
                var p = new Vector3(s[0] * (h.x - chamfer), s[1] * (h.y - chamfer), s[2] * (h.z - chamfer));
                p[axis] = s[axis] * h[axis];
                return center + p;
            }

            // 6 main faces
            for (int a = 0; a < 3; a++)
            {
                int b = (a + 1) % 3, d = (a + 2) % 3;
                for (int sa = -1; sa <= 1; sa += 2)
                {
                    var q = new Vector3[4];
                    for (int k = 0; k < 4; k++)
                    {
                        s[a] = sa; s[b] = QuadU[k]; s[d] = QuadV[k];
                        q[k] = Cp(a);
                    }
                    Vector3 o = Vector3.zero;
                    o[a] = sa;
                    Quad(q[0], q[1], q[2], q[3], o);
                }
            }

            if (chamfer <= 0f) return;

            // 12 edge bevels
            for (int k = 0; k < 3; k++)
            {
                int i = (k + 1) % 3, j = (k + 2) % 3;
                for (int si = -1; si <= 1; si += 2)
                for (int sj = -1; sj <= 1; sj += 2)
                {
                    s[i] = si; s[j] = sj;
                    s[k] = -1;
                    Vector3 pi0 = Cp(i), pj0 = Cp(j);
                    s[k] = 1;
                    Vector3 pi1 = Cp(i), pj1 = Cp(j);
                    Vector3 o = Vector3.zero;
                    o[i] = si; o[j] = sj;
                    Quad(pi0, pi1, pj1, pj0, o);
                }
            }

            // 8 corner triangles
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                s[0] = sx; s[1] = sy; s[2] = sz;
                Tri(Cp(0), Cp(1), Cp(2), new Vector3(sx, sy, sz));
            }
        }

        /// <summary>Icosphere / ellipsoid. <paramref name="jitter"/> randomly pushes vertices in/out (rocks).</summary>
        public void Sphere(Vector3 center, Vector3 radii, int subdiv, bool flat, float jitter = 0f, int seed = 1)
        {
            BuildIcosphere(subdiv, out var pts, out var faces);
            if (jitter > 0f)
            {
                var rng = new System.Random(seed);
                for (int i = 0; i < pts.Count; i++)
                    pts[i] *= 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
            }

            Vector3 Sc(Vector3 v) => new Vector3(v.x * radii.x, v.y * radii.y, v.z * radii.z);

            if (flat)
            {
                for (int f = 0; f < faces.Count; f += 3)
                {
                    Vector3 a = Sc(pts[faces[f]]), b = Sc(pts[faces[f + 1]]), c = Sc(pts[faces[f + 2]]);
                    Tri(center + a, center + b, center + c, (a + b + c) / 3f);
                }
                return;
            }

            int baseIndex = verts.Count;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 p = pts[i];
                Vector3 n = new Vector3(p.x / radii.x, p.y / radii.y, p.z / radii.z);
                Vector2 uv = new Vector2(Mathf.Atan2(p.z, p.x) / (2f * Mathf.PI) + 0.5f, p.normalized.y * 0.5f + 0.5f);
                V(center + Sc(p), n, uv);
            }
            for (int f = 0; f < faces.Count; f += 3)
                STri(baseIndex + faces[f], baseIndex + faces[f + 1], baseIndex + faces[f + 2]);
        }

        public void Sphere(Vector3 center, float radius, int subdiv, bool flat) =>
            Sphere(center, new Vector3(radius, radius, radius), subdiv, flat);

        /// <summary>
        /// Surface of revolution around +Y. Profile points are (radius, height), ordered bottom to top.
        /// Smooth uses profile tangents for normals; flat gives faceted low-poly bands.
        /// </summary>
        public void Lathe(IList<Vector2> profile, int sides, bool smooth, bool capBottom = false, bool capTop = false)
        {
            int n = profile.Count;
            if (n < 2 || sides < 3) return;

            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                minY = Mathf.Min(minY, profile[i].y);
                maxY = Mathf.Max(maxY, profile[i].y);
            }
            float spanY = Mathf.Max(maxY - minY, 1e-5f);

            Vector3 Ring(Vector2 p, int j)
            {
                float ang = (float)j / sides * Mathf.PI * 2f;
                return new Vector3(Mathf.Cos(ang) * p.x, p.y, Mathf.Sin(ang) * p.x);
            }

            if (smooth)
            {
                int stride = sides + 1;
                int baseIndex = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector2 t = profile[Mathf.Min(i + 1, n - 1)] - profile[Mathf.Max(i - 1, 0)];
                    Vector2 n2 = new Vector2(t.y, -t.x);
                    n2 = n2.sqrMagnitude > 1e-12f ? n2.normalized : Vector2.up;
                    for (int j = 0; j <= sides; j++)
                    {
                        float ang = (float)j / sides * Mathf.PI * 2f;
                        float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
                        var pos = new Vector3(c * profile[i].x, profile[i].y, s * profile[i].x);
                        var nrm = new Vector3(c * n2.x, n2.y, s * n2.x);
                        V(pos, nrm, new Vector2((float)j / sides, (profile[i].y - minY) / spanY));
                    }
                }
                for (int i = 0; i < n - 1; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = baseIndex + i * stride + j;
                    int b = a + 1;
                    int c = a + stride + 1;
                    int d = a + stride;
                    SQuad(a, b, c, d);
                }
            }
            else
            {
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 p0 = profile[i], p1 = profile[i + 1];
                    Vector2 seg = p1 - p0;
                    Vector2 n2 = new Vector2(seg.y, -seg.x);
                    for (int j = 0; j < sides; j++)
                    {
                        float mid = (j + 0.5f) / sides * Mathf.PI * 2f;
                        var outward = new Vector3(Mathf.Cos(mid) * n2.x, n2.y, Mathf.Sin(mid) * n2.x);
                        Quad(Ring(p0, j), Ring(p0, j + 1), Ring(p1, j + 1), Ring(p1, j), outward);
                    }
                }
            }

            if (capBottom && profile[0].x > 1e-5f)
            {
                var c0 = new Vector3(0f, profile[0].y, 0f);
                for (int j = 0; j < sides; j++)
                    Tri(c0, Ring(profile[0], j), Ring(profile[0], j + 1), Vector3.down);
            }
            if (capTop && profile[n - 1].x > 1e-5f)
            {
                var c1 = new Vector3(0f, profile[n - 1].y, 0f);
                for (int j = 0; j < sides; j++)
                    Tri(c1, Ring(profile[n - 1], j), Ring(profile[n - 1], j + 1), Vector3.up);
            }
        }

        /// <summary>Cylinder / cone / frustum along +Y from y=0 to y=height.</summary>
        public void Cylinder(float radiusBottom, float radiusTop, float height, int sides, bool smooth, bool capBottom = true, bool capTop = true)
        {
            var profile = new List<Vector2> { new Vector2(radiusBottom, 0f), new Vector2(radiusTop, height) };
            Lathe(profile, sides, smooth, capBottom, capTop);
        }

        /// <summary>Capsule along +Y from y=0 to y=height (smooth).</summary>
        public void Capsule(float radius, float height, int sides = 16, int capRings = 5)
        {
            radius = Mathf.Min(radius, height * 0.5f);
            var profile = new List<Vector2>();
            for (int i = 0; i <= capRings; i++)
            {
                float a = -Mathf.PI * 0.5f + (float)i / capRings * Mathf.PI * 0.5f;
                profile.Add(new Vector2(Mathf.Cos(a) * radius, radius + Mathf.Sin(a) * radius));
            }
            for (int i = 0; i <= capRings; i++)
            {
                float a = (float)i / capRings * Mathf.PI * 0.5f;
                profile.Add(new Vector2(Mathf.Cos(a) * radius, height - radius + Mathf.Sin(a) * radius));
            }
            profile[0] = new Vector2(0f, 0f);
            profile[profile.Count - 1] = new Vector2(0f, height);
            Lathe(profile, sides, true);
        }

        /// <summary>Flat disc facing +Y at local y=0.</summary>
        public void Disc(float radius, int sides)
        {
            for (int j = 0; j < sides; j++)
            {
                float a0 = (float)j / sides * Mathf.PI * 2f, a1 = (float)(j + 1) / sides * Mathf.PI * 2f;
                Tri(Vector3.zero, new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius,
                    new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius, Vector3.up);
            }
        }

        /// <summary>Flat ring (annulus) facing +Y at local y=0. UV.x runs around, UV.y from inner (0) to outer (1).</summary>
        public void Annulus(float innerRadius, float outerRadius, int sides)
        {
            int baseIndex = verts.Count;
            for (int j = 0; j <= sides; j++)
            {
                float a = (float)j / sides * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                V(d * innerRadius, Vector3.up, new Vector2((float)j / sides, 0f));
                V(d * outerRadius, Vector3.up, new Vector2((float)j / sides, 1f));
            }
            for (int j = 0; j < sides; j++)
            {
                int i0 = baseIndex + j * 2;
                SQuad(i0, i0 + 1, i0 + 3, i0 + 2);
            }
        }

        /// <summary>
        /// Torus (or arc of one) lying in the local XY plane, facing +Z. Angles in degrees, 0 = +X, 90 = +Y.
        /// </summary>
        public void TorusXY(float majorRadius, float minorRadius, int segments, int tubeSides,
            float startDeg = 0f, float endDeg = 360f, bool capEnds = false)
        {
            int stride = tubeSides + 1;
            int baseIndex = verts.Count;
            for (int i = 0; i <= segments; i++)
            {
                float th = Mathf.Lerp(startDeg, endDeg, (float)i / segments) * Mathf.Deg2Rad;
                var u = new Vector3(Mathf.Cos(th), Mathf.Sin(th), 0f);
                Vector3 c = u * majorRadius;
                for (int j = 0; j <= tubeSides; j++)
                {
                    float ph = (float)j / tubeSides * Mathf.PI * 2f;
                    Vector3 n = u * Mathf.Cos(ph) + Vector3.forward * Mathf.Sin(ph);
                    V(c + n * minorRadius, n, new Vector2((float)i / segments, (float)j / tubeSides));
                }
            }
            for (int i = 0; i < segments; i++)
            for (int j = 0; j < tubeSides; j++)
            {
                int a = baseIndex + i * stride + j;
                SQuad(a, a + 1, a + stride + 1, a + stride);
            }

            if (capEnds && Mathf.Abs(endDeg - startDeg) < 359.9f)
            {
                Matrix4x4 keep = matrix;
                foreach (float deg in new[] { startDeg, endDeg })
                {
                    float th = deg * Mathf.Deg2Rad;
                    var p = new Vector3(Mathf.Cos(th), Mathf.Sin(th), 0f) * majorRadius;
                    Sphere(p, minorRadius, 1, false);
                }
                Matrix = keep;
            }
        }

        /// <summary>
        /// Extrudes a star-shaped polygon (XY plane) along Z, centered on z=0. Caps are fans from the polygon centroid.
        /// </summary>
        public void ExtrudeXY(IList<Vector2> polygon, float depth)
        {
            var poly = MakeCcw(polygon);
            int n = poly.Count;
            Vector2 centroid = Vector2.zero;
            foreach (var p in poly) centroid += p;
            centroid /= n;
            float zf = depth * 0.5f, zb = -depth * 0.5f;

            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = poly[i], p1 = poly[(i + 1) % n];
                Vector2 e = p1 - p0;
                var outward = new Vector3(e.y, -e.x, 0f);
                Quad(new Vector3(p0.x, p0.y, zb), new Vector3(p1.x, p1.y, zb), new Vector3(p1.x, p1.y, zf), new Vector3(p0.x, p0.y, zf), outward);
                Tri(new Vector3(centroid.x, centroid.y, zf), new Vector3(p0.x, p0.y, zf), new Vector3(p1.x, p1.y, zf), Vector3.forward);
                Tri(new Vector3(centroid.x, centroid.y, zb), new Vector3(p0.x, p0.y, zb), new Vector3(p1.x, p1.y, zb), Vector3.back);
            }
        }

        /// <summary>Extrudes a ring between an outer and inner polygon (same point count, XY plane) along Z.</summary>
        public void ExtrudeRingXY(IList<Vector2> outer, IList<Vector2> inner, float depth)
        {
            int n = outer.Count;
            bool flip = SignedArea(outer) < 0f;
            float zf = depth * 0.5f, zb = -depth * 0.5f;
            for (int k = 0; k < n; k++)
            {
                int i = flip ? n - 1 - k : k;
                int i1 = flip ? (i - 1 + n) % n : (i + 1) % n;
                Vector2 o0 = outer[i], o1 = outer[i1], q0 = inner[i], q1 = inner[i1];

                Vector2 eo = o1 - o0;
                Quad(new Vector3(o0.x, o0.y, zb), new Vector3(o1.x, o1.y, zb), new Vector3(o1.x, o1.y, zf), new Vector3(o0.x, o0.y, zf), new Vector3(eo.y, -eo.x, 0f));
                Vector2 ei = q1 - q0;
                Quad(new Vector3(q0.x, q0.y, zb), new Vector3(q1.x, q1.y, zb), new Vector3(q1.x, q1.y, zf), new Vector3(q0.x, q0.y, zf), new Vector3(-ei.y, ei.x, 0f));

                Quad(new Vector3(o0.x, o0.y, zf), new Vector3(o1.x, o1.y, zf), new Vector3(q1.x, q1.y, zf), new Vector3(q0.x, q0.y, zf), Vector3.forward);
                Quad(new Vector3(o0.x, o0.y, zb), new Vector3(o1.x, o1.y, zb), new Vector3(q1.x, q1.y, zb), new Vector3(q0.x, q0.y, zb), Vector3.back);
            }
        }

        /// <summary>Flat-shaded loft through horizontal rings (bottom to top, equal point counts, each ring convex-ish around its centroid).</summary>
        public void Loft(IList<Vector3[]> rings, bool capBottom, bool capTop)
        {
            for (int r = 0; r < rings.Count - 1; r++)
            {
                Vector3[] lo = rings[r], hi = rings[r + 1];
                int n = lo.Length;
                Vector3 axis = (Centroid(lo) + Centroid(hi)) * 0.5f;
                for (int j = 0; j < n; j++)
                {
                    int j1 = (j + 1) % n;
                    Vector3 mid = (lo[j] + lo[j1] + hi[j] + hi[j1]) * 0.25f;
                    var outward = new Vector3(mid.x - axis.x, 0f, mid.z - axis.z);
                    Quad(lo[j], lo[j1], hi[j1], hi[j], outward);
                }
            }
            if (capBottom) Fan(rings[0], Vector3.down);
            if (capTop) Fan(rings[rings.Count - 1], Vector3.up);
        }

        void Fan(Vector3[] ring, Vector3 outward)
        {
            Vector3 c = Centroid(ring);
            for (int j = 0; j < ring.Length; j++)
                Tri(c, ring[j], ring[(j + 1) % ring.Length], outward);
        }

        /// <summary>Flat-shaded star-shaped polygon in XZ (facing +Y) at height y.</summary>
        public void FlatPolygonXZ(IList<Vector2> polygon, float y)
        {
            Vector3 c = Vector3.zero;
            foreach (var p in polygon) c += new Vector3(p.x, y, p.y);
            c /= polygon.Count;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
                Tri(c, new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y), Vector3.up);
            }
        }

        // ------------------------------------------------------------------ output

        /// <summary>Writes the built geometry into <paramref name="mesh"/> (clearing it first).</summary>
        public void Fill(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(3, ComputeSmoothNormals());
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            Fill(mesh);
            return mesh;
        }

        List<Vector3> ComputeSmoothNormals()
        {
            var sums = new Dictionary<Vector3Int, Vector3>();
            var keys = new Vector3Int[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 p = verts[i] * 2000f;
                var key = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
                keys[i] = key;
                sums.TryGetValue(key, out Vector3 acc);
                sums[key] = acc + normals[i];
            }
            var result = new List<Vector3>(verts.Count);
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 s = sums[keys[i]];
                result.Add(s.sqrMagnitude > 1e-8f ? s.normalized : normals[i]);
            }
            return result;
        }

        // ------------------------------------------------------------------ helpers

        static Vector3 Centroid(Vector3[] pts)
        {
            Vector3 c = Vector3.zero;
            foreach (var p in pts) c += p;
            return c / Mathf.Max(1, pts.Length);
        }

        public static float SignedArea(IList<Vector2> poly)
        {
            float a = 0f;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % poly.Count];
                a += p.x * q.y - q.x * p.y;
            }
            return a * 0.5f;
        }

        static List<Vector2> MakeCcw(IList<Vector2> poly)
        {
            var list = new List<Vector2>(poly);
            if (SignedArea(list) < 0f) list.Reverse();
            return list;
        }

        /// <summary>Regular polygon in XY (CCW), first point at <paramref name="startDeg"/>.</summary>
        public static Vector2[] RegularPolygon(int sides, float radius, float startDeg = 0f)
        {
            var pts = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (startDeg + 360f * i / sides) * Mathf.Deg2Rad;
                pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            return pts;
        }

        /// <summary>Star outline in XY (CCW), points alternating outer/inner radius, first tip pointing up.</summary>
        public static Vector2[] Star(int points, float outerRadius, float innerRadius)
        {
            var pts = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float a = (90f + 180f * i / points) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? outerRadius : innerRadius;
                pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return pts;
        }

        static readonly Dictionary<int, (List<Vector3> pts, List<int> faces)> IcoCache = new Dictionary<int, (List<Vector3>, List<int>)>();

        static void BuildIcosphere(int subdiv, out List<Vector3> pts, out List<int> faces)
        {
            subdiv = Mathf.Clamp(subdiv, 0, 4);
            if (IcoCache.TryGetValue(subdiv, out var cached))
            {
                pts = new List<Vector3>(cached.pts);
                faces = cached.faces;
                return;
            }

            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            pts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (int i = 0; i < pts.Count; i++) pts[i] = pts[i].normalized;
            faces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };

            for (int s = 0; s < subdiv; s++)
            {
                var mids = new Dictionary<long, int>();
                var next = new List<int>(faces.Count * 4);
                var localPts = pts;

                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (mids.TryGetValue(key, out int idx)) return idx;
                    localPts.Add(((localPts[a] + localPts[b]) * 0.5f).normalized);
                    idx = localPts.Count - 1;
                    mids[key] = idx;
                    return idx;
                }

                for (int f = 0; f < faces.Count; f += 3)
                {
                    int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = next;
            }

            IcoCache[subdiv] = (new List<Vector3>(pts), faces);
        }
    }
}
