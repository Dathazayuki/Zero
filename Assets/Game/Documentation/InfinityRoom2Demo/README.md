# Infinity Room2 — Living Room Demo

Bản blockout phòng khách duy nhất, dựng trực tiếp trong Unity bằng UnitySkills REST và ProBuilder. Đây là demo bố cục/hình ảnh để chỉnh sửa tiếp, chưa phải chặng campaign hoàn chỉnh.

## Lưu và thử demo

### Detail pass — 2026-10-03

Preview: [Room2_Detail_Final.png](Room2_Detail_Final.png).

- Đồng hồ được đưa tới `(1.60, 2.46, 4.89)`, ngoài vùng rèm; mặt số, vành và kim nằm trong `IR2_DetailWallClock`.
- Sofa/hai ghế có đệm bo, tay tựa, viền đệm, gối và chân gỗ; bản visual cũ được ẩn nhưng collider giữ nguyên.
- Rèm có bề mặt nếp gấp, ray và móc; backing cửa sổ cũ giữ lại.
- Thêm thảm Persian burgundy, parquet herringbone, vải olive, tranh phong cảnh, bộ trà có quai/vòi, cây có lá/cành riêng và đèn chùm năm tay.
- Mesh mới: `Assets/Game/Models/InfinityRoom2Demo/IR2_*.obj`; material: `Assets/Game/Materials/InfinityRoom2Demo/M_IR2Detail_*.mat`.
- Không đổi gameplay/campaign readiness, không sửa prefab tầng hoặc chạy Scene Builder. Scene edits đang trong bộ nhớ Editor; **nhấn Ctrl+S** trước khi đóng/chuyển scene.

### Texture provenance

Hai texture albedo được tạo bằng công cụ image generation tích hợp, không tải asset bên ngoài:

- `Assets/Game/Textures/InfinityRoom2Demo/T_IR2_PersianRug.png`: prompt direction — flat orthographic Persian rug albedo, burgundy/ivory floral medallion, decorative border, no perspective or baked shadows.
- `Assets/Game/Textures/InfinityRoom2Demo/T_IR2_OliveUpholstery.png`: prompt direction — seamless olive woven upholstery fabric, fine textile fibres, uniform lighting, no object or baked shadows.

Gỗ tái sử dụng `Model/table/textures/wood_table_001_diff_1k.jpg`; tranh tái sử dụng atlas `Model/free-cc0-medieval-paintings-pack/textures/cuadros.png`. Không sửa texture nguồn. Mesh detail được author riêng cho demo; chưa có LOD/lightmap UV production.

### Verification của detail pass

Đã kiểm tra screenshot camera Unity, material binding và scene dirty. Không thêm runtime C# hoặc thay collider kiến trúc. Chưa chạy Play Mode traversal/performance benchmark; cần người dùng kiểm tra đi quanh ghế/bàn và duyệt chất lượng art trước khi đưa vào pool campaign.

1. Scene đang mở: `Assets/Game/Scenes/Demos/InfinityRoom2_LivingRoom_Demo.unity`.
2. Khi kiểm tra cuối, Editor báo scene không dirty và file scene đã chứa hierarchy demo. Agent không gọi save scene/prefab. Sau khi chỉnh sửa tiếp, **nhấn Ctrl+S trong Unity trước khi đóng hoặc chuyển scene**.
3. Nhấn Play; `IR2_DemoPlayer` dùng `FirstPersonController` hiện có: WASD di chuyển, chuột nhìn, Shift chạy.
4. Trong Editor, dùng Shift+F1 để nhả chuột. Demo không có GameFlow, pause menu hoặc checkpoint save của campaign.
5. Kiến trúc/collider blockout dùng `ProBuilderMesh`; các object `IR2_Detail*` dùng model OBJ trong `Models/InfinityRoom2Demo`. Chỉnh Transform/material trong Inspector; muốn sửa topology OBJ cần công cụ modeling/import riêng. Không chạy Scene Builder.

## Bố cục

