# Infinity Room3 — The Records Room Demo

Bản scene demo độc lập theo concept Phòng Hồ Sơ của campaign Zero. Geometry được dựng qua UnitySkills REST, có dry-run và Undo; không dùng Scene Builder hoặc thêm script tự dựng scene.

> **Trạng thái khôi phục sau crash (03-10-2026):** Đã dựng lại bố cục theo ảnh tham chiếu trong Unity Editor và chụp preview. File scene trên đĩa hiện có 16 tủ, 80 hồ sơ và các cụm cây; một số chi tiết cuối (bao gồm vent trần) chưa thấy trong bản lưu. Hãy kiểm tra scene đang mở và nhấn `Ctrl+S` để lưu bản khôi phục mới nhất. Không dùng script rebuild để phục hồi.

## Mở và lưu

- Scene: `Assets/Game/Scenes/Demos/InfinityRoom3_RecordsRoom_Demo.unity`.
- Scene đã được lưu một phần trước crash; những chỉnh sửa sau thời điểm đó có thể chỉ còn trong Editor đang mở. **Nhấn Ctrl+S trong Unity** sau khi xác nhận preview/bố cục để ghi bản khôi phục mới nhất.
- Play: WASD di chuyển, chuột nhìn, Shift chạy; Shift+F1 nhả chuột trong Unity Editor.
- Demo không có GameFlow, pause menu, save checkpoint hoặc mechanic mở cửa.

## Thiết kế và thông số

- Một phòng duy nhất, kích thước bên trong **9 × 11 m**, cao **3 m**.
- Tường dày **0.20 m**, sàn dày **0.25 m**, mặt đi lại ở Y = 0.
- Một cửa ở tường Nam, rộng **1.30 m**, cao **2.35 m**; hiện là cửa đóng tĩnh.
- **16 tủ sắt**: sáu tủ phía Tây, tám tủ phía Đông và hai tủ cuối phòng. Các tủ bên hông xếp sát thành dãy, mỗi tủ có bốn ngăn kéo/tay nắm/nhãn; màu sơn có biến thể bạc màu.
- Hai kệ hồ sơ giữa phòng, cao **2 m**, mỗi kệ dài **3.6 m** và rộng **0.7 m**.
- Khoảng trống giữa hai kệ **1.9 m**, giữa kệ phải và tủ sát tường khoảng **1.4 m**; có lối vòng đầu/cuối kệ.
- Bàn tra cứu với ghế, sổ, điện thoại và đèn bàn; máy photocopy; bảng thông báo, đồng hồ và bàn giấy cuối phòng.
- Trần thả có grid, bốn fixture huỳnh quang và một đèn bàn. Chỉ một nguồn sáng có realtime shadow.
- Màu kem cũ, xanh xám/olive, giấy beige, gỗ tối và điểm nhấn binder đỏ/xanh.
- Thay 16 khối binder lớn bằng **80 binder riêng lẻ**, có nhãn gáy và chi tiết lỗ vòng; thêm nắp hộp, băng giấy, giấy xếp chồng và hồ sơ trên nóc tủ.
- Ba chậu cây có dây rủ; bàn có giấy, bút, chi tiết phím/cord điện thoại và dòng kẻ sổ; thêm bánh ghế, giấy máy photocopy, kệ sách dưới bàn cuối phòng, ghim/chi tiết bảng thông báo, vent và đầu báo khói.
- Texture gỗ bàn, plaster tường/trần và bề mặt sàn tái sử dụng asset có sẵn qua material riêng; không sửa texture nguồn. Ánh sáng chuyển sang vàng ấm, góc camera thấy bàn làm việc ở tiền cảnh.

```text
InfinityRoom3_RecordsRoom_Demo
├── IR3_Architecture
├── IR3_Furniture
│   ├── IR3_Cabinet1…16
│   ├── IR3_ShelfA / IR3_ShelfB
│   ├── IR3_DetailedFiles
│   ├── IR3_Desk / IR3_Chair
│   └── IR3_Photocopier
├── IR3_Decor
│   └── IR3_RefinedDecor
├── IR3_Lighting
└── IR3_Markers
    └── IR3_LoopResumePoint
IR3_DemoPlayer
└── IR3_PreviewCamera
```

## Chỉnh sửa tiếp

Kiến trúc và đồ đạc blockout dùng ProBuilder mesh, có thể sửa bằng ProBuilder. Cây, hộp hồ sơ và bìa tài liệu của đợt bổ sung chi tiết dùng model OBJ riêng; chỉnh topology của chúng trong phần mềm dựng mesh rồi reimport. Material riêng tại `Assets/Game/Materials/InfinityRoom3Demo`.

### Đợt bổ sung chi tiết 03-10-2026

