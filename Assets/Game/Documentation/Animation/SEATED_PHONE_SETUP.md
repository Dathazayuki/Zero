# Seated phone animation

`Assets/Game/Animation/SeatedPhoneScroll.anim` is a six-second looping Humanoid clip authored from the Player rig in Office. The body stays seated, the head looks down, the left hand holds a phone and its thumb makes two scrolling gestures per loop. The right hand rests on the right thigh; it does not touch the phone.

The `.anim` asset is standalone: playback requires only an Animator/controller, not the Editor authoring tool. Finger curves use Unity's `LeftHand.Thumb.*` bindings, not the differently named `HumanTrait.MuscleName` labels. A small wrist motion accompanies the thumb. The resting hand is positioned relative to the thigh bones and faces palm-down. In the Office preview, the Samsung phone is attached to the left hand and `Phone_Prototype` is inactive.

## Preview in Office

The temporary scene objects `SeatedPhone_Test` and `SeatedPhone_PreviewCamera` are outside the Office geometry at X = -10 for unobstructed inspection. The preview camera is disabled so it does not take over the gameplay camera. Select `SeatedPhone_Test`, press F in Scene View, and inspect the Animator or select `SeatedPhoneScroll.anim` in the Animation window. In Play Mode its dedicated controller plays the loop automatically.

Scene edits are unsaved. Save manually with Ctrl+S if you want to retain the preview. The original Player still uses `Player.controller`.

## Use on another character

1. Use a valid Humanoid Avatar. Add `SeatedPhoneScroll.anim` as a state in that character's Animator Controller.
2. Turn off Apply Root Motion; movement and placement remain owned by the scene/gameplay controller.
3. Place the character's root on the floor and adjust the chair to the seated hips. The clip contains the seated body height.
4. Attach a phone to the left-hand bone. Adjust local position/rotation to that character's hand size; the demo phone is a simple placeholder.
5. A gameplay Player needs the existing `PlayerLocomotionAnimator.TryPlayPresentationState` override so Idle/Walk/Run do not overwrite this pose. Call `ClearPresentationOverride` when the presentation ends.

The clip is a procedural authored prototype, not motion capture. Hand contact and seat height require adjustment on characters with different proportions.

## Checks performed

- One-handed correction: neutralize the temporary rig before baking, keep both arms stationary, animate only the left thumb, and reposition the preview phone. Close-up: [SeatedPhone_OneHand_Close.png](SeatedPhone_OneHand_Close.png).
- `SeatedPhoneAnimationTests`: two EditMode tests passed after correcting the bindings, covering loop closure, valid finger bindings, thumb movement on the real Player Avatar and a stationary resting hand. Final grip/hand-height adjustments were subsequently inspected in Play Mode; the live Animator changed the thumb bone rotation between normalized times 0 and 0.125. Save OfficeLoopScene manually before rerunning the Test Runner after scene attachment edits.
- Unity compilation completed with zero errors and zero warnings for the new Editor tool.
- Sampled the clip at 0 and 0.75 seconds and inspected screenshots on the Office Player's actual Humanoid rig.
- Entered Play Mode and inspected the dedicated preview Animator: correct controller, Apply Root Motion off, Always Animate; no non-policy Console errors were reported during the check. Returned to Edit Mode.
- The clip is approximately 197 KB after reducing constant curves. Preview: [SeatedPhone_PlayMode.png](SeatedPhone_PlayMode.png).
- `Validate Changed Code` currently reports 13 pre-existing runtime violations in unrelated files (for example FlickeringLightController, PlayerDeathController and BackroomMonsterController). Project-wide policy validation is not passing; these files were not changed by this animation task.

## Explicit authoring tools

Select a Humanoid model, then use `Tools > Zero > Animation > Create Seated Phone Clip` to regenerate this demo clip. This updates only `SeatedPhoneScroll.anim`, preserving its GUID. `Create Seated Phone Preview` makes a separate model/controller/camera for inspection. `Refresh Seated Phone Preview` updates an existing selected preview and adjusts its phone grip, without recreating scene objects. `Preview Phone Swipe` samples the selected model at 0.75 seconds. Tools execute only on menu invocation and do not save scenes or prefabs.
