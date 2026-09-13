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

## Workflow

User Request
→ Identify relevant skills
→ Read SKILL.md
→ Analyze existing project
→ Create implementation plan
→ Implement
→ Verify changes