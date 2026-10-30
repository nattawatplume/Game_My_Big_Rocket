using UnityEditor;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    public static partial class ArtGenerator
    {
        /// <summary>Floating icons for the three island power-ups. Pivot sits on the ground; the icon hovers above it.</summary>
        static void BuildPowerUps()
        {
            if (!AssetDatabase.IsValidFolder(PrefabsDir + "/PowerUps"))
                AssetDatabase.CreateFolder(PrefabsDir, "PowerUps");

            var height = new Vector3(0f, 0.75f, 0f);

            // Shield: pointed badge with a cream center
            var outline = new[]
            {
                new Vector2(-0.3f, 0.3f), new Vector2(0f, 0.36f), new Vector2(0.3f, 0.3f),
                new Vector2(0.28f, -0.04f), new Vector2(0f, -0.38f), new Vector2(-0.28f, -0.04f),
            };
            var mb = MB();
            mb.ExtrudeXY(outline, 0.1f);
            var shield = SaveMesh("PowerUp_Shield", mb);
            mb = MB();
            mb.At(new Vector3(0f, 0f, 0.055f));
            mb.ExtrudeXY(MeshBuilder.Star(5, 0.17f, 0.075f), 0.02f);
            var shieldStar = SaveMesh("PowerUp_ShieldStar", mb);
            var root = new GameObject("Shield");
            var go = MeshNode("Icon", root.transform, shield, mPip, height);
            MeshNode("Star", go.transform, shieldStar, mStar, Vector3.zero);
            MeshNode("StarBack", go.transform, shieldStar, mStar, Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            SavePrefab(root, "PowerUps/Shield");

            // Speed: lightning bolt
            var bolt = new[]
            {
                new Vector2(0.08f, 0.4f), new Vector2(-0.22f, -0.04f), new Vector2(-0.03f, -0.04f),
                new Vector2(-0.1f, -0.4f), new Vector2(0.24f, 0.08f), new Vector2(0.04f, 0.08f),
            };
            mb = MB();
            mb.ExtrudeXY(bolt, 0.12f);
            var speed = SaveMesh("PowerUp_Speed", mb);
            root = new GameObject("Speed");
            MeshNode("Icon", root.transform, speed, mHelmetYellow, height);
            SavePrefab(root, "PowerUps/Speed");

            // Magnet: red U with metal tips
            mb = MB();
            mb.Box(new Vector3(0f, -0.2f, 0f), new Vector3(0.5f, 0.15f, 0.15f), 0.03f);
            for (int side = -1; side <= 1; side += 2)
                mb.Box(new Vector3(side * 0.18f, 0.02f, 0f), new Vector3(0.15f, 0.34f, 0.15f), 0.03f);
            var magnet = SaveMesh("PowerUp_Magnet", mb);
            mb = MB();
            for (int side = -1; side <= 1; side += 2)
                mb.Box(new Vector3(side * 0.18f, 0.26f, 0f), new Vector3(0.15f, 0.14f, 0.15f), 0.02f);
            var tips = SaveMesh("PowerUp_MagnetTips", mb);
            root = new GameObject("Magnet");
            go = MeshNode("Icon", root.transform, magnet, mRed, height);
            MeshNode("Tips", go.transform, tips, mSilver, Vector3.zero);
            SavePrefab(root, "PowerUps/Magnet");

            // Heart: extruded outline
            var heart = new Vector2[20];
            for (int i = 0; i < heart.Length; i++)
            {
                float a = i / (float)heart.Length * Mathf.PI * 2f;
                float s = Mathf.Sin(a);
                float x = 16f * s * s * s;
                float y = 13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a) - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a);
                heart[i] = new Vector2(x, y) * 0.024f;
            }
            mb = MB();
            mb.ExtrudeXY(heart, 0.14f);
            var heartMesh = SaveMesh("PowerUp_Heart", mb);
            root = new GameObject("Heart");
            MeshNode("Icon", root.transform, heartMesh, mRed, height);
            SavePrefab(root, "PowerUps/Heart");
        }
    }
}
