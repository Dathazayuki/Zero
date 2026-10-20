# Zero — Horror Game (Unity)

## Mở project trên máy mới

### Yêu cầu

- Windows 10/11.
- Git và Git LFS.
- Unity Hub và Unity Editor **6000.0.40f1** (Unity 6.0), theo `ProjectSettings/ProjectVersion.txt`.
- Kết nối Internet để tải Unity packages; đăng nhập Unity Hub bằng license hợp lệ.

### Clone và mở

Clone toàn bộ repository, không clone riêng thư mục `Assets/Game`:

```powershell
git lfs install
git clone <URL_REPOSITORY>
cd <THU_MUC_REPOSITORY>
git lfs pull
```

Trong Unity Hub, chọn **Add** và mở thư mục repository chứa `Assets`, `Packages` và `ProjectSettings`. Chọn Editor **6000.0.40f1**, sau đó chờ Package Manager tải dependency và Unity hoàn tất lần import đầu tiên. Không cần sao chép `Library`, `Temp`, `Logs` hoặc `UserSettings` từ máy cũ.

Project sử dụng URP và Input System. Package versions được quản lý trong `Packages/manifest.json` và `Packages/packages-lock.json`.

### Tripo Unity Bridge (tuỳ chọn)

Hiện `Packages/manifest.json` trỏ Tripo Bridge tới `file:E:/Tripo3d_Unity_Bridge`, là đường dẫn riêng trên máy phát triển. Trước lần mở project đầu tiên trên máy mới:

- Nếu cần dùng Bridge, đặt package ở đường dẫn trên máy mới và đổi dependency sang đường dẫn đó.
- Nếu chỉ cần chạy game, xoá entry `com.tripo3d.unitybridge` trong `dependencies` của `Packages/manifest.json`.

Đừng commit đường dẫn local của máy cá nhân. Nếu nhóm cần Bridge, hãy chuyển dependency sang nguồn package dùng chung trước khi commit.

### Chạy demo

Mở `Assets/Game/Scenes/Demos/Minigame4_WatchfulHall_Demo.unity` trong Unity rồi nhấn **Play**. Đây hiện là scene duy nhất được bật trong Build Settings. Để tạo bản Windows, xác nhận scene này được bật trong Build Profiles và cài Windows Build Support nếu Unity yêu cầu.

Các scene demo khác nằm trong `Assets/Game/Scenes/Demos/`; mở scene mong muốn trực tiếp trong Editor.

### Lưu ý Git

- Chạy `git lfs pull` nếu scene (`*.unity`) hoặc model OBJ (`*.obj`) không tải đầy đủ. Các loại file này được khai báo trong `.gitattributes`.
- Commit file `.meta` cùng asset mới trong `Assets` để giữ GUID và reference Unity.
- Không commit cache như `Library`, `Temp`, `Logs`, `Obj`, `Build`, `Builds` hoặc `UserSettings`.

Chi tiết gameplay và authoring hiện nằm trong `Assets/Game/Documentation/` và README riêng của từng demo.
