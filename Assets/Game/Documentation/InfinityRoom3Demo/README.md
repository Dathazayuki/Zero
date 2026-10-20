# Infinity Room3 — The Records Room Demo

## Preview anomaly Records Room

Trong Play Mode, nhấn **F3** để chọn lần lượt IR3-01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11 → 12 → 13 → 14 → 15 → 16 → 17 → 18 rồi quay vòng; nhấn **F2** để trả phòng về bình thường. Sau F2, F3 bắt đầu lại từ IR3-01. Overlay ở góc màn hình hiển thị ID đang chọn.

| ID | Dấu hiệu | Vị trí |
|---|---|---|
| IR3-01 | Hai mã A-12 và A-13 đổi chỗ; nhãn còn lại giữ nguyên. | Hai ngăn trên của `IR3_Cabinet1`, dãy tủ phía Tây gần bàn. |
| IR3-02 | Một cây bút xoay 180° tại chỗ trên mặt bàn. | `IR3_DetailFolder_Desk/Pen/Pen`. |
| IR3-03 | Năm tập hồ sơ bật vật lý rồi đổ khỏi kệ, va chạm và rơi tự nhiên xuống sàn. | `IR3_File_A_L3_S0_B0..4`, tầng trên kệ A; mỗi tập có BoxCollider và Rigidbody. |
| IR3-04 | Ngăn trên hé ra 2,5 cm, tay nắm và nhãn đi cùng. | `IR3_Cabinet1_Drawer3`. |
| IR3-05 | Góc giấy gấp từ trên phải đổi sang trên trái; chữ không đổi. | `IR3_Notice_0` trên bảng thông báo cuối phòng. |
| IR3-06 | Ba chồng giấy hiện chữ máu R/U/N. | Bàn cuối phòng, `Tailieu (1..3)`. |
| IR3-07 | Dấu bàn tay đen trên mặt ngăn kéo. | Các tủ hồ sơ. |
| IR3-08 | Máy in liên tục nhả giấy xuống sàn. | Máy in gần bàn làm việc. |
| IR3-09 | Gáy hồ sơ có mặt cười bằng máu từ `Art/InfinityRoom3/Smile.png`. | Hai kệ hồ sơ giữa phòng; nhóm `IR3_09_ClosedDossiers/BloodSmiles`. |
| IR3-10 | Mặt người nhìn theo, tới gần sẽ rút vào khe hồ sơ. | Tầng trên kệ A, hàng `L3_S2`. |
| IR3-11 | Ảnh thẻ từ trống thành mắt nhắm, rồi mở mắt sau hai lần quay đi và nhìn lại. | Hồ sơ nhân viên phía trên bên phải bảng cuối phòng. |
| IR3-12 | Hộp hồ sơ rung như có người bên trong. | Hộp trên Cabinet3. |
| IR3-13 | Chín ngăn kéo rải lệch hàng trên tám tủ mở lần lượt khi tới gần, chứa các bộ phận nhân viên lưu trữ, không có đầu. | Đủ tám prefab `IR3_Cabinet1 (8)`–`(15)`, dãy bên phải; tủ `(11)` có hai ngăn. |
| IR3-14 | Đèn bàn chập chờn với nhịp không đều suốt anomaly. | `IR3_Lighting/IR3_DeskLampLight`; cấu hình tại `IR3_14_DeskLampFlicker`. |
| IR3-15 | Hộp rơi, đầu mọc sáu chân; nhìn tới khi mọc xong sẽ bị vồ và gây chết. | Hộp trên Cabinet4. |
| IR3-16 | Vòng tròn pentagram bằng máu trên trần, dùng `Art/InfinityRoom3/SatanCircle.png`. | Trần cuối phòng; `IR3_16_CeilingPentagram/SatanCircle`. |
| IR3-17 | Tủ `(8)` rung; tới gần sẽ kích hoạt tủ `(5)` lao ngang để nghiền Player, camera xoay mượt theo hướng lao. Né ra khỏi đường lao để sống. | Hai tủ đối diện nhau ở cuối phòng; `IR3_17_CabinetCrush`. |
| IR3-18 | Các phần cơ thể kèm nhãn rơi rải trên sàn, lết về vòng tròn quỷ, ráp thành người không đầu rồi bò đuổi; tới gần sẽ Attack và gây chết. | Vòng tròn tại `(0; 0,014; 3,5)`, nhóm `IR3_18_BodyAssembly`; Animator `RecordRoomWoman`. |

