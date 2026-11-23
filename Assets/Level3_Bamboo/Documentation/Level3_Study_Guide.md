# Level 3 — Bamboo Forest: Study Guide

Scenes: `L3_A_BambooTrail` (the trail) and `L3_B_RiverCrossing` (the river).
All Level 3 scripts are in `Assets/Level3_Bamboo/Scripts`.
Lab references are to the module's "2D Unity Lab" PDFs (Lab number, page).
"Not covered by labs" means it is our own feature, written in the same simple style.

---

## 1. The Hierarchy in one minute

Both scenes use the same top-level groups:

| Group | What is inside |
|---|---|
| **Systems** | Things that run the level but are not seen: EventSystem (for UI clicks), music, the QuestDirector (L3_A), the Audio sources (L3_B). |
| **Camera** | `Main Camera` with `CameraFollow2D`. |
| **Environment** | Everything you see that is not gameplay: Background layers, Ground, Platforms, Decorations, Wildlife, Atmosphere (mist, light rays, particles), Weather. |
| **Player** | `Ryu` (the team's shared prefab). Ryu's real object is the child `Player_Ryu`. |
| **Enemies** (L3_A) | `BadawyFight` (Badawy + start trigger + trail gate) and `SwampThing`. |
| **Interactables** | Logs, checkpoints, the river exit / river crossing. |
| **UI** | `VitalsHUD` (health, sanity, pause, mute — a prefab used in both scenes) and the level's own canvas. |

Parent–child relationships that **must** stay as they are:
- `StartingHouse` children (lantern, curtains): they are animated parts of one house prefab.
- `SwampThing` children (hitboxes, health bar): they move with the enemy.
- Everything under a Canvas: UI only draws inside a Canvas.
- `Ryu` → `Player_Ryu` → `groundFlag`: part of the shared team prefab.
- `Grid_Tilemap` → `Ground`: a Tilemap must sit under a Grid.

---

## 2. Feature / object / script table

| Feature | Select in Hierarchy | Script | Lab |
|---|---|---|---|
| Ryu movement & jump | Player > Ryu > Player_Ryu | `PlayerMovement` (team, shared) | Lab 2 p24–37 |
| Ryu animation | Player_Ryu (Animator) | `PlayerMovement` sets parameters | Lab 3 p10–25 |
| Health, sanity, katana, death, respawn | Player_Ryu | `BambooPlayerController` | Lab 6 p5–8 (health/immunity), Lab 2 p35 (ground check) |
| Camera follow + zoom | Camera > Main Camera | `CameraFollow2D` | Lab 4 p10–19 |
| Checkpoints | Interactables > Checkpoints > … | `ForestCheckpoint` | Lab 5 p8–10 |
| Falling / drowning = death | Player_Ryu (`fallDeathY`), RiverCrossing (water) | `BambooPlayerController`, `RiverCrossing` | Lab 5 p14 (DeadlyDrop idea) |
| HUD health & sanity bars | UI > VitalsHUD | `Level3VitalsHud` | Lab 8 p16–17 |
| Pause / mute buttons | UI > VitalsHUD > Pause and Resume, Music Mute | `ForestHudButton` | Lab 8 (UI buttons); pause itself not covered |
| Logs to collect (press E) | Interactables > LogCollection | `LogCollection`, `CollectibleLog` | Lab 7 p9–14 (pickups, idea); E-key not covered |
| Carrying progress to the river | Interactables > RiverExit | `BambooRiverExit`, `RiverProgress` | Lab 7 p10 (static stats), p14 (EnterLevel2) |
| Badawy boss fight | Enemies > BadawyFight | `BadawyEncounter`, `FightStartTrigger`, `ForestEnemy` | Lab 6 p10–25 (enemies), Lab 9 p16 (trigger → manager) |
| Swamp Thing + cage | Enemies > SwampThing, Interactables > LogCollection > Log4_LogCage, Interactables > SwampDialogueTrigger | `SwampThing`, `SwampCageEncounter`, `SwampDialogue` | Lab 6 (damage); Lab 9 p6–12 (typewriter dialogue); boss AI not covered |
| Quest scroll | Systems > QuestDirector, UI > LevelCanvas > Bamboo Quest Scroll | `BambooQuestDirector`, `PlayerTriggerZone`, `BambooQuestScroll` | Lab 9 (typewriter idea); quests not covered |
| Rock & seesaw puzzle | Interactables > LogCollection > Log2_RockSeesaw | `LogSeesaw` | not covered (uses Lab 5's Rigidbody2D ideas) |
| Bamboo chime puzzle (E, sequence counter) | Log1_ChimePuzzle | `ChimePuzzle`, `ChimeInteract` | Lab 9-style triggers and E input |
| Rope cut with katana | Log5_SuspendedLog | `KatanaLogObstacle` | not covered |
| River crossing | Interactables > RiverCrossing | `RiverCrossing`, `FloatingRiverLog` | Lab 5 (physics platforms idea); not covered |
| Background parallax | Environment > Background > BG_… | `ParallaxLayer` | not covered (Lab 1 sorting layers) |
| Weather (snow, rainy night) | Environment > Weather | `BambooWeatherController` | Lab 4 particles (Inspector); logic not covered |
| Wind | Environment > Weather > ForestWind | `ForestWind` | not covered |
| Swaying plants, mist, light rays, fireflies, butterflies, birds | Environment > Decorations / Wildlife / Atmosphere | `WindSway`, `DriftingMist`, `LightRayPulse`, `Firefly`, `Butterfly`, `PerchedBird`, `BreathingAnimal`, `GustLeaf`, `ForestDecorationMotion` | not covered |
| Reactive wildlife (fox, frog, owl, snail, moths, leaf on Ryu) | Environment > Wildlife | `WatchingFox`, `LeafFrog`, `WatchfulOwl`, `StoneSnail`, `LanternMoths`, `IdleLeafOnRyu` | not covered |
| Starting house & flag | Environment > Decorations > StartingHouse, EntranceFlag | `EntranceSpriteLoop` (house), Animator (flag) | Lab 3 (Animator for the flag) |
| Level title | UI > LevelCanvas > LevelTitle | `LevelTitleFade`, `TitleFleck` | not covered |

---

## 3. Every feature, step by step

### Ryu (player)
- **Select:** Player > Ryu > **Player_Ryu**.
- **Look at:** `PlayerMovement` (team script: speed, jump), `BambooPlayerController` (Level 3 rules).
  In `BambooPlayerController`: *Ground Check* (`groundCheck` = groundFlag, `groundCheckRadius`, `whatIsGround`),
  *Katana attack* (`firstSwingFrames`, `secondSwingFrames`, `strikeFrame`), *Hurt* (`hurtFrames`, `hurtClip`,
  `damageInvulnerability`), *Death & Respawn* (`respawnDelay`, `fallDeathY`), *Vitals* (`maxHealth`, `maxSanity`).
- **Starts:** always active. J = attack, K = test death.
- **Then:** `TakeDamage()` lowers health, flashes Ryu red, shows the hurt frames, plays the hurt sound and gives a short
  immunity time (like Lab 6's `isImmune`). At 0 health → `TriggerDeath()`: death animation, controls off.
- **Reset:** after `respawnDelay` seconds, `Respawn()` puts Ryu at the last checkpoint with full health and tells other
  scripts (`Respawned`) so they can reset too.
- **Say to the TA:** "The team's PlayerMovement moves Ryu. My BambooPlayerController adds health, the katana and respawning.
  The ground check is the Lab 2 one: an OverlapCircle at the groundFlag point using the whatIsGround layer."

### Katana attack
- **Select:** Player_Ryu → `BambooPlayerController`, *Katana attack* section.
- **Starts:** pressing J.
- **Then:** Ryu stops (on the ground), the attack frames play from the two frame lists (they alternate each swing).
  On `strikeFrame` the script sends `KatanaSwung` once; enemies and the rope check if the blade reached them.
- **Why sprites and not the Animator?** The team's shared Animator Controller has no attack clip, and we must not edit
  shared assets. The frames are Level 3 copies of the team sheets in `Art/Character/Ryu Combat`, sliced in the Sprite Editor.
- **Say:** "LateUpdate runs after the Animator, so during a swing it swaps in the attack sprite for that frame."

### Camera
- **Select:** Camera > Main Camera → `CameraFollow2D`.
- **Fields:** `target` (Player_Ryu), `offset`, `smoothTime`, zoom limits, `minWorldX` / `maxWorldX`, `groundBottomY`.
- **Then:** follows Ryu smoothly, leads a little in front of him, and is clamped so it never shows past the level edges
  (Lab 4's minX/maxX idea). Mouse wheel or +/- zooms, clamped between min and max (Lab 4's CameraZoom).
- **Say:** "Like Lab 4 but with SmoothDamp instead of Lerp, so it never overshoots."

### Checkpoints (lanterns)
- **Select:** Interactables > Checkpoints > `Checkpoint_GroveApproach`, `Checkpoint_GroveExit`, `RiverCheckpoint`.
- **Look at:** the Box Collider 2D (**Is Trigger** on), `ForestCheckpoint` (`respawnPoint`, `requiredEncounter`).
- **Starts:** `OnTriggerStay2D` when an object tagged **Player** is inside (Lab 5 p10 uses OnTriggerEnter2D;
  Stay is used so the Grove Exit lantern still lights if Ryu is already standing there when Badawy dies).
- **Then:** the lantern sprite changes to lit, "Checkpoint" shows for a moment, and `player.SetCheckpoint()` stores the
  respawn point. Grove Exit only works after the Badawy fight is completed.
- **Reset:** lanterns stay lit for the rest of the scene.
- **Say:** "Lab 5 kept the checkpoint in a LevelManager; here Ryu's own script remembers it, so there is no extra manager."

### HUD (health, sanity, pause, mute)
- **Select:** UI > **VitalsHUD** (prefab `Prefabs/Level 3 Vitals HUD`).
- **Look at:** `Level3VitalsHud` (`healthFill`, `sanityFill`) and the two `ForestHudButton`s (`action` = Pause or MuteMusic).
- **Then:** every frame `fillAmount = health / maxHealth` (exactly Lab 8 p17). Pause sets `Time.timeScale = 0`;
  everything in Level 3 checks `Time.timeScale <= 0` and stops. Mute toggles the music AudioSource.
- **Advanced part:** `EditorPauseFix` (see section 5).
- **Say:** "Same health bar as Lab 8, using the player's real maxHealth instead of 3."

### Log collection (the six logs)
- **Select:** Interactables > **LogCollection**; the five sites `Log1_ChimePuzzle` … `Log5_SuspendedLog`.
  Log 6 (`Log_06 (Swamp Thing reward)`) is under `Log4_LogCage` and appears when Swamp Thing is defeated.
- **Look at:** `LogCollection` (`logs` array, `interactionDistance`, prompt sprites, `counter`).
- **Starts:** Ryu stands close to an available log and presses **E**.
- **Then:** the log disappears, flies to the counter icon, the counter shows "Logs: n/6".
- **Reset:** collected logs stay collected after a respawn (progress is kept, like Lab 7's score).
- **Say:** "It is a pickup like Lab 7's coin, but you press E instead of just touching it, so you don't grab logs by accident."

### Log puzzles
- **Bamboo chimes** (`Log1_ChimePuzzle` → `ChimePuzzle`, one `ChimeInteract` per chime): the clue stone shows MOON → LEAF → SUN.
  Near a chime, E rings the closest one (Animator state **Ring**, its own AudioSource pitch). `nextIndex` counts correct chimes;
  a wrong chime flashes, plays a thud and sets `nextIndex` to 0. After three correct chimes the chest swaps to its empty-open
  sprite and `Log_01` (the log sprite inside it) is unlocked; E near the chest then collects it through LogCollection.
  Respawning resets an unfinished sequence; a solved puzzle and a collected log stay that way.
- **Rope** (`Log5_SuspendedLog` → `KatanaLogObstacle`): a katana strike that reaches the rope cuts it, unlocking the log.
- **Rock & seesaw** (`Log2_RockSeesaw` → `LogSeesaw`): walk right into the `Pushable Rock` to push it. It has a
  **Kinematic Rigidbody2D** and is moved with `MovePosition`, so Ryu pushes it smoothly. On the plank the rock tips the
  seesaw and the raised step lets Ryu reach the log.
- **Clearing steps** (`Log3_ClearingSteps`): platforming only.
- **Cage** (`Log4_LogCage` → `SwampCageEncounter`): Log 4 is locked in a cage. Ryu lures Swamp Thing next to it so his
  arm strike smashes the cage (`TryBreakFromArm`); if that never happens, the cage opens when Swamp Thing dies.
  Log 4 can be collected once the cage is broken **and** Swamp Thing is defeated.

### Badawy fight
- **Select:** Enemies > **BadawyFight** (and its children Badawy, FightStartTrigger, TrailGate).
- **Look at:** `BadawyEncounter` (`spawnInterval`, `purpleEye`, `greenEye`), `FightStartTrigger` (`fight`).
- **Starts:** `FightStartTrigger.OnTriggerEnter2D` when the **Player** tag enters → `fight.StartFight()`
  (same idea as Lab 9's NPCDialogue trigger calling the dialogue manager).
- **Then:** title fades in; every `spawnInterval` seconds Badawy warns and summons a pair of Gloom Eyes
  (Instantiate, Lab 6 p20–21 SpawnGhost). Katana hits lower Badawy's health (`ForestEnemy`, like Lab 9's EnemyTakeDamage).
- **End:** Badawy at 0 health → fight `Completed`, the TrailGate opens, the Grove Exit lantern can be lit.
- **Reset:** if Ryu dies first, the eyes are destroyed and Badawy goes back to the start.
- **Say:** "Enemies have a small state machine: Idle, Warning, Charge, Recover, Hurt, Dead."

### Swamp Thing
- **Select:** Enemies > **SwampThing**; the cage is Interactables > LogCollection > Log4_LogCage;
  the dialogue is Interactables > SwampDialogueTrigger (panel: UI > VitalsHUD > SwampDialogue).
- **Look at:** `SwampThing` (`maxHealth`, damage values, `wakeRange`, `minEncounterX`/`maxEncounterX`), `SwampDialogue`.
- **Starts:** Ryu comes within `wakeRange` of him (he stays dormant until then). Walking into SwampDialogueTrigger
  shows his first line.
- **Then:** he strikes with his arm (warning first), burrows, emerges under Ryu, and can be hit with the katana.
  If his arm strike hits the cage, the cage breaks. `SwampDialogue` types short lines letter by letter (Lab 9 typewriter idea).
- **End:** at 0 health he dies, the cage opens if it was still closed, and Log 6 appears where he fell.
- **Reset:** if Ryu dies, the attempt resets (Swamp Thing goes dormant again and the dialogue can replay).

### Quest scroll
- **Select:** Systems > **QuestDirector** (five child triggers with `PlayerTriggerZone`), UI > LevelCanvas > Bamboo Quest Scroll.
- **Starts:** `PlayerTriggerZone` sets `PlayerInside` in `OnTriggerEnter2D` / `OnTriggerExit2D` (tag Player).
- **Then:** when Ryu walks into a site whose log is still waiting, the scroll shows that site's title and instruction
  with a typing effect. It updates when the cage breaks, a log is collected or the Badawy fight starts.
- **Say:** "Each quest area is just a trigger; the director checks which one Ryu is in."

### River exit (to L3_B)
- **Select:** Interactables > **RiverExit** (trigger, `BambooRiverExit`).
- **Starts:** `OnTriggerStay2D` with the Player tag.
- **Then:** fewer than 6 logs → shows "River Exit Requirement" text. 6 logs → `RiverProgress.Carry()` saves the log ids,
  health, sanity and music mute in **static** variables, then loads `L3_B_RiverCrossing` (Lab 7 p10 static stats + p14 EnterLevel2).

### River crossing (L3_B)
- **Select:** Interactables > **RiverCrossing** (`LogPlacementPoint`, six `Floating Log`s, `WaterEffects`),
  Interactables > Checkpoints > `NearBankCheckpoint`, `FarBankCheckpoint`.
- **Starts:** `Start()` reads the carried values back (`RiverProgress.Receive`), so health and logs match L3_A.
  Near the cradle press **E** with 6/6 logs.
- **Then:** the logs drop into the river; Ryu jumps across them. Touching the water (below `waterHeight` between
  `leftLip` and `rightLip`) kills Ryu (like Lab 5's DeadlyDrop).
- **Log breaking:** each `Floating Log` has a `FloatingRiverLog` script. The first time Ryu lands on top of it
  (`OnCollisionEnter2D`, tag "Player") a timer starts. At 1.25 s its Animator plays **Crack**; at 4 s it plays
  **Break**, turns its BoxCollider2D off (Ryu drops into the water) and spawns the `River Splash` prefab.
- **Reset (retries):** on respawn at NearBankCheckpoint, `RiverCrossing.ResetAfterFailure` calls `ResetLog` on every log:
  timer cleared, collider back on, Animator back to **Float**.
- **End:** reaching FarBankCheckpoint → "River crossed".

### River scene look (L3_B)
| What you see | Object (Hierarchy) | How it works |
|---|---|---|
| Lavender sky | Environment > Background > `Sky` | SpriteRenderer + `ParallaxLayer` (0.9, moves almost with the camera) |
| Teal forest + misty depth | Background > `Forest Backdrop` | One wide sprite + `ParallaxLayer` (0.75) |
| Waterfall | Forest Backdrop > `Waterfall` | Animator `River Waterfall` loops 4 frames |
| Moving water | Environment > Water > `River Water` | Tiled SpriteRenderer, Animator `River Water` loops 4 frames |
| Mossy logs | Floating Log N > `Art` (Animator `Mossy Log`: Float / Crack / Break) and `Ripple` (Animator `River Ripple`) | States are started by `FloatingRiverLog` with `animator.Play(...)` |
| Splash when a log breaks | Prefab `Prefabs/River/River Splash` | Set in `RiverCrossing.ripplePrefab`; destroyed after 0.75 s |
| Mist, fireflies, falling leaves | Environment > Atmosphere > `River Mist Particles`, `Firefly Particles`, `Falling Leaf Particles` | Ordinary Particle Systems, Texture Sheet Animation in Sprites mode; they loop, nothing to reset |
| House and 竹林 flag | Decorations > River Props > `Starting House` (Entrance House prefab), `Entrance Flag` (Animator `Bamboo Grove Banner`) | Pure decoration, same assets as L3_A |
| Mossy boulders / ferns in front | Decorations > Foreground > `Mossy Boulder Left/Right`, `Fern Left/Right` | Sorting order 25 so they draw in front of Ryu |

### Background and parallax
- **Select:** Environment > **Background** > BG_Sky … BG_NearCanopy (and the foreground silhouettes in Decorations).
- **Look at:** `ParallaxLayer` → `parallax` (0.26 for the sky … 0.03 for the near canopy; negative for foreground).
- **Then:** in LateUpdate each layer moves by `cameraMovement × parallax`, so far layers seem slower.
  `BG_NearCanopy` also has `FadeAlongTrail` (it thins out near the river).
- **Sorting:** order in layer decides what is in front (Lab 1).

### Weather
- **Select:** Environment > **Weather** (`BambooWeatherController`) and its child **Zone Markers**:
  `SnowFullUntil`, `SnowGone`, `NightStarts`, `NightFull`, `NightEnds`, `NightCleared`.
- **Starts:** every frame Ryu's x is compared with the marker positions (move a marker to move the zone).
- **Then:** a `night` value moves slowly from 0 (day) to 1 (rainy night). It fades in the night veils (sprites placed
  *between* background layers, so the HUD and gameplay stay bright), sets the rain/mist emission rate, and blows
  the rain with `ForestWind`. Snow falls only near the entrance.
- **Reset:** on respawn the weather snaps to the checkpoint's value. Pause freezes it.
- **Say:** "Not a trigger: the markers are just positions, and the weather blends smoothly between them."

### Wind and decorations
- **Select:** Environment > Weather > **ForestWind**; any fern in Decorations > Wind Plants (`WindSway`),
  any mist in Atmosphere > Mist (`DriftingMist`), Light Rays (`LightRayPulse`), Butterflies (`Butterfly`), etc.
- **Then:** ForestWind starts a gust every few seconds that sweeps left to right; each plant asks
  `wind.GustAt(x)` and leans. `WindSway` also bends a plant away when Ryu walks through it.
  Each decoration script is a few lines in `Update()` using `Mathf.Sin` for gentle motion.

### Wildlife
- **Select:** Environment > Wildlife > Watching Fox, Leaf Frog, Lantern Moths, Stone Snail, Watchful Owl, Idle Leaf On Ryu.
- **Look at:** each has `player` (and `gameplayCamera` where needed) assigned in the Inspector.
- **Then:** each is a small state machine with an `enum State`, e.g. the fox: Watching → Noticed → Leaving → Hidden.
  When their area is off-screen they reset to their start.

### Starting house and flag
- **Select:** Environment > Decorations > **StartingHouse** (prefab `Prefabs/Entrance/Entrance House`), **EntranceFlag**.
- **Then:** the lantern and each curtain have `EntranceSpriteLoop` (cycles a few sprites, flicker for the lantern).
  The flag uses a normal **Animator** clip (`Animations/Entrance/Bamboo Grove Banner Wind`) — Lab 3.

### Level title
- **Select:** UI > LevelCanvas > **LevelTitle** (`LevelTitleFade`, CanvasGroup) with children BambooLeft, BambooRight (behind the top
  corners), LevelNumber, Title and TitleParticles (Leaf 1–3, Fleck 1–5; its CanvasGroup stays at 0 until the fade-out). Soft copies of the far background: Environment > Background > BG_Sky …
  BG_MidCanopy > `Soft Blur` (alpha 0 = sharp).
- **Starts:** the first time the player holds left/right and Ryu is actually moving (not during the tea-stop talk).
- **Then:** one coroutine fades the card in with a tiny scale-up, holds it, then fades it out while the leaves and flecks
  appear and drift away; the bamboo sways the whole time. The `Soft Blur` copies fade in and out with the card, so only the
  distant background looks slightly out of focus. Ryu keeps full control.
- **Reset:** none needed — `played` stays true through checkpoint respawns; pause freezes it (it counts Time.deltaTime).

---

## 4. Likely TA questions

**Why do you use triggers?**
Areas such as checkpoints, the river exit, the fight start and the quest areas are Box Collider 2D with *Is Trigger* ticked.
`OnTriggerEnter2D` / `OnTriggerStay2D` run when another collider overlaps, and I check `other.tag == "Player"` (Lab 5, 6, 7).
Ryu has a Rigidbody2D, which Unity needs for trigger messages.

**Why are trigger objects on the "Ignore Raycast" layer?**
So Ryu's ground check (which only looks at the Default layer) never treats a trigger area as ground.

**How does Ryu know he is on the ground?**
`Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround)` — Lab 2 p35.

**How does respawning work?**
Checkpoints store a point; on death Ryu waits `respawnDelay` seconds and `Respawn()` moves him there. Other scripts listen
to the `Respawned` message to reset (the fight, the weather, the river logs, wildlife).

**How do health and logs get from scene A to scene B?**
`RiverProgress` is a static class: static variables keep their values when a new scene loads (Lab 7 p10's static PlayerStats).

**Why not one big manager?**
Each object has its own small script, so you can select it and see what it does. Only things that really are shared
(the wind, the weather) have one script.

**Why does the player script change sprites instead of using animation clips for attacks?**
The Animator Controller belongs to the whole team. It has no attack state, and changing it would affect other levels.

**How does pause work?**
`Time.timeScale = 0`. Physics and `Time.deltaTime` stop, and Level 3 scripts check `Time.timeScale <= 0` to stop audio
and input too.

---

## 5. Advanced parts that remain (and why)

- **`EditorPauseFix`** — only in the Unity Editor. The project's shared editor tool (`_Core/Scripts/Editor/McpLocalBridge.cs`)
  sets `Time.timeScale` back to 1 every editor frame, which would undo our pause while testing. This file temporarily
  takes over that tool's update while paused. It does nothing in a built game.
- **Events (`Respawned`, `KatanaSwung`, `Defeated`, `ObjectiveChanged`, `LogCollected`)** — about ten scripts need to know
  when Ryu respawns or swings; an event lets each one react without the player script knowing about all of them.
- **`[DefaultExecutionOrder]`** on `BambooPlayerController` (-50) and `ParallaxLayer` (100): makes sure Ryu's state is
  updated before others read it, and that parallax moves after the camera.
- **`TitleFleck`** builds a mesh of small squares from the title text to make it crumble — a visual effect only.
- **Test hooks** in `BambooPlayerController` (`SimulateAttack`, `LastAttackIndex`, …) are used by the shared editor tool.

---

## 6. Hierarchy reference

### L3_A_BambooTrail
```
Systems         EventSystem, LevelMusic, QuestDirector (01–05 quest area triggers)
Camera          Main Camera
Environment     Background (BG_Sky, BG_Clouds, BG_DistantHills, BG_FarWoodland, BG_MidCanopy, BG_NearCanopy)
                Ground (Ground_Visual, Grid_Tilemap), Platforms
                Decorations (StartingHouse, EntranceFlag, Entrance Props, Wind Plants, Animated Decorations,
                             Lantern Clearing, Grove Edges, Temple Approach Props, River Approach Props,
                             Reactive Grass, Gust Leaves, Foreground Silhouettes)
                Wildlife (fox, frog, moths, snail, owl, idle leaf, Butterflies, Birds and Animals)
                Atmosphere (Mist, Light Rays, Particle Effects, River Sound)
                Weather (ForestWind, night veils, Rain/Splash/Mist/Snow particles, Zone Markers)
Player          Ryu > Player_Ryu
Enemies         BadawyFight (Badawy, FightStartTrigger, TrailGate), SwampThing
Interactables   LogCollection (Log1_ChimePuzzle … Log5_SuspendedLog), Checkpoints, RiverExit, SwampDialogueTrigger
UI              VitalsHUD, LevelCanvas (LevelTitle, Log Collection HUD, Badawy Encounter Title,
                Bamboo Quest Scroll, Log Pickup Flight, River Exit Requirement)
```

### L3_B_RiverCrossing
```
Systems         EventSystem, Audio (LevelMusic, RiverEffectsAudio, RiverCurrentAudio)
Camera          Main Camera
Environment     Background (Sky, Forest Backdrop > Waterfall, Near Bamboo), Ground (approach, exit, riverbanks, Ground Colliders), Water,
                Decorations (River Props, Animated Decorations, Foreground), Atmosphere
Player          Ryu > Player_Ryu
Interactables   RiverCrossing (LogPlacementPoint, WaterEffects, Floating Log 1–6), Checkpoints (NearBank, FarBank)
UI              VitalsHUD, RiverCanvas (Log Counter Backing, River Feedback, Place Logs Prompt)
```
