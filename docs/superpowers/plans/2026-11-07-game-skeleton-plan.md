# One Last Time Project Skeleton Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the complete folder architecture, Unity editor scaffolding tool, and all 13 level/menu scenes pre-populated with the standardized 6-group Scene Hierarchy skeleton for *One Last Time*.

**Architecture:** 
1. Build out the Level-First folder structure under `Assets/` (`_Core`, `Level1_Museum` ... `Level5_Hideout`).
2. Implement a dedicated Unity Editor script (`Assets/_Core/Scripts/Editor/ProjectSkeletonGenerator.cs`) using `UnityEditor.SceneManagement` to programmatically build the 13 scenes with standardized root hierarchy GameObjects and save them into their designated folders.
3. Configure Build Settings to register all 13 scenes in chronological story order.

**Tech Stack:** Unity 2021.3.10f1, C# (.NET Standard 2.1), UnityEditor, Git.

**Spec:** `docs/superpowers/specs/2026-11-07-game-skeleton-design.md`

## Global Constraints

- Unity Editor version: `2021.3.10f1`.
- Project Path: `d:\Unity\Projects\OneLastTime`.
- Architecture: Level-First folder organization (`_Core` + 5 Levels).
- Standard Hierarchy root GameObjects:
  - `--- SYSTEMS ---`
  - `--- CAMERAS ---`
  - `--- UI ---`
  - `--- ENVIRONMENT ---`
  - `--- GAMEPLAY & INTERACTABLES ---`
  - `--- CHARACTERS ---`

---

### Task 1: Create Directory Skeleton

**Files:**
- Create: Directories across `Assets/_Core/` and `Assets/Level1_Museum/` through `Assets/Level5_Hideout/`

**Interfaces:**
- Consumes: Directory structure defined in spec `docs/superpowers/specs/2026-11-07-game-skeleton-design.md`
- Produces: Complete empty folder tree in filesystem ready for Unity meta tracking and asset placement

- [ ] **Step 1: Create all directory paths**
Create the directories via PowerShell script:
```powershell
$dirs = @(
    "Assets/_Core/Art/Characters/Ryu",
    "Assets/_Core/Art/Characters/Raccoon",
    "Assets/_Core/Art/Characters/Reina",
    "Assets/_Core/Art/Characters/Yami",
    "Assets/_Core/Art/UI/Health_Heart",
    "Assets/_Core/Art/UI/Sanity_Brain",
    "Assets/_Core/Art/UI/Quest_Scroll",
    "Assets/_Core/Art/VFX/Sparks_Parry",
    "Assets/_Core/Art/VFX/Slash_Katana",
    "Assets/_Core/Art/VFX/Hallucination_Vignette",
    "Assets/_Core/Audio/Music",
    "Assets/_Core/Audio/SFX",
    "Assets/_Core/Prefabs/Characters",
    "Assets/_Core/Prefabs/Environment",
    "Assets/_Core/Prefabs/Systems",
    "Assets/_Core/Prefabs/UI",
    "Assets/_Core/Scenes",
    "Assets/_Core/Scripts/Player",
    "Assets/_Core/Scripts/Companion",
    "Assets/_Core/Scripts/Combat",
    "Assets/_Core/Scripts/Interaction",
    "Assets/_Core/Scripts/Systems",
    "Assets/_Core/Scripts/Editor",
    "Assets/Level1_Museum/Art",
    "Assets/Level1_Museum/Audio",
    "Assets/Level1_Museum/Prefabs",
    "Assets/Level1_Museum/Scenes",
    "Assets/Level1_Museum/Scripts",
    "Assets/Level2_Village/Art",
    "Assets/Level2_Village/Audio",
    "Assets/Level2_Village/Prefabs",
    "Assets/Level2_Village/Scenes",
    "Assets/Level2_Village/Scripts",
    "Assets/Level3_Bamboo/Art",
    "Assets/Level3_Bamboo/Audio",
    "Assets/Level3_Bamboo/Prefabs",
    "Assets/Level3_Bamboo/Scenes",
    "Assets/Level3_Bamboo/Scripts",
    "Assets/Level4_Temple/Art",
    "Assets/Level4_Temple/Audio",
    "Assets/Level4_Temple/Prefabs",
    "Assets/Level4_Temple/Scenes",
    "Assets/Level4_Temple/Scripts",
    "Assets/Level5_Hideout/Art",
    "Assets/Level5_Hideout/Audio",
    "Assets/Level5_Hideout/Prefabs",
    "Assets/Level5_Hideout/Scenes",
    "Assets/Level5_Hideout/Scripts"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path $d | Out-Null
}
```