Các entry được nhóm trong `IR3_Anomalies`, dùng lại `LivingRoomArrangementAnomaly` và `LivingRoomAnomalyPreview` của IR2. Mười tám cấu hình `AnomalyData` nằm trong `ScriptableObjects/Anomalies/InfinityRoom3`; chỉ IR3-15/17/18 gây chết. Menu `Tools > Zero > Records Room > Update Anomalies 03 09 14 16` cập nhật bốn mục theo kế hoạch mới; `Add Encounters 17 18` nối hai encounter cuối và bake NavMesh. Scene được lưu thủ công. Các tên node cũ IR3-03/09 được giữ để bảo toàn tham chiếu.

IR3-03 chỉ bật Rigidbody và trọng lực; không cộng vận tốc ngang, hất lên hoặc lực xoay. Unity Physics tự mô phỏng chuyển động và va chạm. Hồ sơ nằm vững trên mặt kệ sẽ được kệ đỡ; cần đặt chênh mép/mất cân bằng nếu muốn nó tự đổ bằng trọng lực. F2/reset/disable dừng chuyển động, trả pose trên kệ và trạng thái kinematic ban đầu. Menu migration riêng: `Tools > Zero > Records Room > Use Physics For Falling Files 03`; nó gán Rigidbody/collider, bỏ marker Landing và cấu hình lực/quỹ đạo cũ khi scene được lưu lại. IR3-14 có `Lit Seconds`, `Dark Seconds` và `Seed` để điều chỉnh nhịp sáng/tắt. Hai ảnh nguồn không bị sửa: material dùng chung đọc texture mask riêng chỉ giữ nét đỏ, loại nền đen/ô caro.

IR3-17: `Trigger Distance` 1,9 m tính từ bounds tủ `(8)` trên mặt sàn, cảnh báo 0,9 giây, lao trong 0,55 giây. Tủ di chuyển cùng collider; vùng quét kiểm tra Player giữa hai bước vật lý. Khi va trúng Player, màn hình đen ngay trong cùng bước vật lý (`Freeze Duration = 0`). Đây là tùy chọn chung: anomaly có `Style2_FreezeBlackout` và gọi `TryBeginDeath` cũng có thể đặt `Freeze Duration = 0` để dùng cùng kiểu chết. Camera chỉ hướng về tủ `(5)` trong cú lao/cảnh bắt; reset khôi phục cả hai tủ và nhả camera.

IR3-18: rơi 1,5 giây → lết về vòng tròn 9 giây → ghép 3 giây → chase không giới hạn thời gian. Dùng chín mesh/material gốc IR3-13 và thêm phần hông tách từ cùng model để nối kín thân, giữ nguyên asset/ngăn kéo IR3-13. Nhãn gắn theo bone trên bản sao rigged. `ArchiveAssemblyPresentation/Chase Speed` mặc định 3,8 m/s; model phát `Running Crawl` khi đuổi, `Attack` khi bắt trong 0,85 m và không có vật cản. NavMesh chỉ thu geometry trong room; dữ liệu tại `IR3_18_Navigation.asset`. F2/disable dọn agent, vòng tròn, các phần cơ thể và pose; không có vùng an toàn hay timeout trong phòng. Khi đưa vào campaign, reset/deactivate room lúc chuyển tầng sẽ kết thúc truy đuổi; bản demo độc lập hiện chưa có tuyến chuyển tầng hoạt động.

Kiểm thử tích hợp: `RecordsRoomAnomalyPreviewTests` bao phủ danh sách 18 anomaly, phím F2/F3, quay vòng và khôi phục/disable. `RecordsRoomDossierPresentationTests` bao phủ reset cụm hồ sơ/khuôn mặt và collider Player khi nhìn ảnh. `ArchiveDrawerSequenceTests` kiểm tra khoảng cách, nhịp mở và reset chuỗi ngăn kéo. `ArchiveFallingFilesAnomalyTests` và `DeskLampFlickerAnomalyTests` kiểm tra reset/disable trong lúc đang rơi hoặc nhấp nháy. `ArchiveCabinetCrushRulesTests`/`ArchiveAssemblyRulesTests` kiểm tra pha cảnh báo, một lần lao, ghép trước chase và chase không timeout; các test lifecycle cùng tên anomaly kiểm tra reset. Scene phải được lưu thủ công trước khi chạy test.

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
