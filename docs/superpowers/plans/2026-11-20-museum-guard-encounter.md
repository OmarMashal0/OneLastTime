# Museum Guard Encounter Implementation Plan

## Overview
Implement the museum guard encounter in Level 1 (`L1_A_MuseumStealth.unity`) inside the museum interior corridor. The encounter includes a patrolling guard with smooth idle/walk animations, a procedural vision cone with dynamic obstacle line-of-sight clipping, notice-aim-shoot state progression, physical bullet projectile with trail, solid cover hiding detection with monuments, Ryu 10-frame death animation integration, and a museum checkpoint respawn system.

## Task Breakdown

### Task 1: Guard Sprite Asset Preparation & Slicing
- Copy and import the guard master sheet to `Assets/Level1_Museum/Art/Feudal_Japan_Interior/Color_1/Props/guard_sheet.png`.
- Configure TextureImporter (Point filter, 16 PPU, Multiple sprites, preserve transparency).
- Slice into 30 frames (6 cols × 5 rows) with aligned pivot points at feet baseline.
- Slice Ryu's `Assets/_Core/Art/Characters/Ryu/DEATH.png` into 10 frames (32×32, 16 PPU, Point filter, pivot at bottom-center / feet).

### Task 2: Animation Clips & Animator Controllers
- Create Guard animation clips:
  - `guard_idle.anim` (Row 0, 6 frames, loop)
  - `guard_walk.anim` (Row 1, 6 frames, loop)
  - `guard_notice.anim` (Row 2, 6 frames, clamp)
  - `guard_aim.anim` (Row 3, 6 frames, clamp / hold pose)
  - `guard_shoot.anim` (Row 4, 6 frames, clamp / recovery)
- Build `guard.controller` state machine.
- Create Ryu `ryu_death.anim` (10 frames at 10 FPS) and hook it into `player_ryu.controller` via trigger `die` to an un-interruptible Death state.

### Task 3: Vision Cone & Detection System (`GuardVisionCone.cs`)
- Implement procedural 2D triangle-fan mesh generated in front of the guard.
- Raycast against walls / obstacles so the cone clips naturally to geometry.
- Check line of sight from guard to player.
- Query `StealthCover.IsPlayerHidden` so Ryu hidden behind monuments is completely undetectable.
- Expose distance, angle, alert colors, and sorting layer.

### Task 4: Bullet Projectile (`GuardBullet.cs`) & Muzzle Setup
- Create `GuardBullet.prefab` with `Rigidbody2D` (Continuous collision detection), `CircleCollider2D`, small pixel sprite, and `TrailRenderer`.
- Implement bullet script with direction lock, high speed, swept continuous collision, wall/monument absorption, and player damage trigger.
- Add muzzle attachment point to guard sprite, mirrored with scale flips.

### Task 5: Museum Guard AI Controller (`MuseumGuard.cs`)
- Implement patrol between configurable `Point A` and `Point B`.
- 1-second pause at each endpoint playing Idle before turning.
- State progression: `Patrol` -> (detection) -> `Notice` (0.2s) -> `Aim` (0.5s) -> `Shoot` (fires bullet).
- Pre-fire check: If Ryu ducks into cover before shot, cancel and return to Alert/Patrol.
- Provide `ResetGuard()` method for respawn synchronization.

### Task 6: Ryu Death & Checkpoint Respawn System (`PlayerHealth.cs` & `CheckpointManager.cs`)
- Implement `PlayerHealth.cs` handling bullet hit, clearing velocity, disabling player movement, playing death animation, and holding final frame.
- Implement `Checkpoint.cs` and `CheckpointManager.cs` positioned at museum entrance (`-3.84, -22.84`).
- On respawn: clear bullets, reset guards via `ResetGuard()`, restore Ryu health & controls.

### Task 7: Prefab Assembly, Scene Integration & Verification
- Assemble `Museum_Guard.prefab` in `Assets/_Core/Prefabs/Characters/`.
- Place a guard in the museum corridor with patrol endpoints (`x ≈ 4.0` to `x ≈ 18.0`).
- Place checkpoint just inside museum entrance.
- Test patrol, notice, aim, shoot, bullet collision, hiding behind monument, death, and respawn.

