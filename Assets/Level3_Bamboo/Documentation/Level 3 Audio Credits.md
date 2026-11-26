# Level 3 audio sources and credits

## New sound effects (Assets/Level3_Bamboo/Audio/SFX)
Made for this project by a Python script (numpy/scipy) that builds each sound from noise, sine waves and filters.
No recordings or third-party samples were used, so there is no outside licence and no credit is required.

| File | Used by | Plays when |
| --- | --- | --- |
| Katana Swing 1–3 | BambooPlayerController.swingSounds | each katana swing (random, never the same twice in a row) |
| Hit Wood 1–2 | SwampThing.hitSounds | katana hits Swamp Thing (alternates) |
| Hit Body 1–2 | ForestEnemy.hitSounds (Badawy) | katana hits Badawy (alternates) |
| Chime Sun / Moon / Leaf | each chime's AudioSource | ringing that chime |
| Puzzle Wrong | ChimePuzzle.wrongSound | wrong chime order |
| Box Open | ChimePuzzle.chestOpenSound | shrine box opens |
| Ground Collapse | CrackedGround.collapseClip | cracked ground gives way |
| Torch Pickup | TorchPickup.pickupClip | taking the cave torch |
| Torch On / Torch Off | PlayerTorch.onClip / offClip | pressing F |
| Bear Wake Growl / Bear Charge Roar | SleepingBear.warningClip / chargeClip | bear wakes / starts charging |
| Log Crack | RiverCrossing.crackClip | a river log starts to crack |
| UI Click | ForestHudButton.clickSound | pause and mute buttons |

## Reused existing sounds
Forest Pickup (log pickup), Forest Cage Break (cage, river log break, ground crack), River Splash,
Swamp Thing set (wake, warning, swing, slam/landing, pull free, soil/burrow, emerge, hurt, death).

## Hallucination voice (Assets/Level3_Bamboo/Audio/Hallucinations/Boy Calling Otousan (JA).wav)
「お父さん……どこにいるの？」 (Otōsan… doko ni iru no?) — a frightened fictional boy.
Generated with ElevenLabs text to speech (model eleven_v4) using the ElevenLabs voice library voice
"chinyaku – Cute Japanese Voice Actress" (an adult voice actress who also voices boyish characters).
No real child's voice was cloned. Use follows the ElevenLabs terms of service for the account that generated it;
credit line if required: "Voice generated with ElevenLabs."
