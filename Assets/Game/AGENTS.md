# Agent Instructions

## Skills

Before starting any task:

1. Inspect the available skills in `.agents/skills/`.
2. Determine which skill or skills are relevant to the user's request.
3. Read the relevant `SKILL.md` files before implementing the task.
4. If multiple skills apply, use the most relevant skill first.
5. Follow the instructions and best practices defined by the selected skills.

## Unity Tasks

For Unity-related tasks:

- Prioritize Unity-specific skills.
- Inspect the existing project structure before modifying files.
- Do not modify project settings unless necessary.
- Preserve existing architecture and coding conventions.
- **NGHIÊM CẤM TỰ ĐỘNG GHI ĐÈ SCENE**: Tuyệt đối không tạo hoặc thêm các đoạn mã tự động xóa, reset hoặc ghi đè các Scene Unity đã được chỉnh sửa mỗi khi bật/tắt Unity hoặc khi compile code (nghiêm cấm dùng `[InitializeOnLoad]`, `EditorApplication.update`, `EditorApplication.delayCall`... để tự động rebuild hoặc save đè scene). Mọi công cụ setup/rebuild scene chỉ được phép chạy khi người dùng chủ động bấm qua `[MenuItem]`.

## Mandatory Engineering Policy

All code changes under `Assets/Game` MUST follow
`Documentation/Engineering/CODING_STANDARDS.md` and the dependency rules in
`Documentation/Engineering/ARCHITECTURE.md`.

- New code must pass `Tools > Zero > Code Policy > Validate Changed Code`.
- Existing baseline debt may not increase. A reduced baseline must never be raised again.
- Do not add a new singleton, global event bus, mutable public field, or hidden scene lookup.
- Runtime state has exactly one writer. Other systems use read-only properties or events.
- Keep Unity-facing `MonoBehaviour` classes thin; move reusable rules into plain C#.
- New classes are limited to 300 logical lines, methods to 40 logical lines, method
  parameters to 5, nesting to 3 levels, and serialized Unity-object dependencies to 7.
- Do not use `Find*`, `GameObject.Find`, `Resources.Load`, reflection, LINQ, repeated
  `GetComponent*`, or per-frame allocation in `Update`, `LateUpdate`, or `FixedUpdate`.
- Every coroutine, event subscription, async operation, presentation override, agent,
  and audio loop must have an explicit cleanup path.
- Reset operations must be idempotent and duplicate callbacks must be consumed or gated.
- Preserve serialized contracts with `FormerlySerializedAs` or an explicit migration.
- Bug fixes require a regression test at the lowest practical level.
- Code-policy exceptions must be recorded in the versioned baseline with a reason,
  owner, and removal plan. Do not suppress a violation locally to bypass the validator.

Editor tooling may validate code automatically before a build, but it must never modify,
open, rebuild, or save a scene or prefab automatically.

## Minimalist Coding Policy (Ponytail)

The best code is the code you never wrote. Avoid over-engineering, speculative abstractions, and unnecessary boilerplate.

- **The Decision Ladder:**
  1. *Does this need to exist? (YAGNI):* If it's a speculative need, skip it.
  2. *Already in this codebase?:* Look before writing; reuse existing project helpers, classes, or patterns.
  3. *Stdlib / Engine API does it?:* Prefer native platform/engine features over custom implementations.
  4. *Installed dependency solves it?:* Never add new packages when existing tools can do it.
  5. *Can it be one line?:* Keep it concise.
  6. *Only then:* Write the absolute minimum working code.
- **Core Rules:**
  - No unrequested abstractions (no interface with one implementation, no factory for one product).
  - Root cause over symptom: fix bugs at the source where all callers route through.
  - Deletion over addition: fewer lines, fewer files, and smaller diffs win.
  - Boring over clever: keep logic simple, readable, and robust.

## Workflow

User Request
→ Identify relevant skills
→ Read SKILL.md
→ Analyze existing project
→ Create implementation plan
→ Implement
→ Verify changes
