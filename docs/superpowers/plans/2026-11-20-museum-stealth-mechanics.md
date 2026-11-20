# Museum Stealth Mechanics & Level Encounters Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full stealth mechanics for the Museum level: Quiet Walk (sneak with `WALK.png`), Sleeping Guard (woken by running footsteps, bypassed by quiet walking), Wall Jump mechanics with a heavy guard blockade bypassed via ceiling rafters, and a Guarded Door trap requiring monument hiding (`StealthCover`) to safely open.

**Architecture:** 
1. `PlayerMovement.cs` receives Quiet Walk toggle/hold (`KeyCode.LeftShift`), wall jump detection, and a noise broadcast mechanism (`PlayerNoiseEmitter`).
2. `player_ryu.controller` and `ryu_walk.anim` integrate the 12-frame `WALK.png` walk animation.
3. `MuseumGuard.cs` is upgraded to support `IsSleeping` behavior (hearing radius checking for running footsteps, snore/Zzz visual prompt, instant wake-and-shoot if disturbed).
4. `GuardedDoor.cs` watches player interaction: if Ryu opens the door while in the guard's field of view, the guard immediately snipes Ryu.
5. In `L1_A_MuseumStealth.unity`, arrange the three consecutive gameplay sections:
   - Section 1: Sleeping Guard sneak challenge (rewarding Quiet Walk).
   - Section 2: Ground guard blockade with vertical pillars for Wall Jump up to wooden ceiling rafters.
   - Section 3: Guarded exit door with monument cover (`StealthCover`).

**Tech Stack:** Unity 2D, C# (Unity Editor Roslyn/CodeDom), Unity MCP server, 2D Rigidbody/Colliders, Sprites & Animator Controllers.

---

## Global Constraints

- Use exact sprite pivot `(0.48, 0.33)` for `WALK.png` matching `IDLE` and `RUN` baseline.
- Raccoon distraction is completely removed; stealth is player-action driven (Quiet Walk + StealthCover hiding + Wall Jump).
- Bullet speed remains fast (`38 units/sec`) with instantaneous lethal response if detected.
- Never modify unrelated branches; keep changes confined to `level-1` and Museum level assets.

---

## Tasks

### Task 1: Quiet Walk Animation & Controller Setup
**Files:**
- Create: `Assets/_Core/Art/Characters/Ryu/Animations/ryu_walk.anim`
- Modify: `Assets/_Core/Art/Characters/Ryu/Animations/player_ryu.controller`
- Reference: `Assets/_Core/Art/Characters/Ryu/WALK.png`

- [x] **Step 1:** Create `ryu_walk.anim` with 12 frames from `Assets/_Core/Art/Characters/Ryu/WALK.png` (`WALK_0` .. `WALK_11`), sample rate 12 FPS, loop enabled.
- [x] **Step 2:** Add `isWalking` (bool) parameter to `player_ryu.controller`.
- [x] **Step 3:** Add `Walk` state to `player_ryu.controller` with transitions:
  - From `Idle` to `Walk` when `isWalking == true` and `moveSpeed > 0.1`
  - From `Walk` to `Idle` when `moveSpeed <= 0.1`
  - From `Walk` to `Run` when `isWalking == false` and `moveSpeed > 0.1`
  - From `Run` to `Walk` when `isWalking == true`
- [x] **Step 4:** Verify in Unity Editor that animator transitions cleanly between Idle, Walk, and Run.

---

### Task 2: Player Quiet Walk & Noise Mechanics in PlayerMovement
**Files:**
- Modify: `Assets/_Core/Scripts/Player/PlayerMovement.cs`

- [x] **Step 1:** Add `isSneaking` state, `sneakSpeed = 3.2f`, and input handling (`KeyCode.LeftShift` or `KeyCode.LeftControl`).
- [x] **Step 2:** Update animator parameters (`isWalking = isSneaking && moveInput != 0`).
- [x] **Step 3:** Expose public property `public bool IsMakingFootstepNoise => grounded && Mathf.Abs(rb.velocity.x) > 0.5f && !isSneaking;`.
- [x] **Step 4:** Test in Unity Editor: Player moves at sneak speed (3.2f) and plays walk animation when holding Shift, runs at 7.5f when releasing Shift.

---

### Task 3: Wall Jump Mechanics in PlayerMovement
**Files:**
- Modify: `Assets/_Core/Scripts/Player/PlayerMovement.cs`

