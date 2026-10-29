using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    public static partial class ArtGenerator
    {
        // Bean body: superellipse halves around a "waist" (widest point).
        const float BodyBottom = 0.04f;
        const float BodyHeight = 0.82f;
        const float BodyMaxRadius = 0.37f;
        const float BodyWaist = 0.42f;
        const float HelmetY = 0.63f;
        const float HelmetRadius = 0.355f;

        static Mesh charBody, charHelmet, charEyeWhite, charPupil, charShine, charMouth, charBlush, charFoot, charBandAid, charBandPad, charStar;

        static float BodyRadius(float y)
        {
            float t = Mathf.Clamp01((y - BodyBottom) / BodyHeight);
            float u, p;
            if (t < BodyWaist)
            {
                u = (BodyWaist - t) / BodyWaist;
                p = 2.4f;
            }
            else
            {
                u = (t - BodyWaist) / (1f - BodyWaist);
                p = 2.0f;
            }
            float v = Mathf.Max(1f - Mathf.Pow(u, p), 0f);
            return BodyMaxRadius * Mathf.Pow(v, 1f / p);
        }

        /// <summary>Puts <paramref name="t"/> on the bean surface at (x, y) on the +Z side, local +Z along the surface normal.</summary>
        static void PlaceOnBody(Transform t, float x, float y, float protrude)
        {
            float r = Mathf.Max(BodyRadius(y), 0.01f);
            float z = Mathf.Sqrt(Mathf.Max(r * r - x * x, 0.0001f));
            float dr = (BodyRadius(y + 0.01f) - BodyRadius(y - 0.01f)) / 0.02f;
            var n = new Vector3(x / r, -dr, z / r).normalized;
            t.localPosition = new Vector3(x, y, z) + n * protrude;
            t.localRotation = Quaternion.LookRotation(n, Vector3.up);
        }

        static void BuildCharacterMeshes()
        {
            // Body
            var profile = new List<Vector2>();
            const int steps = 22;
            for (int i = 0; i <= steps; i++)
            {
                float tt = 0.5f - 0.5f * Mathf.Cos((float)i / steps * Mathf.PI); // denser near the poles
                float y = BodyBottom + tt * BodyHeight;
                profile.Add(new Vector2(BodyRadius(y), y));
            }
            profile[0] = new Vector2(0f, BodyBottom);
            profile[steps] = new Vector2(0f, BodyBottom + BodyHeight);
            var mb = MB();
            mb.Lathe(profile, 28, true);
            charBody = SaveMesh("Char_Body", mb);

            // Helmet: dome + skirt, brim (longer at the front), ridge on top
            var dome = new List<Vector2> { new Vector2(HelmetRadius, -0.04f) };
            for (int i = 0; i <= 9; i++)
            {
                float a = (float)i / 9 * Mathf.PI * 0.5f;
                dome.Add(new Vector2(Mathf.Cos(a) * HelmetRadius, Mathf.Sin(a) * HelmetRadius));
            }
            dome[dome.Count - 1] = new Vector2(0f, HelmetRadius);
            mb = MB();
            mb.Lathe(dome, 28, true, capBottom: true);
            mb.At(new Vector3(0f, -0.05f, 0.035f), Quaternion.identity, new Vector3(1f, 1f, 1.12f));
            mb.Cylinder(0.4f, 0.392f, 0.035f, 28, true);
            mb.At(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            mb.TorusXY(HelmetRadius - 0.004f, 0.034f, 16, 8, 22f, 158f, true);
            charHelmet = SaveMesh("Char_Helmet", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.085f, 0.1f, 0.045f), 2, false);
            charEyeWhite = SaveMesh("Char_EyeWhite", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.05f, 0.062f, 0.03f), 2, false);
            charPupil = SaveMesh("Char_Pupil", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, 0.018f, 1, false);
            charShine = SaveMesh("Char_EyeShine", mb);

            mb = MB();
            mb.TorusXY(0.045f, 0.011f, 12, 6, 200f, 340f, true);
            charMouth = SaveMesh("Char_Mouth", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.06f, 0.032f, 0.012f), 2, false);
            charBlush = SaveMesh("Char_Blush", mb);

            mb = MB();
            mb.Sphere(new Vector3(0f, 0.06f, 0f), new Vector3(0.1f, 0.06f, 0.13f), 2, false);
            charFoot = SaveMesh("Char_Foot", mb);

            mb = MB();
            mb.Box(Vector3.zero, new Vector3(0.1f, 0.04f, 0.016f), 0.006f);
            charBandAid = SaveMesh("Char_BandAid", mb);

            mb = MB();
            mb.Box(new Vector3(0f, 0f, 0.006f), new Vector3(0.032f, 0.028f, 0.012f), 0.003f);
            charBandPad = SaveMesh("Char_BandAidPad", mb);

            mb = MB();
            mb.ExtrudeXY(MeshBuilder.Star(5, 0.065f, 0.028f), 0.016f);
            charStar = SaveMesh("Char_StarSticker", mb);
        }

        static void BuildCharacters()
        {
            BuildCharacterMeshes();
            BuildCharacter("Pip", mPip, mHelmetYellow, true);
            BuildCharacter("Momo", mMomo, mHelmetOrange, false);
        }

        static void BuildCharacter(string name, Material bodyMat, Material helmetMat, bool isPip)
        {
            var root = new GameObject(name);
            var visual = Node("Visual", root.transform);
            var vt = visual.transform;

            MeshNode("Body", vt, charBody, bodyMat, Vector3.zero);
            MeshNode("FootL", vt, charFoot, bodyMat, new Vector3(-0.15f, 0f, 0.1f), Quaternion.Euler(0f, -12f, 0f));
            MeshNode("FootR", vt, charFoot, bodyMat, new Vector3(0.15f, 0f, 0.1f), Quaternion.Euler(0f, 12f, 0f));

            var helmet = MeshNode("Helmet", vt, charHelmet, helmetMat, new Vector3(0f, HelmetY, 0f));

            // Eyes (sclera + pupil + highlight), pupils nudged slightly toward the nose for cuteness
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = Node(side < 0 ? "EyeL" : "EyeR", vt);
                AddMesh(eye, charEyeWhite, mEyeWhite);
                PlaceOnBody(eye.transform, side * 0.11f, 0.47f, -0.015f);
                var pupil = MeshNode("Pupil", eye.transform, charPupil, mInk, new Vector3(-side * 0.008f, -0.008f, 0.028f));
                MeshNode("Shine", pupil.transform, charShine, mShine, new Vector3(0.016f, 0.022f, 0.03f), Quaternion.identity, false);
            }

            var mouth = Node("Mouth", vt);
            AddMesh(mouth, charMouth, mInk, false);
            PlaceOnBody(mouth.transform, 0f, 0.4f, -0.004f);

            // Cheeks: blush on both sides, Pip trades one blush for a band-aid.
            var blushL = Node("BlushL", vt);
            AddMesh(blushL, charBlush, mBlush, false);
            PlaceOnBody(blushL.transform, -0.215f, 0.36f, -0.004f);
            if (isPip)
            {
                var band = Node("BandAid", vt);
                AddMesh(band, charBandAid, mBandAid, false);
                PlaceOnBody(band.transform, 0.225f, 0.38f, 0.002f);
                band.transform.localRotation *= Quaternion.Euler(0f, 0f, 28f);
                MeshNode("Pad", band.transform, charBandPad, mCream, Vector3.zero, Quaternion.identity, false);
            }
            else
            {
                var blushR = Node("BlushR", vt);
                AddMesh(blushR, charBlush, mBlush, false);
                PlaceOnBody(blushR.transform, 0.215f, 0.36f, -0.004f);

                // Star sticker on the helmet, front-left
                var dir = new Vector3(-0.45f, 0.55f, 0.7f).normalized;
                MeshNode("StarSticker", helmet.transform, charStar, mStar, dir * (HelmetRadius + 0.002f),
                    Quaternion.LookRotation(dir, Vector3.up), false);
            }

            Node("HeadAnchor", root.transform, new Vector3(0f, 1.03f, 0f));

            var squash = root.AddComponent<SquashStretch>();
            squash.visual = vt;

            SavePrefab(root, "Characters/" + name);
        }
    }
}
