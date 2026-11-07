# One Last Time — Project Skeleton & Architecture Specification

**Project:** *One Last Time* (2D Side-scrolling Action Adventure in Unity)  
**Target Unity Version:** `2021.3.10f1` (2D Template, 2D Feature Set)  
**GDD Team:** Omar Ahmed Mashal, Nada Hatem, Eyad Marii, Ziad Mostafa (Group #39)  
**Status:** Approved Design Specification

---

## 1. Architectural Strategy

1. **Two Scenes per Level (10 Level Scenes + 3 Global Scenes):**
   - Each level splits into **Part A** (Stealth / Exploration / Initial Encounter) and **Part B** (Puzzle / Boss / Climax).
   - This enables pairs of teammates to work simultaneously in Git without encountering binary merge conflicts in `.unity` scene files.
2. **Level-First Asset Organization:**
   - Universal assets, prefabs, UI, and base systems live in `Assets/_Core/`.
   - Level-specific assets, art, prefabs, and scripts live in dedicated folders (`Assets/Level1_Museum/` through `Assets/Level5_Hideout/`).
3. **Standardized 6-Group Scene Hierarchy Skeleton:**
   - Every single level scene uses identical top-level parent GameObjects:
     - `--- SYSTEMS ---`
     - `--- CAMERAS ---`
     - `--- UI ---`
     - `--- ENVIRONMENT ---`
     - `--- GAMEPLAY & INTERACTABLES ---`
     - `--- CHARACTERS ---`

---

## 2. Complete Asset Folder Structure

```text
Assets/
├── _Core/
│   ├── Art/
│   │   ├── Characters/
│   │   │   ├── Ryu/
│   │   │   ├── Raccoon/
│   │   │   ├── Reina/
│   │   │   └── Yami/
│   │   ├── UI/
│   │   │   ├── Health_Heart/
│   │   │   ├── Sanity_Brain/
│   │   │   └── Quest_Scroll/
│   │   └── VFX/
│   │       ├── Sparks_Parry/
│   │       ├── Slash_Katana/
│   │       └── Hallucination_Vignette/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Prefabs/
│   │   ├── Characters/
│   │   ├── Environment/
│   │   ├── Systems/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── 00_MainMenu.unity
│   │   ├── 01_IntroCutscene.unity
│   │   └── 99_EndingChoice.unity
│   └── Scripts/
│       ├── Player/
│       ├── Companion/
│       ├── Combat/
│       ├── Interaction/
│       └── Systems/
│
├── Level1_Museum/
│   ├── Art/
│   ├── Audio/
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── L1_A_MuseumStealth.unity
│   │   └── L1_B_DisplayRoom.unity
│   └── Scripts/
│
├── Level2_Village/
│   ├── Art/
│   ├── Audio/
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── L2_A_VillageChase.unity
│   │   └── L2_B_NoticeboardDecode.unity
│   └── Scripts/
│
├── Level3_Bamboo/
│   ├── Art/
│   ├── Audio/
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── L3_A_BambooTrail.unity
│   │   └── L3_B_RiverCrossing.unity
│   └── Scripts/
│
├── Level4_Temple/
│   ├── Art/
│   ├── Audio/
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── L4_A_TempleCollapse.unity
│   │   └── L4_B_VisionsAndReina.unity
│   └── Scripts/
│
└── Level5_Hideout/
    ├── Art/
    ├── Audio/
    ├── Prefabs/
    ├── Scenes/
    │   ├── L5_A_HallwayCollapse.unity
    │   └── L5_B_YamiFinalBattle.unity
    └── Scripts/
```

---

## 3. Scene Breakdown & Narrative Mapping

| Scene File | Level Title | Key Narrative & Gameplay Elements |
|---|---|---|
| `00_MainMenu.unity` | Main Menu | Start Game, Settings, Credits |
| `01_IntroCutscene.unity` | Intro Story | Ryu's backstory, war memory, finding the note: *"One last time"* |
| `L1_A_MuseumStealth.unity` | L1: Bless Your Sword Bleed (Part A) | Night approach, patrol guards, pillar stealth cover, checkpoint detection |
| `L1_B_DisplayRoom.unity` | L1: Bless Your Sword Bleed (Part B) | Painting fragment puzzle, katana showcase, bullet slash CG, cracked wall break |
| `L2_A_VillageChase.unity` | L2: The Hidden Path (Part A) | Note stolen by child, asking villagers, dodging bunny carrot throws |
| `L2_B_NoticeboardDecode.unity` | L2: The Hidden Path (Part B) | Finding Je3efar with child, symbol-to-letter decode puzzle on noticeboard, exit gate |
| `L3_A_BambooTrail.unity` | L3: Whispers in the Bamboo (Part A) | Badawy mushroom boss & Gloom Eye spawners (every 8s), children whisper hallucinations |
| `L3_B_RiverCrossing.unity` | L3: Whispers in the Bamboo (Part B) | Fragile wooden logs over river, checkpoint lanterns, watched wooden gate |
| `L4_A_TempleCollapse.unity` | L4: Echoes of the Past (Part A) | 2 Axe guards duel at entrance, temple collapse, 10s reversed controls warning |
| `L4_B_VisionsAndReina.unity` | L4: Echoes of the Past (Part B) | Non-lethal shadowy hallucination knights, duel with stranger, Reina reveal cutscene |
| `L5_A_HallwayCollapse.unity` | L5: What the Blade Unveiled (Part A) | War recordings corridor, sanity collapse & faint, Reina revives Ryu (Katana Slash unlock) |
| `L5_B_YamiFinalBattle.unity` | L5: What the Blade Unveiled (Part B) | Yami boss fight: rockets, explosive shuriken, shield break, Katana Slash finisher |
| `99_EndingChoice.unity` | Final Choice & Epilogue | Yami revelation, Vengeance vs Mercy branch, rescue family cutscene |

---

## 4. Standard Scene Hierarchy Skeleton

Every level scene will contain the following hierarchy structure:

```text
SceneRoot
├── --- SYSTEMS ---
│   ├── GameManager
│   ├── AudioManager
│   └── LevelController
├── --- CAMERAS ---
│   ├── Main Camera
│   └── CameraBounds
├── --- UI ---
│   ├── HUD_Canvas
│   ├── Interaction_Canvas
│   └── Cutscene_Canvas
├── --- ENVIRONMENT ---
│   ├── Background_Parallax
│   ├── Grid_Tilemap
│   ├── Props_Static
│   └── Lighting_2D
├── --- GAMEPLAY & INTERACTABLES ---
│   ├── Checkpoints
│   ├── Interactables
│   ├── Hazards_And_Puzzles
│   ├── Triggers
│   └── Projectiles_Pool
└── --- CHARACTERS ---
    ├── Player_Ryu
    ├── Companion_Raccoon
    ├── Ally_Reina
    └── Enemies
```
