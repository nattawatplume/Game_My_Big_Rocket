using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>
    /// One-time project setup: URP asset, linear color, new Input System, GameConfig and rocket recipes.
    /// Safe to re-run. Batchmode: -executeMethod IslandRocketRumble.EditorTools.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string SettingsDir = Root + "/Settings";
        const string SoDir = Root + "/ScriptableObjects";

        [MenuItem("IRR/Setup/Run Project Setup")]
        public static void Run()
        {
            foreach (var dir in new[] { SettingsDir, SoDir, SoDir + "/Rockets", Root + "/Scenes", Root + "/Prefabs", Root + "/Art/Models", Root + "/Art/Materials", Root + "/Art/Shaders", Root + "/Audio" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();

            SetupRendering();
            SetupPlayerSettings();
            CreateConfig();
            CreateRecipes();

            AssetDatabase.SaveAssets();
            Debug.Log("[IRR] Project setup complete.");
        }

        static void SetupRendering()
        {
            string rendererPath = SettingsDir + "/IRR_Renderer.asset";
            string pipelinePath = SettingsDir + "/IRR_URP.asset";

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = true;
            pipeline.shadowDistance = 40f;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
        }

        static void SetupPlayerSettings()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.companyName = "nattawatplume";
            PlayerSettings.productName = "Island Rocket Rumble";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            // activeInputHandler: 0 = old, 1 = new Input System, 2 = both. No public API, so set it on the asset.
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings.Length > 0)
            {
                var so = new SerializedObject(playerSettings[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null)
                {
                    prop.intValue = 1;
                    so.ApplyModifiedProperties();
                }
            }
        }

        static void CreateConfig()
        {
            string path = SoDir + "/GameConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(path) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfig>(), path);
        }

        static void CreateRecipes()
        {
            Recipe("T1_Firecracker", PartTier.Scrap, "Firecracker", 30, 0.6f,
                C(PartType.Gear, PartTier.Scrap, 1), C(PartType.Fuel, PartTier.Scrap, 1));
            Recipe("T2_CopperDart", PartTier.Copper, "Copper Dart", 60, 0.8f,
                C(PartType.Gear, PartTier.Copper, 1), C(PartType.Nut, PartTier.Copper, 1), C(PartType.Fuel, PartTier.Copper, 1));
            Recipe("T3_SilverSpear", PartTier.Silver, "Silver Spear", 110, 1.0f,
                C(PartType.Gear, PartTier.Silver, 2), C(PartType.Fuel, PartTier.Silver, 1), C(PartType.Chip, PartTier.Silver, 1));
            Recipe("T4_GoldenGiant", PartTier.Gold, "Golden Giant", 190, 1.4f,
                C(PartType.Gear, PartTier.Gold, 2), C(PartType.Nut, PartTier.Gold, 1), C(PartType.Fuel, PartTier.Gold, 1), C(PartType.Chip, PartTier.Gold, 1));
            Recipe("T5_MegaOverkill", PartTier.Crystal, "Mega Overkill", 350, 2.2f,
                C(PartType.Gear, PartTier.Crystal, 2), C(PartType.Fuel, PartTier.Crystal, 1), C(PartType.Chip, PartTier.Crystal, 1), C(PartType.Gem, PartTier.Crystal, 1));
        }

        static PartCost C(PartType type, PartTier tier, int count) => new PartCost { type = type, tier = tier, count = count };

        static void Recipe(string file, PartTier tier, string name, int damage, float impactScale, params PartCost[] costs)
        {
            string path = $"{SoDir}/Rockets/{file}.asset";
            var r = AssetDatabase.LoadAssetAtPath<RocketRecipe>(path);
            if (r == null)
            {
                r = ScriptableObject.CreateInstance<RocketRecipe>();
                AssetDatabase.CreateAsset(r, path);
            }
            r.tier = tier;
            r.displayName = name;
            r.damage = damage;
            r.impactScale = impactScale;
            r.costs = costs;
            EditorUtility.SetDirty(r);
        }
    }
}