- [ ] **Step 2: Verify folder structure exists**
Run: `Get-ChildItem -Path Assets -Directory`
Expected: `_Core`, `Level1_Museum`, `Level2_Village`, `Level3_Bamboo`, `Level4_Temple`, `Level5_Hideout` (plus legacy `Scenes`).

- [ ] **Step 3: Remove default empty SampleScene and folder if obsolete**
Remove `Assets/Scenes` if no longer needed or keep archived.

- [ ] **Step 4: Commit folder skeleton**
```bash
git add Assets/
git commit -m "feat: scaffold Level-First folder skeleton across _Core and Levels 1 to 5"
```

---

### Task 2: Create Unity Editor Scene & Hierarchy Generator Tool

**Files:**
- Create: `Assets/_Core/Scripts/Editor/ProjectSkeletonGenerator.cs`

**Interfaces:**
- Consumes: Unity Editor APIs (`UnityEditor.SceneManagement.EditorSceneManager`, `UnityEngine.SceneManagement`)
- Produces: Menu item `Tools > One Last Time > Generate All Scenes & Hierarchies`

- [ ] **Step 1: Write `ProjectSkeletonGenerator.cs`**
Create the editor script with methods to create and populate each of the 13 scenes according to the standard hierarchy specification:
```csharp
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

                // Create new scene in editor
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
```

- [ ] **Step 2: Commit generator script**
```bash
git add Assets/_Core/Scripts/Editor/ProjectSkeletonGenerator.cs
git commit -m "feat: add ProjectSkeletonGenerator editor tool for automated scene and hierarchy setup"
```

---

### Task 3: Execute Generator & Verify Scenes in Unity

**Files:**
- Generates: 13 `.unity` scene files + `.unity.meta` files across `Assets/_Core/Scenes/` and Level folders
- Updates: `ProjectSettings/EditorBuildSettings.asset`

- [ ] **Step 1: Execute generator via Unity MCP or batchmode**
Invoke `ProjectSkeletonGenerator.GenerateAllScenes()` via Unity MCP execute_code / execute_menu_item or run Unity in batchmode:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2021.3.10f1\Editor\Unity.exe" -quit -batchmode -projectPath "d:\Unity\Projects\OneLastTime" -executeMethod OneLastTime.EditorTools.ProjectSkeletonGenerator.GenerateAllScenes -logFile "generate_scenes.log"
```

- [ ] **Step 2: Verify all 13 scenes exist on disk**
Check that every path in `AllScenes` has been created and populated with valid YAML scene data.

- [ ] **Step 3: Verify standard hierarchy objects in a generated scene**
Inspect `Assets/Level1_Museum/Scenes/L1_A_MuseumStealth.unity` to confirm:
- `--- SYSTEMS ---`
- `--- CAMERAS ---`
- `--- UI ---`
- `--- ENVIRONMENT ---`
- `--- GAMEPLAY & INTERACTABLES ---`
- `--- CHARACTERS ---`

- [ ] **Step 4: Verify EditorBuildSettings contains all 13 scenes**
Inspect `ProjectSettings/EditorBuildSettings.asset` to confirm all 13 scenes are registered with active checkboxes.

- [ ] **Step 5: Commit generated scenes and project settings**
```bash
git add Assets/ ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: generate 13 level/cutscene/menu scenes with standardized hierarchy skeleton"
```

---

### Task 4: Clean up obsolete files and document usage for teammates

**Files:**
- Modify/Delete: `Assets/Scenes/SampleScene.unity`
- Create: `README.md` (or update with group structure, scene mapping, and teammate workflow)

- [ ] **Step 1: Safely delete old `Assets/Scenes/SampleScene.unity`**
```bash
git rm -r -f Assets/Scenes/
```

- [ ] **Step 2: Create root `README.md` documenting team workflow**
Add quick guide for Omar, Nada, Eyad, and Ziad on where to place sprites/audio, how the 2 scenes per level work, and the hierarchy guidelines.

- [ ] **Step 3: Commit and verify clean git status**
```bash
git add README.md
git commit -m "docs: add teammate guide for scene hierarchy and asset organization"
```

