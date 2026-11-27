# Level 3 audio sources and credits

## CC0 recordings from OpenGameArt (rubberduck)
All of these packs are released under **CC0 1.0 (public domain)**. No credit is required; it is given here anyway.
Author: rubberduck, opengameart.org (all packs: https://opengameart.org/content/cc0-audio-uploader-rubberduck). Packs used:
- 80 CC0 creature SFX: https://opengameart.org/content/80-cc0-creature-sfx
- 80 CC0 RPG SFX: https://opengameart.org/content/80-cc0-rpg-sfx
- 100 CC0 metal and wood SFX: https://opengameart.org/node/85875
- 75 CC0 breaking / falling / hit sfx: https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx
- 25 CC0 mud sfx: https://opengameart.org/node/87193
- 40 CC0 water / splash / slime SFX (water-splash-slime-sfx.zip): listed on the rubberduck page above

The original clips were trimmed, slightly lowered in pitch where noted, layered and level-matched (peaks between -7 and -12 dBFS, so effects stay under dialogue).

| File | Made from (pack file names) | Used by | Plays when |
| --- | --- | --- | --- |
| Swamp Thing/Swamp Entrance Growl | creature monster_04 (lowered) + wood_squeak_01 | SwampThing.wakeSound | he rises after his entrance lines |
| Swamp Thing/Swamp Arm Stretch 1–2 | wood_squeak_02 / wood_misc_07 + RPG blade_01 / blade_03 (lowered) | SwampThing.armSounds | the arm shoots out (ArmStrike) |
| Swamp Thing/Swamp Jump 1–2 | creature grunt_02 / troll_01 + wood_cracking_04 / 02 | SwampThing.jumpSounds | he leaves the ground (SlamJump) |
| Swamp Thing/Swamp Slam Earth | bfh1_hit_02 + bfh1_rock_falling_02 + mud_11 | SwampThing.landSounds[0] | slam landing |
| Swamp Thing/Swamp Dig 1–2 | mud_03 + mud_18 / mud_04 + bfh1_rock_falling_08 | SwampThing.soilSounds | burrowing, then moving under the soil |
| Swamp Thing/Swamp Emerge Burst | bfh1_rock_breaking_01 + wood_breaking_01 + mud_12 | SwampThing.emergeSound | he bursts out of the ground |
| Swamp Thing/Swamp Hurt Crack 1–2 | wood_hit_09 + creature troll_03 / bfh1_wood_hit_02 + creature hurt_04 | SwampThing.hitSounds | katana hits him |
| Swamp Thing/Swamp Defeat Collapse | RPG creature_die_01 + wood_breaking_02 + bfh1_wood_falling_01 | SwampThing.deathSound | defeat |
| SFX/Log Place | wood_slam_04 + wood_hit_08 | RiverCrossing.placeClip | the six logs drop into place (played once) |
| SFX/Log Crack, Log Crack 2 | wood_cracking_04 / wood_cracking_02 + 01 | RiverCrossing.crackClips | a river log starts to crack |
| SFX/Log Break | wood_breaking_01 + bfh1_wood_breaking_03 | RiverCrossing.breakClip | a river log breaks |
| SFX/River Splash 2, 3 | splash_02 / splash_10 | RiverCrossing.splashClips (with River Splash) | log breaks or Ryu falls in |
| SFX/Ground Collapse | bfh1_rock_breaking_01 + bfh1_rock_falling_01 + 03 | CrackedGround.collapseClip | cave ground gives way |
| SFX/Bear Wake Growl | creature roar_02 (lowered) | SleepingBear.warningClip | bear wakes |
| SFX/Bear Charge Roar | RPG creature_roar_03 (lowered) | SleepingBear.chargeClip | bear charges |
| SFX/Torch On | RPG spell_fire_07 | PlayerTorch.onClip | pressing F (torch lights) |
| SFX/Eye Warn | RPG spell_02 | Gloom Eye prefabs, ForestEnemy.attackSound | a Gloom Eye warns before diving |
| SFX/Dialogue Open | RPG book_02 (one page turn, trimmed) | TeaRestConversation / SwampDialogue .openClip | a conversation box opens (quiet) |
| SFX/Dialogue Line | RPG book_04 (short page sound, trimmed) | TeaRestConversation / SwampDialogue .lineClip | a new dialogue line starts (quiet) |
| SFX/Lantern Light | RPG spell_fire_02 (lowered, trimmed) | ForestCheckpoint.lightClip | a checkpoint lantern lights (once) |
| SFX/Seesaw Tip | wood_slam_02 + RPG stones_02 + wood_squeak_01 | LogSeesaw.tipClip | the pushed rock tips the board (once) |
| SFX/Trail Gate Open | wood_squeak_02 + wood_slam_03 | BadawyEncounter.gateOpenClip | the trail gate opens after Badawy's fight |

## ElevenLabs sound effects
Generated with ElevenLabs text to sound effects (model eleven_text_to_sound_v3).
Use follows the ElevenLabs terms of service for the account that generated them; credit line if required: "Sound effects generated with ElevenLabs."

| File | Used by | Plays when |
| --- | --- | --- |
| SFX/Box Open | ChimePuzzle.chestOpenSound | first puzzle box opens: soft wooden creak and latch, played once |
| Swamp Thing/Swamp Land 2 | SwampThing.landSounds[1] | slam landing (takes turns with Swamp Slam Earth) |

## Sounds made for this project (no outside licence)
Made by a Python script (numpy/scipy) from noise, sine waves and filters. No recordings or third-party samples.

| File | Used by | Plays when |
| --- | --- | --- |
| Katana Swing 1–3 | BambooPlayerController.swingSounds | each katana swing |
| Hit Body 1–2 | ForestEnemy.hitSounds (Badawy) | katana hits Badawy |
| Chime Sun / Moon / Leaf | each chime's AudioSource | ringing that chime |
| Puzzle Wrong | ChimePuzzle.wrongSound, RiverCrossing.notReadyClip | wrong chime order; E at the river cradle without all six logs |
| Torch Pickup / Torch Off | TorchPickup.pickupClip / PlayerTorch.offClip | taking the torch / putting it out |
| UI Click | ForestHudButton, DialogueSkipButton | pause, mute and Skip buttons |

## Reused existing project sounds
Forest Pickup (log pickup), Writing Tick (quest scroll ticks, and the quiet dialogue typing tick every 4th letter), Forest Cage Break (Swamp Thing's cage), River Splash, Forest Player Hurt,
Swamp Warning and Swamp Pull Free (Swamp Thing's warning and arm pull-back).
Badawy still uses the older Swamp Emerge / Swamp Hurt / Swamp Slam clips and the Gloom Eyes use Swamp Hurt / Swamp Death;
Swamp Thing itself no longer uses those clips.

## Hallucination voice (Assets/Level3_Bamboo/Audio/Hallucinations/Boy Calling Otousan (JA).wav)
「お父さん……どこにいるの？」 (Otōsan… doko ni iru no?) — a frightened fictional boy.
Generated with ElevenLabs text to speech (model eleven_v4) using the ElevenLabs voice library voice
"chinyaku – Cute Japanese Voice Actress" (an adult voice actress who also voices boyish characters).
No real child's voice was cloned. Use follows the ElevenLabs terms of service for the account that generated it;
credit line if required: "Voice generated with ElevenLabs."
