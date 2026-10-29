using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>
    /// Builds Scenes/Main.unity from the generated art prefabs and wires every module together.
    /// Re-runnable. Batchmode: -executeMethod IslandRocketRumble.EditorTools.SceneBuilder.Build
    /// </summary>
    public static class SceneBuilder
    {
        const string Root = "Assets/_Project";
        const string Prefabs = Root + "/Prefabs";
        const string ScenePath = Root + "/Scenes/Main.unity";

        static readonly Vector3 P1IslandPos = new Vector3(-7f, 0f, -2f);
        static readonly Vector3 P2IslandPos = new Vector3(7f, 0f, -2f);
        static readonly Vector3 RobotIslandPos = new Vector3(0f, 0.8f, 5f);

        // The models are authored at ~1m; at this camera distance they read better a bit larger.
        const float CharacterScale = 1.7f;
        const float RobotScale = 1.5f;

        static readonly string[] RocketFiles = { "T1_Firecracker", "T2_CopperDart", "T3_SilverSpear", "T4_GoldenGiant", "T5_MegaOverkill" };
        static readonly string[] PartFiles = { "Gear", "Nut", "Fuel", "Chip", "Gem" };

        /// <summary>Regenerates all art, then rebuilds the scene.</summary>
        [MenuItem("IRR/Generate Art + Build Main Scene")]
        public static void BuildEverything()
        {
            ArtGenerator.GenerateAll();
            Build();
        }

        [MenuItem("IRR/Build Main Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/ScriptableObjects/GameConfig.asset");
            var recipes = LoadRecipes();

            BuildLighting();
            BuildCamera();

            var context = new GameObject("MatchContext").AddComponent<MatchContext>();
            context.config = config;
            context.recipes = recipes;

            Spawn("Environment/Sea", "Sea", new Vector3(0f, -2.6f, 0f));
            BuildDecor();

            var p1Island = BuildPlayerIsland(PlayerId.P1, P1IslandPos);
            var p2Island = BuildPlayerIsland(PlayerId.P2, P2IslandPos);
            var p1 = BuildPlayer(PlayerId.P1, "Characters/Pip", p1Island);
            var p2 = BuildPlayer(PlayerId.P2, "Characters/Momo", p2Island);
            BuildRobot();

            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            manager.context = context;
            manager.islands = new[] { p1Island.GetComponent<IslandHealth>(), p2Island.GetComponent<IslandHealth>() };
            manager.players = new[] { p1, p2 };
            manager.partSpawners = new[] { p1Island.GetComponent<PartSpawner>(), p2Island.GetComponent<PartSpawner>() };

            AddByName(new GameObject("UI"), "IslandRocketRumble.UIRoot");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[IRR] Main scene built: " + ScenePath);
        }

        static RocketRecipe[] LoadRecipes()
        {
            var recipes = new RocketRecipe[RocketFiles.Length];
            for (int i = 0; i < RocketFiles.Length; i++)
            {
                recipes[i] = AssetDatabase.LoadAssetAtPath<RocketRecipe>($"{Root}/ScriptableObjects/Rockets/{RocketFiles[i]}.asset");
                if (recipes[i] == null) continue;
                var prefab = LoadPrefab("Rockets/" + RocketFiles[i]);
                if (prefab != null && recipes[i].flightPrefab != prefab)
                {
                    recipes[i].flightPrefab = prefab;
                    EditorUtility.SetDirty(recipes[i]);
                }
            }
            return recipes;
        }

        static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Palette.Hex("FFF1DC");
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("E9F6F1");
            RenderSettings.ambientEquatorColor = Palette.Hex("CDEBE1");
            RenderSettings.ambientGroundColor = Palette.Hex("E8C7BC");
            RenderSettings.fog = false;
        }

        static void BuildCamera()
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 8.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sea;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            var rot = Quaternion.Euler(38f, 0f, 0f);
            // Focus sits behind the player islands so the robot (which grows when angry) clears the HUD.
            var focus = new Vector3(0f, 0.6f, 1.8f);
            cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * 40f, rot);
            cam.gameObject.AddComponent<AudioListener>();
        }

        static GameObject BuildPlayerIsland(PlayerId owner, Vector3 pos)
        {
            var island = Spawn("Islands/PlayerIsland", owner + "_Island", pos) ?? new GameObject(owner + "_Island");
            island.transform.position = pos;

            var health = island.AddComponent<IslandHealth>();
            health.owner = owner;
            health.craterPrefab = LoadPrefab("FX/Crater");

            var spawner = island.AddComponent<PartSpawner>();
            spawner.owner = owner;
            spawner.warningRingPrefab = LoadPrefab("FX/WarningRing");
            spawner.lightBeamPrefab = LoadPrefab("FX/LightBeam");
            spawner.partPrefabs = new GameObject[PartFiles.Length];
            for (int i = 0; i < PartFiles.Length; i++)
                spawner.partPrefabs[i] = LoadPrefab("Parts/" + PartFiles[i]);

            var ring = island.transform.Find("CraftRing");
            if (ring == null)
            {
                ring = new GameObject("CraftRing").transform;
                ring.SetParent(island.transform, false);
                ring.localPosition = new Vector3(0f, 0.02f, -1.8f);
            }
            ring.gameObject.AddComponent<CraftZone>().owner = owner;
            return island;
        }

        static PlayerController BuildPlayer(PlayerId id, string prefab, GameObject island)
        {
            var pos = island.transform.position + new Vector3(0f, 0f, 1f);
            var go = Spawn(prefab, id == PlayerId.P1 ? "Pip_P1" : "Momo_P2", pos)
                     ?? GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = Vector3.one * CharacterScale;

            var spawnPoint = new GameObject(go.name + "_Spawn").transform;
            spawnPoint.SetPositionAndRotation(pos, go.transform.rotation);
            spawnPoint.SetParent(island.transform, true);

            var controller = go.AddComponent<PlayerController>();
            controller.id = id;
            controller.islandCenter = island.transform;
            controller.spawnPoint = spawnPoint;
            var look = go.AddComponent<CharacterLook>();
            look.id = id;

            var zone = island.GetComponentInChildren<CraftZone>();
            if (zone != null) zone.player = controller;
            return controller;
        }

        static void BuildRobot()
        {
            Spawn("Islands/RobotIsland", "Robot_Island", RobotIslandPos);
            var robot = Spawn("Robot/Cpe40", "Cpe40", RobotIslandPos + new Vector3(0f, 0f, 0.3f))
                        ?? new GameObject("Cpe40");
            robot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            robot.transform.localScale = Vector3.one * RobotScale;
            var boss = robot.AddComponent<RobotBoss>();
            boss.gemPrefab = LoadPrefab("Parts/Gem");
            boss.warningRingPrefab = LoadPrefab("FX/WarningRing");
        }

        static void BuildDecor()
        {
            var decor = new GameObject("Decor").transform;
            Vector3[] ducks = { new Vector3(-1.5f, -2.5f, -6f), new Vector3(12.5f, -2.5f, 4f), new Vector3(-13f, -2.5f, 3f) };
            foreach (var p in ducks)
            {
                var duck = Spawn("Props/Duck", "Duck", p);
                if (duck == null) continue;
                duck.transform.SetParent(decor, true);
                duck.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                AddByName(duck, "IslandRocketRumble.Bobbing");
            }
            for (int i = 0; i < 2; i++)
            {
                var gull = Spawn("Props/Seagull", "Seagull", new Vector3(i == 0 ? -4f : 5f, 5f, 8f));
                if (gull == null) continue;
                gull.transform.SetParent(decor, true);
                AddByName(gull, "IslandRocketRumble.SeagullCircle");
            }
        }

        static GameObject LoadPrefab(string relative)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{relative}.prefab");
            if (prefab == null) Debug.LogWarning("[IRR] Missing prefab: " + relative);
            return prefab;
        }

        static GameObject Spawn(string relative, string name, Vector3 position)
        {
            var prefab = LoadPrefab(relative);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = position;
            return go;
        }

        /// <summary>Adds a component by type name so this builder doesn't hard-depend on optional modules.</summary>
        static void AddByName(GameObject go, string typeName)
        {
            Type type = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null) break;
            }
            if (type == null) Debug.LogWarning("[IRR] Component type not found: " + typeName);
            else if (go.GetComponent(type) == null) go.AddComponent(type);
        }
    }
}
