using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    public static partial class ArtGenerator
    {
        /// <summary>Wheel-like cylinder along X, centered on the origin.</summary>
        static void CylinderX(MeshBuilder mb, Vector3 center, float radius, float width, int sides, bool smooth)
        {
            mb.At(center + new Vector3(width * 0.5f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f));
            mb.Cylinder(radius, radius, width, sides, smooth);
        }

        /// <summary>Box spanning from <paramref name="a"/> to <paramref name="b"/> with a square cross-section.</summary>
        static void Beam(MeshBuilder mb, Vector3 a, Vector3 b, float thickness)
        {
            Vector3 d = b - a;
            Vector3 up = Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            mb.At((a + b) * 0.5f, Quaternion.LookRotation(d.normalized, up));
            mb.Box(Vector3.zero, new Vector3(thickness, thickness, d.magnitude), thickness * 0.2f);
        }

        static void BuildRobot()
        {
            // --- meshes
            var mb = MB();
            CylinderX(mb, Vector3.zero, 0.34f, 0.24f, 12, false);
            var tire = SaveMesh("Robot_Tire", mb);

            mb = MB();
            CylinderX(mb, Vector3.zero, 0.15f, 0.28f, 8, false);
            var hub = SaveMesh("Robot_Hub", mb);

            mb = MB();
            mb.Box(new Vector3(0f, 0.42f, 0f), new Vector3(0.86f, 0.3f, 1.1f), 0.06f);
            mb.Box(new Vector3(0f, 0.36f, 0.56f), new Vector3(0.7f, 0.12f, 0.08f), 0.03f); // front bumper
            var chassis = SaveMesh("Robot_Base", mb);

            mb = MB();
            mb.Box(new Vector3(0f, 0.82f, -0.12f), new Vector3(0.82f, 0.5f, 0.72f), 0.07f);
            var cab = SaveMesh("Robot_Cab", mb);

            mb = MB();
            mb.Box(Vector3.zero, new Vector3(0.56f, 0.2f, 0.03f), 0.012f);
            var window = SaveMesh("Robot_Window", mb);

            mb = MB();
            mb.Box(Vector3.zero, new Vector3(0.66f, 0.48f, 0.52f), 0.08f);
            var head = SaveMesh("Robot_Head", mb);

            mb = MB();
            mb.Cylinder(0.016f, 0.016f, 0.1f, 6, false);
            mb.Sphere(new Vector3(0f, 0.12f, 0f), 0.038f, 1, true);
            var antenna = SaveMesh("Robot_Antenna", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.095f, 0.1f, 0.04f), 2, false);
            var eyeWhite = SaveMesh("Robot_EyeWhite", mb);

            mb = MB();
            mb.Sphere(Vector3.zero, new Vector3(0.045f, 0.05f, 0.025f), 2, false);
            var pupil = SaveMesh("Robot_Pupil", mb);

            mb = MB();
            mb.At(new Vector3(-0.15f, 0.1f, 0f), Quaternion.Euler(0f, 0f, -14f)).Box(Vector3.zero, new Vector3(0.15f, 0.03f, 0.03f), 0.008f);
            mb.At(new Vector3(0.15f, 0.1f, 0f), Quaternion.Euler(0f, 0f, 14f)).Box(Vector3.zero, new Vector3(0.15f, 0.03f, 0.03f), 0.008f);
            var brows = SaveMesh("Robot_Brows", mb);

            mb = MB();
            mb.Box(Vector3.zero, new Vector3(0.2f, 0.045f, 0.025f), 0.01f);
            var mouth = SaveMesh("Robot_Mouth", mb);

            mb = MB();
            Beam(mb, new Vector3(-0.36f, 0f, 0f), new Vector3(-0.36f, -0.24f, 0.36f), 0.07f);
            Beam(mb, new Vector3(0.36f, 0f, 0f), new Vector3(0.36f, -0.24f, 0.36f), 0.07f);
            mb.Identity();
            mb.Box(new Vector3(0f, -0.41f, 0.5f), new Vector3(0.95f, 0.05f, 0.38f), 0.015f);  // floor
            mb.Box(new Vector3(0f, -0.25f, 0.33f), new Vector3(0.95f, 0.36f, 0.05f), 0.015f); // back
            mb.Box(new Vector3(-0.45f, -0.31f, 0.5f), new Vector3(0.05f, 0.24f, 0.38f), 0.012f);
            mb.Box(new Vector3(0.45f, -0.31f, 0.5f), new Vector3(0.05f, 0.24f, 0.38f), 0.012f);
            for (int i = 0; i < 4; i++)
                mb.Box(new Vector3(-0.33f + i * 0.22f, -0.41f, 0.71f), new Vector3(0.08f, 0.04f, 0.08f), 0.01f); // teeth
            var bucket = SaveMesh("Robot_Bucket", mb);

            mb = MB();
            mb.Cylinder(0.05f, 0.05f, 0.32f, 8, false);
            mb.At(new Vector3(0f, 0.32f, 0f)).Cylinder(0.065f, 0.065f, 0.04f, 8, false);
            var exhaust = SaveMesh("Robot_Exhaust", mb);

            // --- hierarchy
            var root = new GameObject("Cpe40");
            var visual = Node("Visual", root.transform);
            var vt = visual.transform;

            var body = MeshNode("Body", vt, cab, mRobotBlue, Vector3.zero);
            MeshNode("Window", body.transform, window, mSilverBlue, new Vector3(0f, 0.88f, 0.245f));
            MeshNode("Base", vt, chassis, mRobotYellow, Vector3.zero);
            for (int side = -1; side <= 1; side += 2)
            {
                var wheel = MeshNode(side < 0 ? "WheelL" : "WheelR", vt, tire, mTire, new Vector3(side * 0.56f, 0.34f, -0.02f));
                MeshNode("Hub", wheel.transform, hub, mRobotYellow, Vector3.zero);
            }

            var headGo = MeshNode("Head", vt, head, mCream, new Vector3(0f, 1.31f, -0.06f));
            MeshNode("Antenna", headGo.transform, antenna, mRobotYellow, new Vector3(0.2f, 0.24f, -0.05f));

            // Cross-eyed: pupils pushed toward the nose.
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = MeshNode(side < 0 ? "EyeL" : "EyeR", vt, eyeWhite, mEyeWhite, new Vector3(side * 0.14f, 1.36f, 0.2f));
                MeshNode("Pupil", eye.transform, pupil, mInk, new Vector3(-side * 0.048f, -0.01f, 0.026f));
            }
            MeshNode("Brows", vt, brows, mInk, new Vector3(0f, 1.355f, 0.23f));
            MeshNode("Mouth", vt, mouth, mInk, new Vector3(0f, 1.2f, 0.205f));

            MeshNode("Bucket", vt, bucket, mRobotYellow, new Vector3(0f, 0.5f, 0.5f));
            MeshNode("Exhaust", vt, exhaust, mMetal, new Vector3(0.28f, 1.07f, -0.42f));

            BuildRobotForms(vt);

            Node("FireRoot", root.transform, new Vector3(0f, 1.6f, -0.06f));
            Node("ThrowPoint", root.transform, new Vector3(0f, 0.9f, 1.0f));

            SavePrefab(root, "Robot/Cpe40");
        }

        /// <summary>Add-on parts for each rage form. Names start with FormN; RobotBoss shows them from that rage level up.</summary>
        static void BuildRobotForms(Transform vt)
        {
            // --- Form 1: alarm siren + shoulder armor
            var mb = MB();
            mb.Cylinder(0.11f, 0.09f, 0.06f, 10, false);
            var sirenBase = SaveMesh("Robot_F1_SirenBase", mb);
            mb = MB();
            mb.Sphere(new Vector3(0f, 0.06f, 0f), new Vector3(0.085f, 0.11f, 0.085f), 2, false);
            var sirenDome = SaveMesh("Robot_F1_SirenDome", mb);
            mb = MB();
            for (int side = -1; side <= 1; side += 2)
                mb.Box(new Vector3(side * 0.5f, 1.04f, -0.12f), new Vector3(0.24f, 0.16f, 0.66f), 0.05f);
            var armor = SaveMesh("Robot_F1_Armor", mb);
            MeshNode("Form1_SirenBase", vt, sirenBase, mMetal, new Vector3(-0.08f, 1.55f, -0.06f));
            MeshNode("Form1_Siren", vt, sirenDome, mRed, new Vector3(-0.08f, 1.61f, -0.06f));
            MeshNode("Form1_Armor", vt, armor, mRobotYellow, Vector3.zero);

            // --- Form 2: fist arms + bucket spikes
            mb = MB();
            for (int side = -1; side <= 1; side += 2)
            {
                Beam(mb, new Vector3(side * 0.62f, 1f, -0.1f), new Vector3(side * 0.82f, 0.72f, 0.1f), 0.13f);
                Beam(mb, new Vector3(side * 0.82f, 0.72f, 0.1f), new Vector3(side * 0.82f, 0.62f, 0.42f), 0.12f);
            }
            var arms = SaveMesh("Robot_F2_Arms", mb);
            mb = MB();
            for (int side = -1; side <= 1; side += 2)
                mb.Sphere(new Vector3(side * 0.82f, 0.62f, 0.52f), 0.2f, 2, false);
            var fists = SaveMesh("Robot_F2_Fists", mb);
            mb = MB();
            for (int i = 0; i < 4; i++)
            {
                mb.At(new Vector3(-0.33f + i * 0.22f, -0.41f, 0.74f), Quaternion.Euler(90f, 0f, 0f));
                mb.Cylinder(0.065f, 0.006f, 0.26f, 6, false);
            }
            var spikes = SaveMesh("Robot_F2_Spikes", mb);
            MeshNode("Form2_Arms", vt, arms, mMetal, Vector3.zero);
            MeshNode("Form2_Fists", vt, fists, mRed, Vector3.zero);
            MeshNode("Form2_Spikes", vt.Find("Bucket"), spikes, mMetal, Vector3.zero);

            // --- Form 3: horns, back jets, back spikes, teeth
            mb = MB();
            for (int side = -1; side <= 1; side += 2)
            {
                mb.At(new Vector3(side * 0.26f, 1.52f, -0.06f), Quaternion.Euler(0f, 0f, -side * 28f));
                mb.Cylinder(0.08f, 0.008f, 0.42f, 8, false);
            }
            var horns = SaveMesh("Robot_F3_Horns", mb);
            mb = MB();
            for (int side = -1; side <= 1; side += 2)
            {
                mb.At(new Vector3(side * 0.3f, 0.62f, -0.62f));
                mb.Cylinder(0.17f, 0.12f, 0.62f, 10, false);
            }
            var jets = SaveMesh("Robot_F3_Jets", mb);
            mb = MB();
            for (int side = -1; side <= 1; side += 2)
            {
                mb.At(new Vector3(side * 0.3f, 0.55f, -0.62f));
                mb.Cylinder(0.12f, 0.17f, 0.07f, 10, false);
            }
            var jetTips = SaveMesh("Robot_F3_JetTips", mb);
            mb = MB();
            for (int i = 0; i < 3; i++)
            {
                mb.At(new Vector3(0f, 1.05f - i * 0.2f, -0.48f - i * 0.02f), Quaternion.Euler(-65f, 0f, 0f));
                mb.Cylinder(0.075f, 0.006f, 0.3f, 6, false);
            }
            var backSpikes = SaveMesh("Robot_F3_BackSpikes", mb);
            mb = MB();
            for (int i = 0; i < 4; i++)
                mb.At(new Vector3(-0.09f + i * 0.06f, 1.165f, 0.215f), Quaternion.Euler(0f, 0f, 45f)).Box(Vector3.zero, new Vector3(0.05f, 0.05f, 0.02f));
            var teeth = SaveMesh("Robot_F3_Teeth", mb);
            MeshNode("Form3_Horns", vt, horns, mInk, Vector3.zero);
            MeshNode("Form3_Jets", vt, jets, mMetal, Vector3.zero);
            MeshNode("Form3_JetTips", vt, jetTips, mRed, Vector3.zero);
            MeshNode("Form3_BackSpikes", vt, backSpikes, mRed, Vector3.zero);
            MeshNode("Form3_Teeth", vt, teeth, mEyeWhite, Vector3.zero);
        }
    }
}
