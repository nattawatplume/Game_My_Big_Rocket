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

            Node("FireRoot", root.transform, new Vector3(0f, 1.6f, -0.06f));
            Node("ThrowPoint", root.transform, new Vector3(0f, 0.9f, 1.0f));

            SavePrefab(root, "Robot/Cpe40");
        }
    }
}
