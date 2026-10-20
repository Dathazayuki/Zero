# Zero - CorrectArrow Dynamic Environment Loop

## Chạy dự án Unity trên máy khác

### Yêu cầu

- Windows 10/11 (các hệ điều hành khác cần cấu hình build target tương ứng).
- Git và Git LFS.
- Unity Hub và Unity Editor **6000.0.40f1** (Unity 6.0), theo `ProjectSettings/ProjectVersion.txt`.
- Kết nối Internet lần đầu để tải Unity packages. Unity Hub cũng cần được đăng nhập với license hợp lệ.

### Clone và mở project

Clone repository đầy đủ; không clone riêng thư mục `Assets/Game`, vì cấu hình Unity nằm ở cấp project:

```powershell
git lfs install
git clone <URL_REPOSITORY>
cd <THU_MUC_REPOSITORY>
git lfs pull
```

Mở Unity Hub → **Add** → chọn thư mục repository có các thư mục `Assets`, `Packages` và `ProjectSettings`. Chọn Editor **6000.0.40f1** để mở project. Chờ Package Manager tải package và Unity hoàn tất import asset lần đầu; không cần đưa `Library`, `Temp`, `Logs` hay `UserSettings` từ máy cũ sang.

Project dùng URP và Input System. Phiên bản package đã được khóa trong `Packages/manifest.json` và `Packages/packages-lock.json`; hãy để Unity resolve theo hai file này.

### Tripo Bridge (tuỳ chọn)

Trước lần mở project đầu tiên, xử lý dependency local này trong `Packages/manifest.json`:

```json
"com.tripo3d.unitybridge": "file:E:/Tripo3d_Unity_Bridge"
```

Đường dẫn này thuộc máy phát triển hiện tại, nên Unity Package Manager trên máy mới sẽ không tìm thấy package nếu giữ nguyên. Nếu máy mới có Tripo Unity Bridge, đặt package ở một đường dẫn local trên máy đó rồi sửa giá trị dependency sang đường dẫn tương ứng. Nếu chỉ cần mở/chạy game và không dùng Tripo Bridge, xoá riêng entry `com.tripo3d.unitybridge` khỏi phần `dependencies` trước khi mở Unity. Đừng commit đường dẫn local của máy cá nhân; nếu nhóm cần Bridge, hãy thống nhất một nguồn package dùng chung trước khi đưa dependency đó vào Git.

### Chạy demo

Trong Unity, mở `Assets/Game/Scenes/Demos/Minigame4_WatchfulHall_Demo.unity` rồi nhấn **Play**. Đây hiện là scene duy nhất được bật trong Build Settings. Để chạy bản build Windows, chọn **File → Build Profiles** (hoặc **Build Settings** tuỳ giao diện Unity), xác nhận scene demo được bật và cài Windows Build Support cho Editor nếu Unity yêu cầu.

Các scene demo khác nằm trong `Assets/Game/Scenes/Demos/`. Mở scene mong muốn trực tiếp trong Editor; Build Settings hiện không bật tất cả demo.

### Lưu ý Git

- Cài Git LFS trước khi clone để tải đầy đủ scene Unity (`*.unity`) và model OBJ (`*.obj`), được khai báo trong `.gitattributes` ở gốc repository.
- Commit file `.meta` cùng asset mới trong `Assets`; Unity dùng chúng để giữ GUID/reference ổn định.
- Không commit thư mục cache sinh tự động như `Library`, `Temp`, `Logs`, `Obj`, `Build`, `Builds` hoặc `UserSettings`.
- Nếu scene hoặc model hiện biểu tượng LFS pointer / không mở được, chạy `git lfs pull` từ gốc repository rồi mở lại project.

## Core rule

Every active `FloorSegment` owns a locked Room round with a random `CorrectArrow` direction and a 55% anomaly roll.

```text
Normal Room  -> follow CorrectArrow
Anomaly Room -> take the opposite direction from CorrectArrow
```

Truth table:

```text
Normal  + Arrow UP   -> choose UP
Normal  + Arrow DOWN -> choose DOWN
Anomaly + Arrow UP   -> choose DOWN
Anomaly + Arrow DOWN -> choose UP
```

The Room state is prepared when its segment becomes active and never rerolls when the segment becomes Current.

## Floor number progress

`Floor_Number` contains display objects `0` through `8` (names `Number0` through `Number8` are also accepted).

```text
Correct decision -> increase by 1, clamped at 8
Wrong decision   -> the next floor displays 0
```

This value is consecutive-correct progress and is independent from `LogicalFloorIndex`.

## Dynamic segment pool

- The configured pool contains at least nine `PF_FloorSegment` instances.
- Exactly five are active: `Below2 -> Below1 -> Current -> Above1 -> Above2`.
- All remaining segments are disabled, reset reserves.
- A commit keeps only the adjacent target, acquires four inactive reserves, releases the other four members of the old active window, then builds two new Below and two new Above positions.
- The segment immediately behind the Player is always a different physical instance from the floor just left.
- There is no runtime `Instantiate`, `Destroy`, Player teleport, fade, or scene reload.
- `SegmentCommitTrigger` owns the complete transition. The old recycle-trigger system has been removed.

Each returned segment resets transforms, anomaly state, CorrectArrow, audio, particles, animators, rigidbodies, and commit triggers before entering the inactive pool.

## Runtime debug

- `F1`: toggle debug panel.
- `F2`: force the current Room to Normal without changing its Arrow direction.
- `F3`: force an Anomaly without changing its Arrow direction.

The panel displays current Arrow, expected direction, anomaly, Number progress, all five active roles, active/inactive/total pool counts, and active segment order.

## Rebuild and validation

- `Tools > Environment Loop > Install or Repair Room Anomalies`
- `Tools > Environment Loop > Create or Rebuild Complete Scene`
- `Tools > Environment Loop > Run Environment Stress Validation`

The builder reuses `Assets/Game/Prefabs/Environment/PF_FloorSegment.prefab`, preserving the handmade Room and environment geometry.

The validator covers the eight Arrow/Anomaly truth-table cases, Number `0 -> 8` and wrong reset, the five-role active window, pools 9/12, inactive baseline state, target-only retention, physical replacement behind the Player, 100 UP, 100 DOWN, 200 direction changes, stable pooled identities, anchor alignment, and unchanged Player transform.
