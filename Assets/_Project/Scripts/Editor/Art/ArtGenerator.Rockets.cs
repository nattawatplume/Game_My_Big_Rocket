using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    public static partial class ArtGenerator
    {
        // Unit-size face parts (scaled uniformly per rocket).
        static Mesh rEyeWhite, rPupil, rShine, rMouthO, rMouthSmile, rMouthLine, rMouthSmirk, rBrow, rSunglasses;

        static void BuildRocketFaceMeshes()
        {
            var mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.1f, 0.12f, 0.05f), 2, false);
            rEyeWhite = SaveMesh("Rocket_EyeWhite", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.06f, 0.075f, 0.03f), 2, false);
            rPupil = SaveMesh("Rocket_Pupil", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, 0.022f, 1, false);
            rShine = SaveMesh("Rocket_EyeShine", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.05f, 0.06f, 0.03f), 2, false);
            rMouthO = SaveMesh("Rocket_MouthO", mb);

            mb = MB();
            mb.TorusXY(0.08f, 0.018f, 12, 6, 200f, 340f, true);
            rMouthSmile = SaveMesh("Rocket_MouthSmile", mb);

            mb = MB();
            mb.Box(Vector3.zero, new Vector3(0.14f, 0.03f, 0.03f), 0.01f);
            rMouthLine = SaveMesh("Rocket_MouthLine", mb);

            mb = MB();
            mb.TorusXY(0.08f, 0.018f, 10, 6, 215f, 305f, true);
            rMouthSmirk = SaveMesh("Rocket_MouthSmirk", mb);

            mb = MB();
            mb.Box(Vector3.zero, new Vector3(0.12f, 0.028f, 0.03f), 0.01f);
            rBrow = SaveMesh("Rocket_Brow", mb);

            mb = MB();
            mb.Box(new Vector3(-0.11f, 0f, 0f), new Vector3(0.17f, 0.12f, 0.04f), 0.03f);
            mb.Box(new Vector3(0.11f, 0f, 0f), new Vector3(0.17f, 0.12f, 0.04f), 0.03f);
            mb.Box(new Vector3(0f, 0.03f, 0f), new Vector3(0.08f, 0.025f, 0.03f), 0.008f);
            rSunglasses = SaveMesh("Rocket_Sunglasses", mb);
        }

        // ------------------------------------------------------------------ geometry helpers

        /// <summary>Rocket body tube from y0 to y1 with a softly tapered tail (top left open for the nose).</summary>
        static void RocketTube(MeshBuilder mb, float r, float y0, float y1)
        {
            float len = y1 - y0;
            var p = Pts(0f, y0, r * 0.8f, y0, r * 0.93f, y0 + len * 0.03f, r, y0 + len * 0.09f, r, y1);
            mb.Lathe(p, 18, true);
        }

        /// <summary>Ogive nose from (radius, y0) to the tip at y1.</summary>
        static void RocketNose(MeshBuilder mb, float r, float y0, float y1)
        {
            var p = new List<Vector2>();
            const int steps = 8;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float rad = r * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(t, 1.8f)));
                p.Add(new Vector2(rad, Mathf.Lerp(y0, y1, t)));
            }
            p[steps] = new Vector2(0f, y1);
            mb.Lathe(p, 18, true, capBottom: true);
        }

        static void RocketBand(MeshBuilder mb, float r, float y, float h)
        {
            mb.At(new Vector3(0f, y, 0f));
            mb.Cylinder(r, r, h, 18, true);
            mb.Identity();
        }

        static Mesh RocketNozzle(string name, float r, float h)
        {
            var mb = MB();
            mb.Cylinder(r * 0.78f, r * 0.55f, h, 10, false);
            return SaveMesh(name, mb);
        }

        static void RocketFins(MeshBuilder mb, int count, float startDeg, float bodyR, float rootY0, float rootY1,
            float span, float drop, float tipLen, float thickness)
        {
            var poly = Pts(0f, rootY0, span, rootY0 - drop, span, rootY0 - drop + tipLen, 0f, rootY1);
            for (int i = 0; i < count; i++)
            {
                var rot = Quaternion.Euler(0f, startDeg + 360f * i / count, 0f);
                mb.At(rot * new Vector3(bodyR * 0.85f, 0f, 0f), rot);
                mb.ExtrudeXY(poly, thickness);
            }
            mb.Identity();
        }

        static void RocketBooster(MeshBuilder mb, Vector3 basePos, float r, float len)
        {
            mb.At(basePos);
            RocketTube(mb, r, 0f, len * 0.72f);
            RocketNose(mb, r * 1.01f, len * 0.72f, len);
            mb.At(basePos + new Vector3(0f, -len * 0.06f, 0f));
            mb.Cylinder(r * 0.75f, r * 0.55f, len * 0.07f, 8, false);
            mb.Identity();
        }

        /// <summary>Places a face part on a cylinder of radius r (local +Z = surface normal), uniformly scaled.</summary>
        static GameObject FacePart(string name, Transform parent, Mesh mesh, Material mat, float r, float x, float y,
            float scale, float protrude, float rollDeg = 0f)
        {
            float z = Mathf.Sqrt(Mathf.Max(r * r - x * x, 0.0001f));
            var n = new Vector3(x, 0f, z).normalized;
            var go = MeshNode(name, parent, mesh, mat, new Vector3(x, y, z) + n * protrude,
                Quaternion.LookRotation(n, Vector3.up) * Quaternion.Euler(0f, 0f, rollDeg), false);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static void RocketEyes(Transform visual, float r, float y, float scale, float pupilInward = 0.01f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = FacePart(side < 0 ? "EyeL" : "EyeR", visual, rEyeWhite, mEyeWhite, r, side * r * 0.4f, y, scale, -0.02f * scale);
                var pupil = MeshNode("Pupil", eye.transform, rPupil, mInk, new Vector3(-side * pupilInward, -0.012f, 0.03f), Quaternion.identity, false);
                MeshNode("Shine", pupil.transform, rShine, mShine, new Vector3(0.02f, 0.03f, 0.03f), Quaternion.identity, false);
            }
        }

        static GameObject RocketRoot(string name, out Transform visual)
        {
            var root = new GameObject(name);
            visual = Node("Visual", root.transform).transform;
            Node("Flame", root.transform, Vector3.zero);
            return root;
        }

        // ------------------------------------------------------------------ the five rockets

        static void BuildRockets()
        {
            BuildRocketFaceMeshes();
            BuildRocketT1();
            BuildRocketT2();
            BuildRocketT3();
            BuildRocketT4();
            BuildRocketT5();
        }

        /// <summary>T1 Firecracker: tiny red/white cracker with a fuse and a shocked face (~0.36 tall).</summary>
        static void BuildRocketT1()
        {
            const float r = 0.075f;
            var mb = MB();
            mb.Lathe(Pts(0f, 0.02f, r * 0.95f, 0.02f, r, 0.035f, r, 0.27f), 16, true);
            var body = SaveMesh("Rocket_T1_Body", mb);

            mb = MB();
            RocketBand(mb, r * 1.05f, 0.05f, 0.022f);
            RocketBand(mb, r * 1.05f, 0.205f, 0.022f);
            mb.At(new Vector3(0f, 0.27f, 0f)).Cylinder(r * 1.06f, r * 0.3f, 0.06f, 16, true);
            mb.Identity();
            mb.Lathe(Pts(0f, 0f, r * 0.9f, 0f, r * 0.95f, 0.02f), 16, true); // little foot cap
            RocketFins(mb, 3, 90f, r, 0.02f, 0.09f, 0.045f, 0.02f, 0.04f, 0.012f);
            var paper = SaveMesh("Rocket_T1_Paper", mb);

            mb = MB();
            mb.At(new Vector3(0f, 0.325f, 0f), Quaternion.Euler(0f, 0f, 18f)).Cylinder(0.009f, 0.007f, 0.07f, 6, false);
            var fuse = SaveMesh("Rocket_T1_Fuse", mb);

            var root = RocketRoot("T1_Firecracker", out var v);
            MeshNode("Body", v, body, mRed, Vector3.zero);
            MeshNode("Paper", v, paper, mCream, Vector3.zero);
            MeshNode("Fuse", v, fuse, mDoor, Vector3.zero);
            const float s = r * 3f;
            RocketEyes(v, r, 0.155f, s);
            FacePart("Mouth", v, rMouthO, mInk, r, 0f, 0.1f, s, -0.01f * s);
            SavePrefab(root, "Rockets/T1_Firecracker");
        }

        /// <summary>T2 Copper Dart: slender copper rocket with swept fins and a big grin (~0.72 tall).</summary>
        static void BuildRocketT2()
        {
            const float r = 0.085f;
            var mb = MB();
            RocketTube(mb, r, 0.05f, 0.5f);
            var body = SaveMesh("Rocket_T2_Body", mb);

            mb = MB();
            RocketNose(mb, r * 1.02f, 0.5f, 0.72f);
            var nose = SaveMesh("Rocket_T2_Nose", mb);

            mb = MB();
            RocketBand(mb, r * 1.04f, 0.44f, 0.03f);
            RocketFins(mb, 3, 90f, r, 0.07f, 0.22f, 0.09f, 0.05f, 0.07f, 0.016f);
            var trim = SaveMesh("Rocket_T2_Trim", mb);

            var nozzle = RocketNozzle("Rocket_T2_Nozzle", r, 0.06f);

            var root = RocketRoot("T2_CopperDart", out var v);
            MeshNode("Body", v, body, mCopper, Vector3.zero);
            MeshNode("Nose", v, nose, mCream, Vector3.zero);
            MeshNode("Trim", v, trim, mCopperDark, Vector3.zero);
            MeshNode("Nozzle", v, nozzle, mMetal, Vector3.zero);
            const float s = r * 3f;
            RocketEyes(v, r, 0.34f, s);
            FacePart("Mouth", v, rMouthSmile, mInk, r, 0f, 0.3f, s, -0.01f * s);
            SavePrefab(root, "Rockets/T2_CopperDart");
        }

        /// <summary>T3 Silver Spear: silver body, two side boosters, determined brows (~1.0 tall).</summary>
        static void BuildRocketT3()
        {
            const float r = 0.12f;
            var mb = MB();
            RocketTube(mb, r, 0.07f, 0.68f);
            var body = SaveMesh("Rocket_T3_Body", mb);

            mb = MB();
            RocketNose(mb, r * 1.02f, 0.68f, 1.0f);
            RocketBand(mb, r * 1.04f, 0.13f, 0.035f);
            RocketBand(mb, r * 1.04f, 0.6f, 0.035f);
            RocketFins(mb, 4, 45f, r, 0.08f, 0.3f, 0.12f, 0.06f, 0.1f, 0.02f);
            var trim = SaveMesh("Rocket_T3_Trim", mb);

            mb = MB();
            RocketBooster(mb, new Vector3(r + 0.055f, 0.06f, 0f), 0.06f, 0.42f);
            RocketBooster(mb, new Vector3(-(r + 0.055f), 0.06f, 0f), 0.06f, 0.42f);
            var boosters = SaveMesh("Rocket_T3_Boosters", mb);

            var nozzle = RocketNozzle("Rocket_T3_Nozzle", r, 0.08f);

            var root = RocketRoot("T3_SilverSpear", out var v);
            MeshNode("Body", v, body, mSilver, Vector3.zero);
            MeshNode("Trim", v, trim, mSilverBlue, Vector3.zero);
            MeshNode("Boosters", v, boosters, mSilver, Vector3.zero);
            MeshNode("Nozzle", v, nozzle, mMetal, Vector3.zero);
            const float s = r * 3f;
            RocketEyes(v, r, 0.45f, s, 0f);
            FacePart("BrowL", v, rBrow, mInk, r, -r * 0.4f, 0.45f + 0.15f * s, s, 0.01f * s, -18f);
            FacePart("BrowR", v, rBrow, mInk, r, r * 0.4f, 0.45f + 0.15f * s, s, 0.01f * s, 18f);
            FacePart("Mouth", v, rMouthLine, mInk, r, 0f, 0.37f, s, -0.005f);
            SavePrefab(root, "Rockets/T3_SilverSpear");
        }

        /// <summary>T4 Golden Giant: big cream body with gold stripes, big fins, one raised eyebrow (~1.35 tall).</summary>
        static void BuildRocketT4()
        {
            const float r = 0.19f;
            var mb = MB();
            RocketTube(mb, r, 0.09f, 0.92f);
            var body = SaveMesh("Rocket_T4_Body", mb);

            mb = MB();
            RocketNose(mb, r * 1.02f, 0.92f, 1.35f);
            RocketBand(mb, r * 1.03f, 0.18f, 0.06f);
            RocketBand(mb, r * 1.03f, 0.3f, 0.025f);
            RocketBand(mb, r * 1.03f, 0.84f, 0.045f);
            RocketFins(mb, 4, 45f, r, 0.1f, 0.44f, 0.2f, 0.1f, 0.16f, 0.03f);
            var gold = SaveMesh("Rocket_T4_Gold", mb);

            var nozzle = RocketNozzle("Rocket_T4_Nozzle", r, 0.1f);

            var root = RocketRoot("T4_GoldenGiant", out var v);
            MeshNode("Body", v, body, mGoldCream, Vector3.zero);
            MeshNode("Gold", v, gold, mGold, Vector3.zero);
            MeshNode("Nozzle", v, nozzle, mMetal, Vector3.zero);
            const float s = r * 3f;
            RocketEyes(v, r, 0.6f, s);
            FacePart("BrowL", v, rBrow, mInk, r, -r * 0.4f, 0.6f + 0.15f * s, s, 0.01f * s, 6f);
            FacePart("BrowR", v, rBrow, mInk, r, r * 0.4f, 0.6f + 0.21f * s, s, 0.01f * s, 22f);
            FacePart("Mouth", v, rMouthSmirk, mInk, r, 0.02f, 0.53f, s, -0.01f * s);
            SavePrefab(root, "Rockets/T4_GoldenGiant");
        }

        /// <summary>T5 Mega Overkill: huge crystal-pink rocket, 4 boosters, crystal tip and sunglasses (~1.8 tall).</summary>
        static void BuildRocketT5()
        {
            const float r = 0.26f;
            var mb = MB();
            RocketTube(mb, r, 0.11f, 1.2f);
            var body = SaveMesh("Rocket_T5_Body", mb);

            mb = MB();
            RocketNose(mb, r * 1.02f, 1.2f, 1.64f);
            var nose = SaveMesh("Rocket_T5_Nose", mb);

            mb = MB();
            mb.Lathe(Pts(0f, 1.58f, 0.1f, 1.67f, 0f, 1.8f), 6, false); // faceted crystal tip
            RocketBand(mb, r * 1.03f, 0.2f, 0.05f);
            RocketBand(mb, r * 1.03f, 1.1f, 0.05f);
            var trim = SaveMesh("Rocket_T5_Trim", mb);

            mb = MB();
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, 45f + 90f * i, 0f);
                RocketBooster(mb, rot * new Vector3(r + 0.075f, 0f, 0f) + new Vector3(0f, 0.08f, 0f), 0.09f, 0.66f);
            }
            var boosters = SaveMesh("Rocket_T5_Boosters", mb);

            mb = MB();
            RocketFins(mb, 4, 0f, r, 0.12f, 0.48f, 0.24f, 0.12f, 0.18f, 0.035f);
            var fins = SaveMesh("Rocket_T5_Fins", mb);

            var nozzle = RocketNozzle("Rocket_T5_Nozzle", r, 0.12f);

            var root = RocketRoot("T5_MegaOverkill", out var v);
            MeshNode("Body", v, body, mCrystal, Vector3.zero);
            MeshNode("Nose", v, nose, mCrystalLight, Vector3.zero);
            MeshNode("Trim", v, trim, mLavender, Vector3.zero);
            MeshNode("Boosters", v, boosters, mLavender, Vector3.zero);
            MeshNode("Fins", v, fins, mCrystal, Vector3.zero);
            MeshNode("Nozzle", v, nozzle, mMetal, Vector3.zero);
            const float s = r * 3f;
            // Cool guy: sunglasses instead of visible eyes.
            FacePart("Sunglasses", v, rSunglasses, mInk, r, 0f, 0.83f, s * 1.15f, 0.025f * s);
            FacePart("Mouth", v, rMouthSmile, mInk, r, 0f, 0.7f, s, -0.01f * s);
            SavePrefab(root, "Rockets/T5_MegaOverkill");
        }
    }
}
