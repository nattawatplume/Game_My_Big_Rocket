using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>
    /// Dress-up accessories (hats, face items, back items) for Pip and Momo, plus Resources/CosmeticLibrary.asset.
    /// Every accessory is authored in character-root space (same space as the ~1 m bean in ArtGenerator.Characters),
    /// so the runtime just parents it under "Visual" at identity.
    /// </summary>
    public static partial class ArtGenerator
    {
        const string CosmeticsDir = PrefabsDir + "/Cosmetics";
        const string LibraryPath = Root + "/Resources/CosmeticLibrary.asset";

        // Indexed exactly like CosmeticCatalog (null = no prefab).
        static readonly string[] HatFiles = { null, "Cap", "Crown", "FlowerCrown", "CatEars", "PartyHat", "ChefHat", "FrogHat", "BunnyEars", null };
        static readonly string[] FaceFiles = { null, "RoundGlasses", "Sunglasses", "Mustache", "HeartStickers" };
        static readonly string[] BackFiles = { null, "Jetpack", "Cape", "Backpack", "AngelWings" };

        static Material mCosHeart;

        static void BuildCosmetics()
        {
            foreach (var d in new[] { CosmeticsDir + "/Hats", CosmeticsDir + "/Faces", CosmeticsDir + "/Backs", Root + "/Resources" })
                Directory.CreateDirectory(d);
            AssetDatabase.Refresh();

            mCosHeart = Toon("IRR_Toon_Cos_Heart", Palette.Hex("FF6F91"), 1f);

            BuildCap(); BuildCrown(); BuildFlowerCrown(); BuildCatEars(); BuildPartyHat(); BuildChefHat(); BuildFrogHat(); BuildBunnyEars();
            BuildRoundGlasses(); BuildSunglasses(); BuildMustache(); BuildHeartStickers();
            BuildJetpack(); BuildCape(); BuildBackpack(); BuildAngelWings();
            BuildCosmeticLibrary();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Saves <paramref name="mb"/> as mesh "Cos_{prefab}_{part}" and adds it as child <paramref name="part"/>.</summary>
        static void CosPart(GameObject root, string part, MeshBuilder mb, Material mat, bool shadows = true)
        {
            var mesh = SaveMesh("Cos_" + root.name + "_" + part, mb);
            MeshNode(part, root.transform, mesh, mat, Vector3.zero, Quaternion.identity, shadows);
        }

        /// <summary>Point on the bean's front (+Z) surface at (x, y), pushed out along the normal; rotation has +Z = normal.</summary>
        static void FrontFrame(float x, float y, float protrude, out Vector3 pos, out Quaternion rot)
        {
            float r = Mathf.Max(BodyRadius(y), 0.01f);
            float z = Mathf.Sqrt(Mathf.Max(r * r - x * x, 0.0001f));
            float dr = (BodyRadius(y + 0.01f) - BodyRadius(y - 0.01f)) / 0.02f;
            var n = new Vector3(x / r, -dr, z / r).normalized;
            pos = new Vector3(x, y, z) + n * protrude;
            rot = Quaternion.LookRotation(n, Vector3.up);
        }

        /// <summary>Quarter-ellipse dome profile (radius, height) from the rim up to the pole.</summary>
        static List<Vector2> DomeProfile(float radius, float baseY, float height, int steps = 8)
        {
            var p = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float a = (float)i / steps * Mathf.PI * 0.5f;
                p.Add(new Vector2(Mathf.Cos(a) * radius, baseY + Mathf.Sin(a) * height));
            }
            p[steps] = new Vector2(0f, baseY + height);
            return p;
        }

        // ------------------------------------------------------------------ hats (helmet hidden; head top at y≈0.86)

        static void BuildCap()
        {
            var root = new GameObject("Cap");
            var mb = MB();
            mb.Lathe(DomeProfile(0.305f, 0.66f, 0.25f), 24, true, capBottom: true);
            CosPart(root, "Crown", mb, mRed);

            var bill = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                float a = i / 10f * Mathf.PI;
                bill.Add(new Vector2(Mathf.Cos(a) * 0.25f, Mathf.Sin(a) * 0.24f));
            }
            mb = MB();
            mb.At(new Vector3(0f, 0.675f, 0.15f), Quaternion.Euler(98f, 0f, 0f));
            mb.ExtrudeXY(bill, 0.03f);
            mb.Identity();
            mb.Sphere(new Vector3(0f, 0.915f, 0f), 0.035f, 1, false);
            CosPart(root, "Bill", mb, mCream);
            SavePrefab(root, "Cosmetics/Hats/Cap");
        }

        static void BuildCrown()
        {
            var root = new GameObject("Crown");
            const float y0 = 0.77f, y1 = 0.87f;
            var mb = MB();
            mb.Lathe(Pts(0.195f, y0, 0.215f, y0, 0.235f, y1, 0.215f, y1, 0.195f, y0), 20, false);
            for (int k = 0; k < 5; k++)
            {
                float a = (90f + k * 72f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 0.222f, y1 - 0.005f, Mathf.Sin(a) * 0.222f);
                mb.At(p, Quaternion.Euler(0f, -k * 72f, 0f));
                mb.Cylinder(0.05f, 0f, 0.11f, 4, false);
            }
            CosPart(root, "Band", mb, mGold);

            mb = MB();
            for (int k = 0; k < 5; k++)
            {
                float a = (90f + k * 72f) * Mathf.Deg2Rad;
                mb.Sphere(new Vector3(Mathf.Cos(a) * 0.222f, y1 + 0.11f, Mathf.Sin(a) * 0.222f), 0.022f, 1, false);
            }
            CosPart(root, "Tips", mb, mCrystal);

            mb = MB();
            mb.Sphere(new Vector3(0f, 0.82f, 0.232f), new Vector3(0.03f, 0.034f, 0.018f), 1, true);
            CosPart(root, "Gem", mb, mRed);
            SavePrefab(root, "Cosmetics/Hats/Crown");
        }

        static void BuildFlowerCrown()
        {
            var root = new GameObject("FlowerCrown");
            var mb = MB();
            mb.At(new Vector3(0f, 0.75f, 0f), Quaternion.Euler(90f, 0f, 0f));
            mb.TorusXY(0.245f, 0.022f, 24, 6);
            CosPart(root, "Vine", mb, mLeaf);

            var petals = new[] { MB(), MB(), MB() };
            var centers = MB();
            for (int k = 0; k < 7; k++)
            {
                float a = (90f + k * 360f / 7f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var p = dir * 0.255f + Vector3.up * 0.765f;
                var n = (dir * 0.8f + Vector3.up * 0.6f).normalized;
                var rot = Quaternion.LookRotation(n, Vector3.up);
                var pm = petals[k % 3];
                for (int j = 0; j < 5; j++)
                {
                    float b = j * 72f * Mathf.Deg2Rad;
                    pm.At(p + rot * new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f) * 0.032f, rot);
                    pm.Sphere(Vector3.zero, new Vector3(0.026f, 0.026f, 0.011f), 1, false);
                }
                centers.Sphere(p + n * 0.008f, 0.018f, 1, false);
            }
            CosPart(root, "PetalsPink", petals[0], mBlush, false);
            CosPart(root, "PetalsCream", petals[1], mCream, false);
            CosPart(root, "PetalsLilac", petals[2], mLavender, false);
            CosPart(root, "Centers", centers, mStar, false);
            SavePrefab(root, "Cosmetics/Hats/FlowerCrown");
        }

        static void BuildCatEars()
        {
            var root = new GameObject("CatEars");
            var outer = MB();
            var inner = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                var basePos = new Vector3(s * 0.13f, 0.78f, 0.02f);
                var rot = Quaternion.Euler(-6f, 0f, -s * 20f);
                outer.At(basePos, rot);
                outer.ExtrudeXY(Pts(-0.1f, 0f, 0.1f, 0f, 0.035f, 0.15f, 0f, 0.165f, -0.035f, 0.15f), 0.07f);
                inner.At(basePos + rot * new Vector3(0f, 0f, 0.03f), rot);
                inner.ExtrudeXY(Pts(-0.06f, 0.02f, 0.06f, 0.02f, 0f, 0.125f), 0.02f);
            }
            CosPart(root, "BodyTintEars", outer, mWhite);   // follows the body color at runtime
            CosPart(root, "Inner", inner, mBlush, false);
            SavePrefab(root, "Cosmetics/Hats/CatEars");
        }

        static void BuildPartyHat()
        {
            var root = new GameObject("PartyHat");
            var basePos = new Vector3(0.02f, 0.79f, 0f);
            var rot = Quaternion.Euler(8f, 0f, -10f);
            const float h = 0.36f, r0 = 0.17f;
            var bands = new[] { MB(), MB() };
            for (int k = 0; k < 4; k++)
            {
                float ya = k * h / 4f, yb = (k + 1) * h / 4f;
                var m = bands[k % 2];
                m.At(basePos + rot * new Vector3(0f, ya, 0f), rot);
                m.Cylinder(r0 * (1f - ya / h), r0 * (1f - yb / h), h / 4f, 18, true, k == 0, false);
            }
            CosPart(root, "StripeA", bands[0], mCrystal);
            CosPart(root, "StripeB", bands[1], mStar);

            var trim = MB();
            trim.At(basePos, rot * Quaternion.Euler(90f, 0f, 0f));
            trim.TorusXY(r0, 0.02f, 18, 6);
            trim.Identity();
            trim.Sphere(basePos + rot * new Vector3(0f, h, 0f), 0.05f, 1, true);
            CosPart(root, "Pompom", trim, mCream);
            SavePrefab(root, "Cosmetics/Hats/PartyHat");
        }

        static void BuildChefHat()
        {
            var root = new GameObject("ChefHat");
            var mb = MB();
            mb.At(new Vector3(0f, 0.73f, 0f));
            mb.Cylinder(0.258f, 0.268f, 0.11f, 24, true, true, false);
            CosPart(root, "Band", mb, mCream);

            mb = MB();
            mb.Sphere(new Vector3(0f, 0.97f, 0f), new Vector3(0.25f, 0.16f, 0.25f), 2, false);
            for (int k = 0; k < 5; k++)
            {
                float a = (90f + k * 72f) * Mathf.Deg2Rad;
                mb.Sphere(new Vector3(Mathf.Cos(a) * 0.17f, 0.94f, Mathf.Sin(a) * 0.17f), 0.12f, 2, false);
            }
            mb.Sphere(new Vector3(0f, 1.06f, 0f), 0.13f, 2, false);
            CosPart(root, "Puff", mb, mWhite);
            SavePrefab(root, "Cosmetics/Hats/ChefHat");
        }

        static void BuildFrogHat()
        {
            var root = new GameObject("FrogHat");
            var hood = MB();
            hood.Lathe(DomeProfile(0.335f, 0.6f, 0.34f), 24, true, capBottom: true);
            var whites = MB();
            var pupils = MB();
            var blush = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                hood.Sphere(new Vector3(s * 0.13f, 0.9f, 0.08f), 0.085f, 2, false);
                whites.Sphere(new Vector3(s * 0.13f, 0.92f, 0.15f), new Vector3(0.055f, 0.055f, 0.03f), 2, false);
                pupils.Sphere(new Vector3(s * 0.125f, 0.915f, 0.178f), new Vector3(0.03f, 0.034f, 0.015f), 1, false);
                var p = new Vector3(s * 0.24f, 0.72f, 0.2f);
                blush.At(p, Quaternion.LookRotation(new Vector3(p.x, 0.25f, p.z), Vector3.up));
                blush.Sphere(Vector3.zero, new Vector3(0.05f, 0.028f, 0.012f), 1, false);
            }
            CosPart(root, "Hood", hood, mLeaf);
            CosPart(root, "EyeWhites", whites, mEyeWhite);
            CosPart(root, "Pupils", pupils, mInk, false);
            CosPart(root, "Blush", blush, mBlush, false);
            SavePrefab(root, "Cosmetics/Hats/FrogHat");
        }

        static void BuildBunnyEars()
        {
            var root = new GameObject("BunnyEars");
            var outer = MB();
            var inner = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                var basePos = new Vector3(s * 0.09f, 0.79f, 0f);
                var rot = Quaternion.Euler(-10f, 0f, -s * (s > 0 ? 22f : 10f)); // right ear flops a bit more
                outer.At(basePos, rot, new Vector3(1f, 1f, 0.55f));
                outer.Capsule(0.055f, 0.36f, 12, 4);
                inner.At(basePos + rot * new Vector3(0f, 0.06f, 0.028f), rot, new Vector3(1f, 1f, 0.3f));
                inner.Capsule(0.03f, 0.26f, 10, 3);
            }
            CosPart(root, "Ears", outer, mCream);
            CosPart(root, "Inner", inner, mBlush, false);
            SavePrefab(root, "Cosmetics/Hats/BunnyEars");
        }

        // ------------------------------------------------------------------ face (eyes at (±0.11, 0.47), mouth y 0.4)

        static void BuildRoundGlasses()
        {
            var root = new GameObject("RoundGlasses");
            var mb = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                FrontFrame(s * 0.11f, 0.47f, 0.045f, out var p, out var r);
                mb.At(p, r);
                mb.TorusXY(0.082f, 0.012f, 20, 6);
            }
            FrontFrame(0f, 0.462f, 0.05f, out var bp, out var br);
            mb.At(bp, br);
            mb.TorusXY(0.03f, 0.01f, 8, 6, 20f, 160f);
            CosPart(root, "Frame", mb, mInk, false);
            SavePrefab(root, "Cosmetics/Faces/RoundGlasses");
        }

        static void BuildSunglasses()
        {
            var root = new GameObject("Sunglasses");
            var lens = new List<Vector2>();
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                lens.Add(new Vector2(0.1f * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.6f), 0.085f * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.6f)));
            }
            var mb = MB();
            var shine = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                FrontFrame(s * 0.105f, 0.475f, 0.04f, out var p, out var r);
                mb.At(p, r);
                mb.ExtrudeXY(lens, 0.02f);
                shine.At(p + r * new Vector3(-0.03f, 0.03f, 0.011f), r * Quaternion.Euler(0f, 0f, -35f));
                shine.Box(Vector3.zero, new Vector3(0.05f, 0.012f, 0.004f));
            }
            FrontFrame(0f, 0.5f, 0.045f, out var bp, out var br);
            mb.At(bp, br);
            mb.Box(Vector3.zero, new Vector3(0.07f, 0.02f, 0.02f), 0.005f);
            CosPart(root, "Lenses", mb, mTire);
            CosPart(root, "Shine", shine, mShine, false);
            SavePrefab(root, "Cosmetics/Faces/Sunglasses");
        }

        static void BuildMustache()
        {
            var root = new GameObject("Mustache");
            FrontFrame(0f, 0.432f, 0.008f, out var p, out var r);
            var mb = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                mb.At(p + r * new Vector3(s * 0.045f, 0f, 0.01f), r * Quaternion.Euler(0f, 0f, s * 12f));
                mb.Sphere(Vector3.zero, new Vector3(0.055f, 0.026f, 0.02f), 2, false);
                mb.At(p + r * new Vector3(s * 0.098f, 0.018f, 0f), r);
                mb.Sphere(Vector3.zero, 0.022f, 1, false);
            }
            CosPart(root, "Mustache", mb, mTrunk);
            SavePrefab(root, "Cosmetics/Faces/Mustache");
        }

        static void BuildHeartStickers()
        {
            var root = new GameObject("HeartStickers");
            var heart = new List<Vector2>();
            for (int i = 0; i < 20; i++)
            {
                float t = i / 20f * Mathf.PI * 2f;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                heart.Add(new Vector2(x, y + 2f) * 0.0032f);
            }
            var mb = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                FrontFrame(s * 0.215f, 0.36f, 0.018f, out var p, out var r);
                mb.At(p, r * Quaternion.Euler(0f, 0f, -s * 10f));
                mb.ExtrudeXY(heart, 0.014f);
            }
            CosPart(root, "Hearts", mb, mCosHeart, false);

            mb = MB();
            FrontFrame(-0.2f, 0.565f, 0.008f, out var sp, out var sr);
            mb.At(sp, sr * Quaternion.Euler(0f, 0f, 12f));
            mb.ExtrudeXY(MeshBuilder.Star(5, 0.04f, 0.018f), 0.012f);
            FrontFrame(0.215f, 0.58f, 0.008f, out sp, out sr);
            mb.At(sp, sr * Quaternion.Euler(0f, 0f, -15f));
            mb.ExtrudeXY(MeshBuilder.Star(5, 0.027f, 0.012f), 0.012f);
            CosPart(root, "Stars", mb, mStar, false);
            SavePrefab(root, "Cosmetics/Faces/HeartStickers");
        }

        // ------------------------------------------------------------------ back (-Z side, body back z≈-0.37 at y 0.4)

        static void BuildJetpack()
        {
            var root = new GameObject("Jetpack");
            var tanks = MB();
            var noses = MB();
            var nozzles = MB();
            var flames = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                var c = new Vector3(s * 0.1f, 0f, -0.4f);
                tanks.At(c + Vector3.up * 0.24f);
                tanks.Cylinder(0.075f, 0.075f, 0.3f, 14, true, false, false);
                noses.At(c + Vector3.up * 0.54f);
                noses.Cylinder(0.075f, 0f, 0.13f, 14, true, false, false);
                nozzles.At(c + Vector3.up * 0.18f);
                nozzles.Cylinder(0.06f, 0.045f, 0.06f, 12, false);
                flames.At(c + Vector3.up * 0.18f, Quaternion.Euler(180f, 0f, 0f));
                flames.Cylinder(0.045f, 0f, 0.12f, 8, true, true, false);
            }
            noses.Identity();
            noses.Box(new Vector3(0f, 0.4f, -0.37f), new Vector3(0.16f, 0.2f, 0.06f), 0.015f);
            CosPart(root, "Tanks", tanks, mSilver);
            CosPart(root, "Noses", noses, mRed);
            CosPart(root, "Nozzles", nozzles, mMetal);
            CosPart(root, "Flames", flames, mBeak, false);
            SavePrefab(root, "Cosmetics/Backs/Jetpack");
        }

        static void BuildCape()
        {
            var root = new GameObject("Cape");
            const int rows = 6, cols = 10;
            const float top = 0.6f, bottom = 0.1f, thick = 0.02f;
            float waistY = BodyBottom + BodyWaist * BodyHeight;
            var outerPts = new Vector3[rows + 1, cols + 1];
            var innerPts = new Vector3[rows + 1, cols + 1];
            var dirs = new Vector3[rows + 1, cols + 1];
            for (int i = 0; i <= rows; i++)
            {
                float v = (float)i / rows;
                float y = Mathf.Lerp(top, bottom, v);
                float span = Mathf.Lerp(70f, 88f, v) * Mathf.Deg2Rad;
                float rad = (y < waistY ? BodyMaxRadius : BodyRadius(y)) + 0.03f + (top - y) * 0.08f;
                for (int j = 0; j <= cols; j++)
                {
                    float phi = Mathf.Lerp(-span, span, (float)j / cols);
                    var d = new Vector3(Mathf.Sin(phi), 0f, -Mathf.Cos(phi));
                    float yy = i == rows ? y + Mathf.Sin(j * Mathf.PI * 0.5f) * 0.02f : y; // wavy hem
                    dirs[i, j] = d;
                    outerPts[i, j] = d * rad + Vector3.up * yy;
                    innerPts[i, j] = d * (rad - thick) + Vector3.up * yy;
                }
            }

            var outer = MB();
            var lining = MB();
            for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
            {
                var d = (dirs[i, j] + dirs[i, j + 1]).normalized;
                outer.Quad(outerPts[i, j], outerPts[i, j + 1], outerPts[i + 1, j + 1], outerPts[i + 1, j], d);
                lining.Quad(innerPts[i, j], innerPts[i, j + 1], innerPts[i + 1, j + 1], innerPts[i + 1, j], -d);
            }
            for (int i = 0; i < rows; i++)
            {
                // side edges: outward along the tangent, away from the cape center
                var dl = dirs[i, 0];
                var dr = dirs[i, cols];
                outer.Quad(outerPts[i, 0], outerPts[i + 1, 0], innerPts[i + 1, 0], innerPts[i, 0], -new Vector3(-dl.z, 0f, dl.x));
                outer.Quad(outerPts[i, cols], outerPts[i + 1, cols], innerPts[i + 1, cols], innerPts[i, cols], new Vector3(-dr.z, 0f, dr.x));
            }
            for (int j = 0; j < cols; j++)
            {
                outer.Quad(outerPts[0, j], outerPts[0, j + 1], innerPts[0, j + 1], innerPts[0, j], Vector3.up);
                outer.Quad(outerPts[rows, j], outerPts[rows, j + 1], innerPts[rows, j + 1], innerPts[rows, j], Vector3.down);
            }
            CosPart(root, "Cape", outer, mRed);
            CosPart(root, "Lining", lining, mGoldCream);

            var collar = MB();
            collar.At(new Vector3(0f, top, 0f), Quaternion.Euler(90f, 0f, 0f));
            collar.TorusXY(BodyRadius(top) + 0.025f, 0.025f, 14, 6, 200f, 340f, true);
            CosPart(root, "Collar", collar, mGold);
            SavePrefab(root, "Cosmetics/Backs/Cape");
        }

        static void BuildBackpack()
        {
            var root = new GameObject("Backpack");
            var mb = MB();
            mb.Box(new Vector3(0f, 0.4f, -0.42f), new Vector3(0.38f, 0.36f, 0.2f), 0.05f);
            CosPart(root, "Bag", mb, mCopper);

            mb = MB();
            mb.Box(new Vector3(0f, 0.54f, -0.44f), new Vector3(0.36f, 0.08f, 0.22f), 0.03f);
            CosPart(root, "Flap", mb, mCopperDark);

            mb = MB();
            mb.Box(new Vector3(0f, 0.33f, -0.535f), new Vector3(0.26f, 0.14f, 0.06f), 0.02f);
            CosPart(root, "Pocket", mb, mCream);

            mb = MB();
            mb.Box(new Vector3(0f, 0.49f, -0.555f), new Vector3(0.06f, 0.06f, 0.02f), 0.006f);
            CosPart(root, "Buckle", mb, mGold);
            SavePrefab(root, "Cosmetics/Backs/Backpack");
        }

        static void BuildAngelWings()
        {
            var root = new GameObject("AngelWings");
            var wing = Pts(0f, 0.06f, 0.08f, 0.16f, 0.2f, 0.25f, 0.33f, 0.3f, 0.43f, 0.28f, 0.46f, 0.2f, 0.4f, 0.15f, 0.42f, 0.08f,
                0.35f, 0.04f, 0.36f, -0.03f, 0.27f, -0.04f, 0.25f, -0.11f, 0.16f, -0.09f, 0.12f, -0.15f, 0.05f, -0.08f, 0f, -0.05f);
            var main = MB();
            var layer = MB();
            for (int s = -1; s <= 1; s += 2)
            {
                var shape = new List<Vector2>(wing.Count);
                var small = new List<Vector2>(wing.Count);
                foreach (var p in wing)
                {
                    shape.Add(new Vector2(p.x * s, p.y));
                    small.Add(new Vector2(p.x * s * 0.66f, p.y * 0.62f + 0.02f));
                }
                var basePos = new Vector3(s * 0.06f, 0.42f, -0.35f);
                var rot = Quaternion.Euler(0f, s * 28f, s * 6f);
                main.At(basePos, rot);
                main.ExtrudeXY(shape, 0.035f);
                layer.At(basePos + rot * new Vector3(0f, 0f, -0.026f), rot);
                layer.ExtrudeXY(small, 0.02f);
            }
            CosPart(root, "Wings", main, mWhite);
            CosPart(root, "Feathers", layer, mCream);
            SavePrefab(root, "Cosmetics/Backs/AngelWings");
        }

        // ------------------------------------------------------------------ library asset

        static void BuildCosmeticLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<CosmeticLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<CosmeticLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            lib.hats = LoadCosmetics("Hats", HatFiles, CosmeticCatalog.Hats.Length);
            lib.faces = LoadCosmetics("Faces", FaceFiles, CosmeticCatalog.Faces.Length);
            lib.backs = LoadCosmetics("Backs", BackFiles, CosmeticCatalog.Backs.Length);
            EditorUtility.SetDirty(lib);
        }

        static GameObject[] LoadCosmetics(string folder, string[] files, int catalogCount)
        {
            if (files.Length != catalogCount)
                Debug.LogWarning($"[IRR] Cosmetics/{folder}: {files.Length} prefabs listed but the catalog has {catalogCount} options.");
            var result = new GameObject[catalogCount];
            for (int i = 0; i < catalogCount && i < files.Length; i++)
                if (files[i] != null)
                    result[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"{CosmeticsDir}/{folder}/{files[i]}.prefab");
            return result;
        }
    }
}