- Một không gian liên tục: kích thước sử dụng khoảng **9 × 10 m**, cao **3.2 m**.
- Tường kín dày **0.20 m**; sàn dày **0.25 m**, mặt sàn tại Y = 0.
- Chỉ một cửa vào ở tường Đông, rộng **1.3 m**, cao **2.4 m**. Cửa đang là khối đóng tĩnh, chưa có mechanic mở cửa.
- Sofa, hai ghế đơn, bàn trà, bộ ấm/chén, TV CRT, tủ trưng bày, kệ sách, bàn điện thoại, đèn cây, rèm, tranh, gương và cây cảnh.
- Không có giường ngủ, phòng phụ hoặc vách chia khu.
- Palette: tường kem cũ, gỗ tối, vải olive, thảm burgundy, ánh sáng vàng ấm.

## Hierarchy chính

```text
InfinityRoom2_LivingRoom_Demo
├── IR2_Architecture
├── IR2_Furniture
├── IR2_Decor
├── IR2_Lighting
└── IR2_Markers
    └── IR2_LoopResumePoint
IR2_DemoPlayer
└── IR2_PreviewCamera
```

Material riêng nằm tại `Assets/Game/Materials/InfinityRoom2Demo`. Texture sàn tái sử dụng `wood_table_001_diff_1k.jpg` hiện có, không sửa texture nguồn. Chỉ đèn chính có shadow; ba đèn fill không có shadow.

## Giới hạn của demo

- Furniture đã có bản detail OBJ cho sofa, ghế đơn, rèm, bộ trà, cây và đèn chùm; vẫn là demo art, chưa phải production asset hoàn thiện.
- Tranh dùng landscape từ atlas hiện có; gương vẫn chưa phản chiếu; TV chưa có nội dung/âm thanh.
- Chưa có anomaly, roster, thang/cầu thang, commit trigger, NavMesh hay chuyển chặng.
- Các đồ đạc nhiều phần được tách để dễ sửa. Trước khi dùng trong pool tầng, cần tối ưu renderer/mesh theo nhóm material và kiểm tra performance; không nhân nguyên demo nhiều lần để coi là production asset.
- `InfinityRoom2.asset` vẫn `Content Ready = false`, chưa có `Segment Prefab`; không tự bật readiness cho demo.

## Đưa vào campaign sau khi duyệt bố cục

1. Lưu demo thủ công, hoàn thiện art và kiểm tra lối đi trong Play Mode.
2. Tạo prefab Room2 từ root môi trường, không đưa `IR2_DemoPlayer`/camera demo vào prefab phòng.
3. Đưa prefab phòng vào một prefab `FloorSegment` có anchors, single commit trigger và `RoomAnomalyController` theo contract hiện có.
4. Bổ sung anomaly component/Anomaly Slots, rồi gán đúng roster và segment prefab vào `ScriptableObjects/Campaign/InfinityRoom2.asset`.
5. Kiểm tra checkpoint/entry và chuyển chặng; chỉ bật `Content Ready` sau nghiệm thu.

Hướng dẫn đầy đủ: [README_CAMPAIGN_AUTHORING.md](../Engineering/README_CAMPAIGN_AUTHORING.md).

## Kiểm tra đã thực hiện

- Xem ảnh render từ camera Unity: [Room2_Blockout_Preview_v2.png](Room2_Blockout_Preview_v2.png).
- Physics raycast xác nhận sàn tại Y = 0, tường Tây và collider cửa vào.
- Overlap box tại điểm spawn chỉ gặp CharacterController của chính Player, không gặp đồ đạc/tường.
- Editor không đang compile/update tại thời điểm kiểm tra. Không thêm hoặc sửa runtime C#.
- Chưa thực hiện Play Mode traversal; cần kiểm tra thủ công đi quanh bàn/ghế và va chạm trước khi dùng trong gameplay.
- Hash scene Title không thay đổi; không gọi Scene Builder hoặc save scene/prefab tự động.