- [x] **Step 1:** Add wall check raycasts/capsule checks left and right (`wallCheckDistance = 0.35f`, `whatIsWall` / `whatIsGround`).
- [x] **Step 2:** Add wall slide behavior (clamped downward velocity e.g. -2.5f when pressing into a wall while falling).
- [x] **Step 3:** Add wall jump kick-off force: `wallJumpForce = new Vector2(9f, 13f)` with a brief horizontal input lock (`0.15s`) to prevent sticking.
- [x] **Step 4:** Verify wall jump allows fluid leaping back and forth between two parallel vertical colliders.

---

### Task 4: Sleeping Guard Component & Snore FX
**Files:**
- Modify: `Assets/_Core/Scripts/Interaction/MuseumGuard.cs`
- Create: `Assets/_Core/Prefabs/Characters/Museum_Guard_Sleeping.prefab`

- [x] **Step 1:** Add `isSleeping` mode and `hearingRadius = 5.5f` to `MuseumGuard.cs`.
- [x] **Step 2:** When sleeping:
  - Guard animation plays sleeping frame or slow breathing idle with closed eyes.
  - Vision cone is disabled or collapsed to 0 length.
  - Floating "Zzz" prompt / text icon above guard's head.
- [x] **Step 3:** In `Update()` while sleeping:
  - Check if player is within `hearingRadius` and `playerMovement.IsMakingFootstepNoise`.
  - If heard, guard immediately enters `AlertNotice` with a startled exclamation mark, activates vision cone (red), and shoots!
  - If player is sneaking (`isSneaking == true`), guard remains sound asleep.
- [x] **Step 4:** Test sleeping guard waking up from running footsteps vs remaining asleep when sneaking past.

---

### Task 5: Heavy Guard Blockade & Wall-Jump High Route
**Files:**
- Modify: `Assets/Level1_Museum/Scenes/L1_A_MuseumStealth.unity`

- [x] **Step 1:** Place a ground blockade of 2-3 alert guards with overlapping sightlines at the middle section (e.g. `X = 20..30`) making ground passage impossible.
- [x] **Step 2:** Construct two vertical wall-jump pillars (`ground` layer) before the blockade (`X ≈ 18..20`).
- [x] **Step 3:** Create ceiling rafter platforms (`Y ≈ -17.5`, one-way / top-edge colliders) spanning across the ceiling beams over the guards.
- [x] **Step 4:** Add a safe dropdown platform at `X ≈ 32` past the ground blockade.
- [x] **Step 5:** Verify player can wall-jump up the shaft, walk across the ceiling beams over the guards' heads, and drop down safely behind them.

---

### Task 6: Guarded Door Trap & Monument Hiding
**Files:**
- Create: `Assets/_Core/Scripts/Interaction/GuardedDoor.cs`
- Modify: `Assets/Level1_Museum/Scenes/L1_A_MuseumStealth.unity`

- [x] **Step 1:** Implement `GuardedDoor.cs` on the exit door:
  - Watches for player trying to interact/open.
  - Checks if the door-watching guard is currently facing the door / has line of sight to the door.
  - If player tries to open while guard is watching: guard instantly triggers `ForceShootPlayer()` and kills Ryu.
- [x] **Step 2:** Setup the watching guard with a timed patrol / glance cycle:
  - Guard looks towards the door for 3.5s, then turns around to inspect artifacts behind him for 3.0s.
- [x] **Step 3:** Place a monument hiding spot (`StealthCover`) close to the door.
  - Player hides in the monument (`E` to hide).
  - When guard turns his back, player exits cover, quietly slips to the door, and opens it safely.
- [x] **Step 4:** Verify trying to rush the door gets the player shot, while using the monument hiding spot allows safe passage.

---

### Task 7: Full Sequence End-to-End Verification
**Files:**
- Test: `Assets/Level1_Museum/Scenes/L1_A_MuseumStealth.unity`

- [x] **Step 1:** Test Section 1: Sneak past the Sleeping Guard using Quiet Walk (Shift).
- [x] **Step 2:** Test Section 2: Wall Jump up to ceiling rafters and cross over the 3-guard heavy blockade.
- [x] **Step 3:** Test Section 3: Hide behind the monument, wait for the door-guard to turn away, and open the exit door.
- [x] **Step 4:** Verify respawn at checkpoint if killed anywhere along the chain.
