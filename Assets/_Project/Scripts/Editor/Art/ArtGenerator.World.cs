using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IslandRocketRumble.EditorTools
{
    public static partial class ArtGenerator
    {
        // ------------------------------------------------------------------ props

        static void BuildProps()
        {
            BuildTree();
            BuildTreePine();
            BuildHouse();
            BuildCrab();
            BuildDuck();
            BuildSeagull();
        }

        static void BuildTree()
        {
            var mb = MB();
            mb.Cylinder(0.09f, 0.065f, 0.75f, 6, false);
            var trunk = SaveMesh("Tree_Trunk", mb);

            mb = MB();
            mb.Sphere(new Vector3(0f, 1.0f, 0f), new Vector3(0.5f, 0.44f, 0.5f), 1, true, 0.06f, 7);
            mb.Sphere(new Vector3(0.26f, 1.3f, 0.08f), new Vector3(0.26f, 0.24f, 0.26f), 1, true, 0.05f, 11);
            var leaves = SaveMesh("Tree_Leaves", mb);

            var root = new GameObject("Tree");
            MeshNode("Trunk", root.transform, trunk, mTrunk, Vector3.zero);
            MeshNode("Leaves", root.transform, leaves, mLeaf, Vector3.zero);
            SavePrefab(root, "Props/Tree");
        }

        static void BuildTreePine()
        {
            var mb = MB();
            mb.Cylinder(0.08f, 0.06f, 0.45f, 6, false);
            var trunk = SaveMesh("TreePine_Trunk", mb);

            mb = MB();
            mb.At(new Vector3(0f, 0.3f, 0f)).Cylinder(0.55f, 0f, 0.75f, 7, false);
            mb.At(new Vector3(0f, 0.72f, 0f), Quaternion.Euler(0f, 25f, 0f)).Cylinder(0.43f, 0f, 0.62f, 7, false);
            mb.At(new Vector3(0f, 1.12f, 0f), Quaternion.Euler(0f, 50f, 0f)).Cylinder(0.3f, 0f, 0.5f, 7, false);
            var leaves = SaveMesh("TreePine_Leaves", mb);

            var root = new GameObject("TreePine");
            MeshNode("Trunk", root.transform, trunk, mTrunk, Vector3.zero);
            MeshNode("Leaves", root.transform, leaves, mLeafDark, Vector3.zero);
            SavePrefab(root, "Props/TreePine");
        }

        /// <summary>Cute little house with a face on its front (+Z).</summary>
        static void BuildHouse()
        {
            var mb = MB();
            mb.Box(new Vector3(0f, 0.4f, 0f), new Vector3(1.0f, 0.8f, 0.9f), 0.05f);
            mb.At(new Vector3(0f, 0.8f, 0f)).ExtrudeXY(Pts(-0.5f, 0f, 0.5f, 0f, 0f, 0.38f), 0.88f);
            var walls = SaveMesh("House_Walls", mb);

            const float slope = 37.4f;
            mb = MB();
            mb.At(new Vector3(-0.296f, 1.038f, 0f), Quaternion.Euler(0f, 0f, slope)).Box(Vector3.zero, new Vector3(0.78f, 0.07f, 1.06f), 0.02f);
            mb.At(new Vector3(0.296f, 1.038f, 0f), Quaternion.Euler(0f, 0f, -slope)).Box(Vector3.zero, new Vector3(0.78f, 0.07f, 1.06f), 0.02f);
            mb.Identity();
            mb.Box(new Vector3(0.28f, 1.15f, -0.22f), new Vector3(0.13f, 0.32f, 0.13f), 0.02f); // chimney
            var roof = SaveMesh("House_Roof", mb);

            mb = MB();
            mb.Box(new Vector3(0f, 0.19f, 0.455f), new Vector3(0.24f, 0.36f, 0.04f), 0.03f);
            var door = SaveMesh("House_Door", mb);

            mb = MB();
            mb.Sphere(new Vector3(-0.22f, 0.56f, 0.452f), new Vector3(0.06f, 0.075f, 0.02f), 1, false);
            mb.Sphere(new Vector3(0.22f, 0.56f, 0.452f), new Vector3(0.06f, 0.075f, 0.02f), 1, false);
            mb.At(new Vector3(0f, 0.5f, 0.455f)).TorusXY(0.06f, 0.014f, 10, 6, 200f, 340f, true);
            mb.Identity();
            mb.Sphere(new Vector3(0.07f, 0.19f, 0.48f), 0.018f, 1, false); // door knob
            var face = SaveMesh("House_Face", mb);

            mb = MB();
            mb.Sphere(new Vector3(-0.34f, 0.45f, 0.452f), new Vector3(0.07f, 0.035f, 0.012f), 1, false);
            mb.Sphere(new Vector3(0.34f, 0.45f, 0.452f), new Vector3(0.07f, 0.035f, 0.012f), 1, false);
            var blush = SaveMesh("House_Blush", mb);

            var root = new GameObject("House");
            MeshNode("Walls", root.transform, walls, mCream, Vector3.zero);
            MeshNode("Roof", root.transform, roof, mRoof, Vector3.zero);
            MeshNode("Door", root.transform, door, mDoor, Vector3.zero);
            MeshNode("Face", root.transform, face, mInk, Vector3.zero, Quaternion.identity, false);
            MeshNode("Blush", root.transform, blush, mBlush, Vector3.zero, Quaternion.identity, false);
            SavePrefab(root, "Props/House");
        }

        static void BuildCrab()
        {
            var mb = MB();
            mb.Sphere(new Vector3(0f, 0.11f, 0f), new Vector3(0.17f, 0.08f, 0.13f), 1, true);
            for (int side = -1; side <= 1; side += 2)
            {
                mb.Sphere(new Vector3(side * 0.2f, 0.12f, 0.1f), 0.06f, 1, true);
                mb.Identity();
                mb.Box(new Vector3(side * 0.22f, 0.16f, 0.15f), new Vector3(0.03f, 0.05f, 0.07f), 0.008f);
                Beam(mb, new Vector3(side * 0.12f, 0.1f, 0.05f), new Vector3(side * 0.18f, 0.115f, 0.09f), 0.03f);
                for (int k = 0; k < 3; k++)
                {
                    float z = -0.07f + k * 0.05f;
                    Beam(mb, new Vector3(side * 0.12f, 0.09f, z), new Vector3(side * 0.25f, 0.005f, z - 0.02f), 0.025f);
                }
                mb.Identity();
                mb.At(new Vector3(side * 0.05f, 0.16f, 0.07f)).Cylinder(0.012f, 0.012f, 0.08f, 5, false);
                mb.Identity();
            }
            var body = SaveMesh("Crab_Body", mb);

            mb = MB();
            mb.Sphere(new Vector3(-0.05f, 0.255f, 0.075f), 0.032f, 1, false);
            mb.Sphere(new Vector3(0.05f, 0.255f, 0.075f), 0.032f, 1, false);
            var eyes = SaveMesh("Crab_Eyes", mb);

            mb = MB();
            mb.Sphere(new Vector3(-0.05f, 0.258f, 0.1f), 0.016f, 1, false);
            mb.Sphere(new Vector3(0.05f, 0.258f, 0.1f), 0.016f, 1, false);
            mb.At(new Vector3(0f, 0.13f, 0.128f)).TorusXY(0.028f, 0.007f, 8, 5, 200f, 340f, true);
            var pupils = SaveMesh("Crab_Pupils", mb);

            var root = new GameObject("Crab");
            var visual = Node("Visual", root.transform).transform;
            MeshNode("Body", visual, body, mCrab, Vector3.zero);
            MeshNode("Eyes", visual, eyes, mEyeWhite, Vector3.zero);
            MeshNode("Pupils", visual, pupils, mInk, Vector3.zero, Quaternion.identity, false);
            root.AddComponent<CrabWander>();
            SavePrefab(root, "Props/Crab");
        }

        static void BuildDuck()
        {
            var mb = MB();
            mb.Sphere(new Vector3(0f, 0.12f, 0f), new Vector3(0.2f, 0.14f, 0.25f), 2, false);
            mb.Sphere(new Vector3(0f, 0.31f, 0.12f), 0.12f, 2, false);
            mb.Sphere(new Vector3(0f, 0.2f, -0.22f), new Vector3(0.07f, 0.07f, 0.08f), 1, false);
            var body = SaveMesh("Duck_Body", mb);

            mb = MB();
            mb.Sphere(new Vector3(0f, 0.29f, 0.24f), new Vector3(0.07f, 0.028f, 0.06f), 1, false);
            var beak = SaveMesh("Duck_Beak", mb);

            mb = MB();
            mb.Sphere(new Vector3(-0.065f, 0.34f, 0.21f), 0.022f, 1, false);
            mb.Sphere(new Vector3(0.065f, 0.34f, 0.21f), 0.022f, 1, false);
            var eyes = SaveMesh("Duck_Eyes", mb);

            var root = new GameObject("Duck");
            var visual = Node("Visual", root.transform).transform;
            MeshNode("Body", visual, body, mDuck, Vector3.zero);
            MeshNode("Beak", visual, beak, mBeak, Vector3.zero);
            MeshNode("Eyes", visual, eyes, mInk, Vector3.zero, Quaternion.identity, false);
            var bob = root.AddComponent<Bobbing>();
            bob.amplitude = 0.04f;
            bob.frequency = 0.9f;
            bob.tiltAmount = 6f;
            SavePrefab(root, "Props/Duck");
        }

        static Mesh SeagullWing(string name, int side, bool tip)
        {
            var mb = MB();
            if (tip) mb.Box(new Vector3(side * 0.37f, 0f, -0.01f), new Vector3(0.1f, 0.018f, 0.11f), 0.006f);
            else mb.Box(new Vector3(side * 0.17f, 0f, 0f), new Vector3(0.34f, 0.022f, 0.14f), 0.008f);
            return SaveMesh(name, mb);
        }

        static void BuildSeagull()
        {
            var mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.09f, 0.085f, 0.2f), 2, false);
            mb.Sphere(new Vector3(0f, 0.06f, 0.17f), 0.075f, 2, false);
            mb.Box(new Vector3(0f, 0.01f, -0.22f), new Vector3(0.1f, 0.02f, 0.1f), 0.008f);
            var body = SaveMesh("Seagull_Body", mb);

            mb = MB();
            mb.At(new Vector3(0f, 0.05f, 0.235f), Quaternion.Euler(90f, 0f, 0f)).Cylinder(0.022f, 0f, 0.07f, 6, false);
            var beak = SaveMesh("Seagull_Beak", mb);

            mb = MB();
            mb.Sphere(new Vector3(-0.045f, 0.085f, 0.215f), 0.012f, 1, false);
            mb.Sphere(new Vector3(0.045f, 0.085f, 0.215f), 0.012f, 1, false);
            var eyes = SaveMesh("Seagull_Eyes", mb);

            var wingL = SeagullWing("Seagull_WingL", -1, false);
            var wingR = SeagullWing("Seagull_WingR", 1, false);
            var tipL = SeagullWing("Seagull_WingTipL", -1, true);
            var tipR = SeagullWing("Seagull_WingTipR", 1, true);

            var root = new GameObject("Seagull");
            var visual = Node("Visual", root.transform).transform;
            MeshNode("Body", visual, body, mEyeWhite, Vector3.zero);
            MeshNode("Beak", visual, beak, mBeak, Vector3.zero);
            MeshNode("Eyes", visual, eyes, mInk, Vector3.zero, Quaternion.identity, false);
            var wl = MeshNode("WingL", visual, wingL, mEyeWhite, new Vector3(-0.07f, 0.03f, 0f));
            MeshNode("Tip", wl.transform, tipL, mGullGrey, Vector3.zero);
            var wr = MeshNode("WingR", visual, wingR, mEyeWhite, new Vector3(0.07f, 0.03f, 0f));
            MeshNode("Tip", wr.transform, tipR, mGullGrey, Vector3.zero);
            var circle = root.AddComponent<SeagullCircle>();
            circle.wingL = wl.transform;
            circle.wingR = wr.transform;
            SavePrefab(root, "Props/Seagull");
        }

        // ------------------------------------------------------------------ islands

        static Vector3[] IslandRing(float half, float cut, float y, float jitter, System.Random rng)
        {
            var c = new[]
            {
                new Vector2(half, -half + cut), new Vector2(half, half - cut), new Vector2(half - cut, half), new Vector2(-half + cut, half),
                new Vector2(-half, half - cut), new Vector2(-half, -half + cut), new Vector2(-half + cut, -half), new Vector2(half - cut, -half),
            };
            var ring = new Vector3[16];
            for (int i = 0; i < 8; i++)
            {
                Vector2 a = c[i], b = c[(i + 1) % 8];
                Vector2 m = (a + b) * 0.5f;
                float ja = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
                float jm = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
                ring[i * 2] = new Vector3(a.x * ja, y, a.y * ja);
                ring[i * 2 + 1] = new Vector3(m.x * jm, y, m.y * jm);
            }
            return ring;
        }

        static void IslandMeshes(string prefix, float size, float depth, int seed, out Mesh top, out Mesh side)
        {
            var mb = MB();
            mb.Box(new Vector3(0f, -0.25f, 0f), new Vector3(size, 0.5f, size), 0.18f);
            top = SaveMesh(prefix + "_Top", mb);

            float h = size * 0.5f;
            var rng = new System.Random(seed);
            var rings = new List<Vector3[]>
            {
                IslandRing(h - 0.45f, size * 0.13f, -depth, 0.02f, rng),
                IslandRing(h - 0.2f, size * 0.1f, -depth * 0.55f, 0.03f, rng),
                IslandRing(h - 0.1f, size * 0.09f, -0.35f - depth * 0.2f, 0.02f, rng),
                IslandRing(h - 0.04f, size * 0.07f, -0.35f, 0f, rng),
            };
            mb = MB();
            mb.Loft(rings, true, false);
            side = SaveMesh(prefix + "_Side", mb);
        }

        static void BuildIslands()
        {
            // Craft ring: ~20 short flat dashes around radius 1.3
            var mb = MB();
            const int dashes = 20;
            const float ringR = 1.3f;
            for (int i = 0; i < dashes; i++)
            {
                float a = (i + 0.5f) / dashes * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(a) * ringR, 0.015f, Mathf.Sin(a) * ringR);
                var tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                mb.At(pos, Quaternion.LookRotation(tangent, Vector3.up));
                mb.Box(Vector3.zero, new Vector3(0.13f, 0.03f, 0.25f), 0.012f);
            }
            var craftRing = SaveMesh("CraftRing", mb);

            // Player island
            IslandMeshes("Island_Player", 8f, 3.0f, 12345, out var pTop, out var pSide);
            var root = new GameObject("PlayerIsland");
            MeshNode("Top", root.transform, pTop, mIslandTop, Vector3.zero);
            MeshNode("Side", root.transform, pSide, mIslandSide, Vector3.zero);
            var props = Node("Props", root.transform).transform;
            PlacePrefab("Props/Tree", props, new Vector3(-3.3f, 0f, 2.9f), 10f);
            PlacePrefab("Props/TreePine", props, new Vector3(3.2f, 0f, 3.1f), 0f, 1.05f);
            PlacePrefab("Props/Tree", props, new Vector3(3.3f, 0f, -3.2f), 40f, 0.85f);
            PlacePrefab("Props/TreePine", props, new Vector3(-3.4f, 0f, -3.0f), 15f, 0.9f);
            PlacePrefab("Props/Tree", props, new Vector3(0.9f, 0f, 3.4f), 70f, 0.75f);
            PlacePrefab("Props/House", props, new Vector3(-2.1f, 0f, 2.3f), 225f);
            PlacePrefab("Props/Crab", props, new Vector3(2.4f, 0f, 0.6f), 200f);
            PlacePrefab("Props/Crab", props, new Vector3(-2.6f, 0f, -1.2f), 150f);
            var ring = MeshNode("CraftRing", root.transform, craftRing, mCraftRing, new Vector3(0f, 0.02f, -1.8f), Quaternion.identity, false);
            var spin = ring.AddComponent<SimpleSpin>();
            spin.speed = 12f;
            SavePrefab(root, "Islands/PlayerIsland");

            // Robot island (top at y=0.8 in the world, so the cliff runs deeper to reach the sea)
            IslandMeshes("Island_Robot", 5f, 3.8f, 777, out var rTop, out var rSide);
            root = new GameObject("RobotIsland");
            MeshNode("Top", root.transform, rTop, mIslandTop, Vector3.zero);
            MeshNode("Side", root.transform, rSide, mIslandSide, Vector3.zero);
            props = Node("Props", root.transform).transform;
            var rockSpots = new[]
            {
                new Vector3(-1.9f, 0f, 1.8f), new Vector3(1.85f, 0f, -1.9f), new Vector3(1.9f, 0f, 1.7f), new Vector3(-1.7f, 0f, -1.95f),
            };
            for (int i = 0; i < rockSpots.Length; i++)
            {
                mb = MB();
                float s = 0.22f + 0.07f * (i % 3);
                mb.Sphere(new Vector3(0f, s * 0.55f, 0f), new Vector3(s * 1.2f, s, s), i % 2 == 0 ? 0 : 1, true, 0.18f, 31 + i * 17);
                var rock = SaveMesh("Rock_" + (i + 1), mb);
                MeshNode("Rock" + (i + 1), props, rock, mRock, rockSpots[i], Quaternion.Euler(0f, i * 67f, 0f));
            }
            SavePrefab(root, "Islands/RobotIsland");
        }

        // ------------------------------------------------------------------ FX

        static void BuildFx()
        {
            var mb = MB();
            mb.Lathe(Pts(0.5f, 0f, 0.5f, 1f), 20, true);
            var beam = SaveMesh("FX_LightBeam", mb);
            var root = new GameObject("LightBeam");
            AddMesh(root, beam, mBeam, false).receiveShadows = false;
            root.AddComponent<TintRenderer>();
            SavePrefab(root, "FX/LightBeam");

            mb = MB();
            mb.At(new Vector3(0f, 0.03f, 0f)).Annulus(0.86f, 1f, 48);
            var ring = SaveMesh("FX_WarningRing", mb);
            root = new GameObject("WarningRing");
            AddMesh(root, ring, mRing, false).receiveShadows = false;
            root.AddComponent<TintRenderer>();
            SavePrefab(root, "FX/WarningRing");

            var rng = new System.Random(4242);
            var poly = new List<Vector2>();
            const int points = 18;
            for (int i = 0; i < points; i++)
            {
                float a = (float)i / points * Mathf.PI * 2f;
                float r = (i % 2 == 0 ? 1f : 0.84f) * (0.94f + 0.06f * (float)rng.NextDouble());
                poly.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            mb = MB();
            mb.FlatPolygonXZ(poly, 0.015f);
            var crater = SaveMesh("FX_Crater", mb);
            root = new GameObject("Crater");
            AddMesh(root, crater, mScorch, false).receiveShadows = false;
            SavePrefab(root, "FX/Crater");
        }

        // ------------------------------------------------------------------ sea

        static void BuildSea()
        {
            const int cells = 60;
            const float size = 120f;
            var mb = MB();
            int stride = cells + 1;
            for (int z = 0; z <= cells; z++)
            for (int x = 0; x <= cells; x++)
            {
                var p = new Vector3(-size * 0.5f + size * x / cells, 0f, -size * 0.5f + size * z / cells);
                mb.V(p, Vector3.up, new Vector2((float)x / cells, (float)z / cells));
            }
            for (int z = 0; z < cells; z++)
            for (int x = 0; x < cells; x++)
            {
                int a = z * stride + x;
                mb.SQuad(a, a + 1, a + stride + 1, a + stride);
            }
            var sea = SaveMesh("Sea", mb);
            var bounds = sea.bounds;
            bounds.Expand(new Vector3(0f, 2f, 0f)); // vertex waves move above/below the flat plane
            sea.bounds = bounds;

            var root = new GameObject("Sea");
            var r = AddMesh(root, sea, mSea, false);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
            SavePrefab(root, "Environment/Sea");
        }
    }
}
