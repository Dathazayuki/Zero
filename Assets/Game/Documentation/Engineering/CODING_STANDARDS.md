# Zero Engineering Standard

This document is the source of truth for code under `Assets/Game`. Keywords **MUST**,
**MUST NOT**, **SHOULD**, and **MAY** are normative. Identifiers and log codes use
English. Explanations may use Vietnamese.

## 1. Architecture and ownership

The project is a long-lived small game. Use the smallest architecture that preserves
clear ownership:

```text
Scene/Bootstrap -> MonoBehaviour adapter -> pure C# gameplay rule <- authored config
                                         -> presentation through events/read-only state
```

- A `MonoBehaviour` MUST bridge Unity lifecycle, scene references, and presentation. A
  gameplay rule that can run without a `GameObject` MUST be a plain C# type.
- A `ScriptableObject` MUST contain authored configuration or shared immutable data. It
  MUST NOT own per-run mutable state.
- Every runtime value MUST have one writer. Other systems receive a read-only property,
  a narrow query, or an event.
- Direct references are preferred for one-to-one collaboration. C# events are for
  one-to-many notification. Interfaces are introduced only for multiple implementations
  or a real test/dependency seam.
- New global event buses, service locators, and singletons are forbidden.
  `GameFlowController.Instance` is a controlled legacy exception.
- A state with mutually exclusive modes MUST use an enum/state machine and one guarded
  transition function. A collection of overlapping state booleans is forbidden.
- Managers MUST NOT select concrete anomaly implementations by type, asset name, or a
  growing switch. New anomaly content extends `AnomalyBehaviour` and existing contracts.

## 2. SOLID without framework inflation

- A class MUST have one clear reason to change. Split orchestration, rules, persistence,
  and presentation when they change for different reasons.
- Derived behaviours MUST preserve base lifecycle semantics. Activate/deactivate/reset
  pairs are symmetric and reset is idempotent.
- An interface SHOULD contain no more than five cohesive members.
- High-level flow depends on capabilities or events where concrete dependencies would
  invert the module direction.
- Do not create a factory, generic abstraction, wrapper, or interface without a current
  consumer or test seam. Readable duplication is preferable to a premature framework.

## 3. Enforced size limits

For new files and debt-free files:

| Rule | Limit |
|---|---:|
| Public top-level types per file | 1 |
| Logical lines per class/file | 300 |
| Logical lines per method | 40 |
| Parameters per method | 5 |
| Control-flow nesting | 3 |
| Branch points per method | 10 |
| Serialized `UnityEngine.Object` dependencies | 7 |

Small related enums and DTOs may share a file. Partial classes MUST NOT be used to hide
a god object. Existing debt is recorded in `CodePolicyBaseline.json`; it may not increase.

## 4. Data and public API

- Inspector data is `[SerializeField] private`. Runtime state is private and exposed only
  as a read-only property or event when necessary.
- Mutable public fields are forbidden.
- Authoring values MUST NOT be overwritten as runtime state.
- Animator parameters and state names used repeatedly MUST be cached as hashes/constants.
- Serialized field renames require `[FormerlySerializedAs]`. Removing or changing a
  serialized type requires an explicit migration and prefab/scene validation.
- Legacy public APIs used by assets or Editor tools remain as adapters until their callers
  are migrated.

## 5. Unity lifecycle

- `Awake`: cache local components and validate composition.
- `OnEnable`: subscribe and start repeatable lifecycle work.
- `OnDisable`: unsubscribe, stop owned coroutines/async work/audio/agents, and clear
  presentation overrides.
- `Start`: only for dependencies that must finish `Awake` first.
- Every per-frame callback opens with cheap readiness and state guards.
- Do not rely on incidental `Awake` order. Only a bootstrap/load-bearing service may use
  `DefaultExecutionOrder`; more than a few ordered scripts indicates an architecture bug.
- A coroutine has one owner, a duplicate-start gate, and a cleanup path.
- `async void` is forbidden except a required Unity callback. Async work has cancellation
  or a lifetime/version token.
- Reset is safe when called twice and leaves no stale callback, timer, agent, audio, or UI.

## 6. Hot-path performance

Inside `Update`, `LateUpdate`, and `FixedUpdate`, code MUST NOT use:

- `Find*`, `GameObject.Find`, or `Resources.Load`;
- repeated `GetComponent*`;
- LINQ or reflection;
- per-frame material, mesh, texture, render texture, audio clip, collection, or delegate
  allocation;
- unthrottled logging.

Composition-time lookup is allowed only once and the result is cached. Gameplay random
that affects tests MUST accept a seed or injectable random source.

## 7. Diagnostics and failure behaviour

Stable log format:

```text
<SYSTEM>_<EVENT>
<SYSTEM>_CONFIGURATION_INVALID: field=..., object=...
<SYSTEM>_STATE_REJECTED: from=..., request=...
```

- Pass a Unity context object to the log when available.
- Never swallow an exception or repeatedly retry a rejected action each frame.
- Invalid configuration fails safe and MUST NOT trap the Player.
- Trigger/collider callbacks are consumed or gated before invoking downstream work.
- Development-only logs use `UNITY_EDITOR || DEVELOPMENT_BUILD`.

## 8. Editor and asset safety

- `[InitializeOnLoad]`, automatic scene setup, `EditorApplication.update`, and
  `EditorApplication.delayCall` are forbidden for setup/rebuild/save work.
- Setup tools execute only from an explicit `[MenuItem]`, validate first, record Undo,
  and never use `Apply All`.
- Tools MUST NOT automatically save scenes or prefabs.
- Runtime assemblies MUST NOT depend on Editor assemblies.
- Scene and prefab hierarchy names are contracts. Changing them requires a migration and
  validation task.

## 9. Tests and Definition of Done

- Pure rules/state machines: EditMode tests.
- Triggers, colliders, coroutine lifecycle, reset, and scene contracts: PlayMode tests.
- Every bug fix adds a regression test at the lowest practical level.
- Random tests use a fixed seed; timing assertions use ranges rather than an exact frame.
- Meaningful flows test duplicate callbacks, reset during work, disable cleanup, and
  missing-reference fail-safe behaviour.

A change is complete only when it compiles without new warnings, policy validation passes,
relevant tests pass, no runtime exception is introduced, and serialized contracts remain
valid.

## 10. Policy exceptions

An exception is recorded only in `CodePolicyBaseline.json` with a rule ID, owner, reason,
and removal plan. New files cannot add baseline debt. Local suppressions used only to bypass
policy are forbidden. When debt is reduced, the baseline ceiling is reduced with it.