- Ba cây được thay bằng mesh có 71 lá nhọn/cây, sống lá nổi, gân giữa, cành phân nhánh và bốn dây rủ cong. Chậu có thành trong, miệng dày và mặt đất riêng. Mỗi cây gồm sáu phần mesh theo vật liệu, không có collider chi tiết.
- 16 hộp trên nóc tủ và 12 hộp đóng trên kệ có mép nắp, dải băng giấy, nhãn và nét in thay cho khối đơn sắc.
- Bốn hộp mở trên tầng trên cùng của kệ có lỗ cầm thật, thành hộp, nắp cất dưới đáy, bảy bìa hồ sơ có tab và các tờ giấy bên trong.
- Bìa tài liệu mở trên bàn có hai bìa, nhiều lớp giấy, dòng in, kẹp và bút.
- Các đối tượng cũ được giữ inactive để có thể đối chiếu; camera preview được trả về vị trí ban đầu sau khi chụp cận cảnh.
- Model nguồn tại `Assets/Game/Models/InfinityRoom3Demo`: `IR3_DetailedPothos.obj`, `IR3_ArchiveStorageBox.obj`, `IR3_OpenArchiveBox.obj`, `IR3_OpenDocumentFolder.obj`. Đây là mesh hình học được author riêng cho demo, không dùng asset tải bên ngoài hoặc script sinh geometry lúc chạy game.
- Pothos có khoảng 9.000 triangle/cây; đây vẫn là asset demo chưa có LOD. Các phần cùng vật liệu được gom trong mesh nguồn để tránh tạo một GameObject cho mỗi lá/tờ giấy.
- Thay đổi scene đang ở Unity Editor; nhấn **Ctrl+S** để lưu trước khi đóng scene.

Preview: [toàn phòng](Room3_DetailPass.png), [cận cảnh cây](Room3_Plant_Closeup.png), [cận cảnh hồ sơ](Room3_Archive_Closeup.png).

- Bản refinement bám mật độ đồ đạc và palette concept hơn, nhưng vẫn là demo low-poly, không phải bản tái tạo photorealistic. Chưa có texture chữ hồ sơ đọc được, model cây/điện thoại hoàn thiện hoặc baked lighting.
- Các khối binder cũ được giữ **inactive**, không xóa, để có thể đối chiếu/khôi phục.
- Các chi tiết tĩnh mới được gộp theo material trong `IR3_DetailedFiles` và `IR3_RefinedDecor`; khi muốn animate một binder riêng, cần tách mesh hoặc thay bằng prefab. Phần gộp chỉ áp dụng chi tiết mới tạo, không gộp kiến trúc/tủ/kệ cũ.
- Không có reflection, máy photocopy hoạt động, audio, anomaly, NavMesh hoặc logic tương tác.
- Đồ đạc nhiều phần được tách để dễ authoring. Trước khi đưa vào pool, tối ưu/merge phần tĩnh theo material, giữ phần có thể thay đổi làm anomaly riêng và kiểm tra profiler.
- Bản này chỉ là **The Records Room**. **The Calling Corridor** là minigame khác, chưa được dựng trong scene này.

## Nối vào campaign sau khi duyệt art

1. Lưu scene thủ công, kiểm tra di chuyển và hoàn thiện asset.
2. Tạo Room3 prefab từ root môi trường; không đưa Player/camera demo vào prefab phòng.
3. Đưa Room3 vào prefab `FloorSegment` với anchors, commit trigger và `RoomAnomalyController` theo contract hiện có.
4. Tạo anomaly component/Anomaly Slots và roster tương ứng; gán segment prefab và roster vào `ScriptableObjects/Campaign/InfinityRoom3.asset`.
5. Chỉ bật `Content Ready` sau khi nghiệm thu. Definition hiện vẫn giữ `false`, không tự coi demo là campaign chơi hoàn chỉnh.

Xem [README_CAMPAIGN_AUTHORING.md](../Engineering/README_CAMPAIGN_AUTHORING.md) cho quy trình đầy đủ.

## Kiểm tra

- [Preview bản khôi phục theo ảnh tham chiếu](Room3_Recovered_Final_20261003.png). Đây là ảnh trong Editor sau khi dựng lại; scene cần được lưu thủ công nếu các chỉnh sửa mới nhất chưa được ghi xuống đĩa.
- Raycast xác nhận mặt sàn Y = 0 và tường Tây tại X = -4.5.
- Overlap box tại spawn chỉ gặp CharacterController của chính Player.
- Spherecast bán kính 0.28 m xuyên lối giữa hai kệ không gặp vật cản trong đoạn được kiểm tra.
- Bản blockout trước đã qua Play Mode smoke test 5 giây: `healthy = true`, `errorCount = 0`. Kết quả này không tự chứng minh bản refinement; cần kiểm tra lại sau khi chỉnh. Smoke test không thay thế kiểm thử đi quanh toàn phòng.
- Hash scene Room2 không đổi sau authoring; definition Room3 vẫn `Content Ready = false`, prefab/roster chưa gán.
- Không thêm/sửa runtime C#, scene gốc hoặc cấu hình campaign.
