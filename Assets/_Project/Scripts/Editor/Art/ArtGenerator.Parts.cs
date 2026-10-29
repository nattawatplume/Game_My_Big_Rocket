using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    public static partial class ArtGenerator
    {
        /// <summary>Collectible parts: white (tinted at runtime with the tier color), standing up, facing +Z, ~0.45 wide.</summary>
        static void BuildParts()
        {
            // Gear: 8 teeth with a round hole
            const int teeth = 8;
            const float tipR = 0.225f, rootR = 0.17f, holeR = 0.07f;
            var outer = new List<Vector2>();
            var inner = new List<Vector2>();
            float step = 360f / teeth;
            for (int t = 0; t < teeth; t++)
            {
                float b = t * step;
                float[] angles = { b - 0.28f * step, b - 0.13f * step, b + 0.13f * step, b + 0.28f * step };
                float[] radii = { rootR, tipR, tipR, rootR };
                for (int k = 0; k < 4; k++)
                {
                    float a = angles[k] * Mathf.Deg2Rad;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    outer.Add(d * radii[k]);
                    inner.Add(d * holeR);
                }
            }
            var mb = MB();
            mb.At(new Vector3(0f, tipR, 0f));
            mb.ExtrudeRingXY(outer, inner, 0.12f);
            BuildPartPrefab("Gear", SaveMesh("Part_Gear", mb));

            // Hex nut: hexagon (edges split so it pairs with a 12-gon hole)
            const float hexR = 0.2f;
            outer.Clear();
            inner.Clear();
            var hex = MeshBuilder.RegularPolygon(6, hexR, 0f);
            for (int i = 0; i < 6; i++)
            {
                outer.Add(hex[i]);
                outer.Add((hex[i] + hex[(i + 1) % 6]) * 0.5f);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                inner.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.085f);
            }
            mb = MB();
            mb.At(new Vector3(0f, hexR * Mathf.Sin(60f * Mathf.Deg2Rad), 0f));
            mb.ExtrudeRingXY(outer, inner, 0.13f);
            BuildPartPrefab("Nut", SaveMesh("Part_Nut", mb));

            // Fuel canister: rounded jerry can with handle, cap and an embossed X
            mb = MB();
            mb.Box(new Vector3(0f, 0.18f, 0f), new Vector3(0.3f, 0.36f, 0.18f), 0.04f);
            Beam(mb, new Vector3(-0.1f, 0.34f, 0f), new Vector3(-0.1f, 0.44f, 0f), 0.035f);
            Beam(mb, new Vector3(0.0f, 0.34f, 0f), new Vector3(0.0f, 0.44f, 0f), 0.035f);
            Beam(mb, new Vector3(-0.12f, 0.44f, 0f), new Vector3(0.02f, 0.44f, 0f), 0.035f);
            Beam(mb, new Vector3(-0.08f, 0.08f, 0.09f), new Vector3(0.08f, 0.28f, 0.09f), 0.025f);
            Beam(mb, new Vector3(0.08f, 0.08f, 0.09f), new Vector3(-0.08f, 0.28f, 0.09f), 0.025f);
            mb.At(new Vector3(0.09f, 0.34f, 0f)).Cylinder(0.04f, 0.04f, 0.07f, 8, false);
            BuildPartPrefab("Fuel", SaveMesh("Part_Fuel", mb));

            // Chip: square body, pins on all four sides, raised die on the front
            mb = MB();
            const float cy = 0.21f;
            mb.Box(new Vector3(0f, cy, 0f), new Vector3(0.32f, 0.32f, 0.07f), 0.015f);
            mb.Box(new Vector3(0f, cy, 0.04f), new Vector3(0.13f, 0.13f, 0.025f), 0.01f);
            for (int k = -1; k <= 1; k++)
            {
                float o = k * 0.09f;
                mb.Box(new Vector3(-0.185f, cy + o, 0f), new Vector3(0.05f, 0.04f, 0.025f), 0.005f);
                mb.Box(new Vector3(0.185f, cy + o, 0f), new Vector3(0.05f, 0.04f, 0.025f), 0.005f);
                mb.Box(new Vector3(o, cy + 0.185f, 0f), new Vector3(0.04f, 0.05f, 0.025f), 0.005f);
                mb.Box(new Vector3(o, cy - 0.185f, 0f), new Vector3(0.04f, 0.05f, 0.025f), 0.005f);
            }
            BuildPartPrefab("Chip", SaveMesh("Part_Chip", mb));

            // Gem: faceted brilliant, point down
            mb = MB();
            mb.Lathe(Pts(0f, 0f, 0.2f, 0.24f, 0.12f, 0.36f, 0f, 0.36f), 8, false);
            BuildPartPrefab("Gem", SaveMesh("Part_Gem", mb));
        }

        static void BuildPartPrefab(string name, Mesh mesh)
        {
            var root = new GameObject(name);
            var visual = Node("Visual", root.transform);
            AddMesh(visual, mesh, mWhite);
            var bob = visual.AddComponent<Bobbing>();
            bob.amplitude = 0.05f;
            bob.frequency = 1.6f;
            bob.spinSpeed = 90f;
            bob.heightOffset = 0.06f;
            root.AddComponent<TintRenderer>();
            SavePrefab(root, "Parts/" + name);
        }
    }
}
