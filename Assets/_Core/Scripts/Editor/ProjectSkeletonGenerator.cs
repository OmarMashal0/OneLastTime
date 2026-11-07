#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace OneLastTime.EditorTools
{
    public static class ProjectSkeletonGenerator
    {
        private struct SceneConfig
        {
            public string scenePath;
            public bool isLevelScene;

            public SceneConfig(string path, bool isLevel)
            {
                scenePath = path;
                isLevelScene = isLevel;
            }
        }

        private static readonly SceneConfig[] AllScenes = new[]
        {
            new SceneConfig("Assets/_Core/Scenes/00_MainMenu.unity", false),
            new SceneConfig("Assets/_Core/Scenes/01_IntroCutscene.unity", false),
            new SceneConfig("Assets/Level1_Museum/Scenes/L1_A_MuseumStealth.unity", true),
            new SceneConfig("Assets/Level1_Museum/Scenes/L1_B_DisplayRoom.unity", true),
            new SceneConfig("Assets/Level2_Village/Scenes/L2_A_VillageChase.unity", true),
            new SceneConfig("Assets/Level2_Village/Scenes/L2_B_NoticeboardDecode.unity", true),
            new SceneConfig("Assets/Level3_Bamboo/Scenes/L3_A_BambooTrail.unity", true),
            new SceneConfig("Assets/Level3_Bamboo/Scenes/L3_B_RiverCrossing.unity", true),
            new SceneConfig("Assets/Level4_Temple/Scenes/L4_A_TempleCollapse.unity", true),
            new SceneConfig("Assets/Level4_Temple/Scenes/L4_B_VisionsAndReina.unity", true),
            new SceneConfig("Assets/Level5_Hideout/Scenes/L5_A_HallwayCollapse.unity", true),
            new SceneConfig("Assets/Level5_Hideout/Scenes/L5_B_YamiFinalBattle.unity", true),
            new SceneConfig("Assets/_Core/Scenes/99_EndingChoice.unity", false),
        };

        [MenuItem("Tools/One Last Time/Generate All Scenes & Hierarchies")]
        public static void GenerateAllScenes()
        {
            var editorBuildSettingsScenes = new EditorBuildSettingsScene[AllScenes.Length];

            for (int i = 0; i < AllScenes.Length; i++)
            {
                var config = AllScenes[i];
                string directory = Path.GetDirectoryName(config.scenePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create new empty scene in editor
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                if (config.isLevelScene)
                {
                    BuildStandardLevelHierarchy();
                }
                else
                {
                    BuildStandardMenuOrCutsceneHierarchy(Path.GetFileNameWithoutExtension(config.scenePath));
                }

                EditorSceneManager.SaveScene(scene, config.scenePath);
                editorBuildSettingsScenes[i] = new EditorBuildSettingsScene(config.scenePath, true);
            }

            EditorBuildSettings.scenes = editorBuildSettingsScenes;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[One Last Time] Successfully generated {AllScenes.Length} scenes with standardized hierarchies and added to Build Settings!");
        }

        private static void BuildStandardLevelHierarchy()
        {
            // --- SYSTEMS ---
            GameObject systemsRoot = new GameObject("--- SYSTEMS ---");
            CreateChild(systemsRoot, "GameManager");
            CreateChild(systemsRoot, "AudioManager");
            CreateChild(systemsRoot, "LevelController");

            // --- CAMERAS ---
            GameObject camerasRoot = new GameObject("--- CAMERAS ---");
            GameObject mainCam = CreateChild(camerasRoot, "Main Camera");
            Camera cam = mainCam.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            mainCam.tag = "MainCamera";
            mainCam.AddComponent<AudioListener>();
            CreateChild(camerasRoot, "CameraBounds");

            // --- UI ---
            GameObject uiRoot = new GameObject("--- UI ---");
            CreateChild(uiRoot, "HUD_Canvas");
            CreateChild(uiRoot, "Interaction_Canvas");
            CreateChild(uiRoot, "Cutscene_Canvas");

            // --- ENVIRONMENT ---
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");
            CreateChild(envRoot, "Background_Parallax");
            CreateChild(envRoot, "Grid_Tilemap");
            CreateChild(envRoot, "Props_Static");
            CreateChild(envRoot, "Lighting_2D");

            // --- GAMEPLAY & INTERACTABLES ---
            GameObject gameplayRoot = new GameObject("--- GAMEPLAY & INTERACTABLES ---");
            CreateChild(gameplayRoot, "Checkpoints");
            CreateChild(gameplayRoot, "Interactables");
            CreateChild(gameplayRoot, "Hazards_And_Puzzles");
            CreateChild(gameplayRoot, "Triggers");
            CreateChild(gameplayRoot, "Projectiles_Pool");

            // --- CHARACTERS ---
            GameObject charRoot = new GameObject("--- CHARACTERS ---");
            CreateChild(charRoot, "Player_Ryu");
            CreateChild(charRoot, "Companion_Raccoon");
            CreateChild(charRoot, "Ally_Reina");
            CreateChild(charRoot, "Enemies");
        }

        private static void BuildStandardMenuOrCutsceneHierarchy(string name)
        {
            GameObject systemsRoot = new GameObject("--- SYSTEMS ---");
            CreateChild(systemsRoot, "SceneController");
            CreateChild(systemsRoot, "AudioManager");

            GameObject camerasRoot = new GameObject("--- CAMERAS ---");
            GameObject mainCam = CreateChild(camerasRoot, "Main Camera");
            Camera cam = mainCam.AddComponent<Camera>();
            cam.orthographic = true;
            mainCam.tag = "MainCamera";
            mainCam.AddComponent<AudioListener>();

            GameObject uiRoot = new GameObject("--- UI ---");
            CreateChild(uiRoot, "Canvas_" + name);
        }

        private static GameObject CreateChild(GameObject parent, string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            return child;
        }
    }
}
#endif
