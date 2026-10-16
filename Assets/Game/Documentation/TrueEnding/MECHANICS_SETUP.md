# Mechanics setup

## Shared reset system

Create one `ChallengeResetController` in the scene. Each challenge owns a reset Transform (`r1_spawn` or `r2_spawn`). On failure:

1. Temporarily disable player movement/look input.
2. Fade to black in ~0.15 s.
3. Teleport the player to the room reset Transform.
4. Reset player velocity.
5. Restore challenge state.
6. Fade in in ~0.25 s.
7. Re-enable input.

Avoid death screens or loading screens; the reset should feel like the room silently corrected reality.

## Room 1 — Don't Look Back

### Rule
After crossing `R1_ACTIVATE`, the player must continue toward +X. Looking backward inside `R1_DONT_LOOK_BACK` resets the room.

### Recommended detection
Use the camera forward vector, flattened to the XZ plane. Compare it with world forward `(1,0,0)` using `Vector3.Dot`.

- Normal forward look: dot near `+1`.
- Sideways glance: dot near `0` and should be allowed.
- Looking backward: dot becomes negative.
- Recommended fail threshold: `< -0.35` continuously for `0.12 s`.

This avoids punishing a tiny mouse twitch while still making a deliberate look behind fail.

### Horror feedback
Do not display UI. Use audio behind the player: one footstep, breathing, cloth movement, or a soft clap. The player should be tempted to turn around but never required to interact.

### Completion
Crossing `R1_COMPLETE` disables Room 1 detection and marks the challenge complete.

---

## Room 2 — Move only while lights are OFF

### Rule
- OFF: movement allowed.
- WARNING flicker: player should prepare to stop.
- ON: movement is forbidden.

Recommended cycle from JSON:

`OFF 2.4s -> WARNING 0.35s -> ON 1.6s -> WARNING 0.25s -> repeat`

### Movement detection
Use horizontal player displacement rather than raw input so the rule remains correct even if the controller changes.

When the state enters ON, allow ~0.18 s grace time. After the grace period, if horizontal displacement in a frame exceeds ~0.035 m, reset Room 2.

Ignore tiny CharacterController settling and camera rotation. Looking around while standing still is allowed.

### Light setup
All `r2_light_*` objects are driven by one controller so they switch together. During WARNING, flicker them at roughly 8 Hz. Audio can include a ballast click or electrical hum change so the rule remains readable even if the player is not staring at a lamp.

### Completion
Crossing `R2_COMPLETE` stops the cycle and leaves the lights in the desired final state before the Blue Sky connector.

---

## Blue Sky ending

Crossing `BLUESKY_ENTER` should gradually remove Backrooms presentation:

- Fade fluorescent hum out.
- Fade wind / open-space ambience in.
- Blend to clean blue-sky lighting and post-processing.
- Do not reset the player anymore.

At `BLUESKY_ENDING`:

1. Disable player movement and look.
2. Snap or smoothly align player to `ENDING_PLAYER_MARK`.
3. Blend to `ENDING_CAMERA` over ~1.2 s.
4. Turn character toward the camera.
5. Trigger animator parameter `Bow`.
6. Hold for ~2 s after the bow.
7. Fade to white over ~1.5 s.
8. Register `ENDING_B_CURTAIN_CALL` and load credits/end screen.

The visual should evoke a theatrical farewell without copying a specific film shot exactly.
