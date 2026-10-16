# Audio setup and audit

The mixer is `Assets/Game/Audio/MX_ZeroGame.mixer`. Every audible source must use
its `Master/Music`, `Master/Ambience`, or `Master/SFX` output group. The catalog is
`Assets/Game/Resources/Audio/GameSoundCatalog.asset`.

## Authoring a sound

1. Add the clip to the project. Add a semantic entry to the catalog (for example,
   `monster.footstep`) and assign its clip, bus, volume, pitch, loop, and distance.
   The generated `clip.*` entries are inventory only, not reliable bus assignments.
2. For an object that emits sound, add `AudioEmitter`, set its sound ID, and call
   `Play()` from the gameplay action. An `AudioEmitter` follows its object and stops
   its owned loop when disabled. It also binds when spawned after scene load.
3. For an existing scripted `AudioSource`, keep the clip reference in the Inspector.
   `SourceSoundPlayback` uses that authored clip, even if a catalog ID also exists.
   Set the source's Output to the correct mixer group for any Play On Awake sound.
4. World sounds (doors, monsters, anomaly objects) should be 3D. Start with
   Spatial Blend `1`, Min Distance `1 m`, Max Distance `10–20 m`, and Linear Rolloff.
   Tune Max Distance and volume while listening from the farthest accessible room.
   UI, player breathing/footsteps, and cinematic voice may remain 2D.
5. Run `Tools > Zero > Audio > Audit Loaded Sources` in each relevant loaded scene.
   Review every `AUDIO_UNROUTED` message; assign its bus explicitly. The audit does
   not modify or save the scene.

## Troubleshooting

- No sound: inspect `AUDIO_ID_INVALID`, `AUDIO_CLIP_MISSING`, and
  `AUDIO_POOL_EXHAUSTED`; check the clip, active source, listener, mixer group, and
  whether the gameplay action actually calls `Play()`.
- Wrong sound: inspect `AUDIO_CLIP_MISMATCH` and check the source's authored clip
  first; generated `clip.*` IDs must not be used as semantic event IDs.
- Far sound too loud: check that its source is 3D, that Max Distance is not larger
  than intended, and that it is positioned on the emitting object rather than the
  camera or a persistent manager.
- Settings slider seems ineffective: check Output group and ensure the clip is not
  also playing from a separate, unrouted Play On Awake source.

Unity scene/prefab references must be configured manually or through an explicit,
Undo-aware Editor operation. No scene is rebuilt or saved automatically.

Current scene audit note: `InfiniteStairwayScene/GlobalAmbience` has Play On Awake
enabled but no AudioClip and no Output group in its serialized AudioSource. Assign
the intended ambience clip and `MX_ZeroGame/Ambience` in the Inspector, or disable
Play On Awake if a controller is intended to start it later.
