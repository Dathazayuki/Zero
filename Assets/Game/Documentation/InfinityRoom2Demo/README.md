# Infinity Room2 — Living Room Demo

### IR2-10, 15, 16, 17

Menu chủ động `Tools > Zero > Living Room > Add Anomalies 10 15 16 17` nối bốn nội dung mới vào preview theo ID. Thứ tự F3 là **01–13 → 15 → 16 → 17 → 18 → 20 → 01**; F2 khôi phục phòng. IR2-14/19 chưa triển khai.

IR2-20 dùng menu `Tools > Zero > Living Room > Add Family Rug Anomaly 20`. Father/Mother ngồi trên sofa dài, đứa trẻ chỉ đổ bóng dưới bàn trà. Khi Player trên thảm, hoa văn tối từ mép về chân trong 3 giây và ghế B kéo nhẹ tới tối đa 0,35 m. Rời thảm trước thời hạn sẽ hủy cảnh báo và trả ghế/hoa văn về bình thường. Ở lại đủ thời gian: tay Children trồi lên, death API khóa Player và đặt hai người cách phía trước khoảng 1,05 m, cách nhau 0,68 m; nếu đồ đạc chắn chỗ đứng sẽ chọn góc trống gần hướng nhìn nhất. Mother phát `Attack`, Father phát `Attack2`. Camera xoay xuống tay trong 0,45 giây, giữ 0,35 giây rồi ngẩng lên hai người trong 0,65 giây; sau đoạn freeze 3,1 giây thì blackout và hồi sinh bằng death API. Khi ngẩng lên, hai animation được phát lại ở tốc độ thường để thấy rõ đòn đánh. F2/disable dọn pose, ghế, thảm, tay và camera override; death controller sở hữu khóa điều khiển và hồi sinh. `Warning Seconds`, `Look Down Seconds`, `Hands Hold Seconds`, `Look Up Seconds` nằm trên `IR2_20_YourSeat`.

IR2-12/18 dùng menu `Tools > Zero > Living Room > Add Television Anomalies 12 18`. IR2-12 phát hình Father/Children/Mother ngồi trên sofa, sau 3 giây đồng loạt nghiêng đầu 28°. Camera hậu trường chỉ xuất hình 12 FPS vào RenderTexture 512×384 khi anomaly đang bật; reset giải phóng texture và tắt hậu trường.

IR2-18 dùng Mother: 3 giây áp mặt sau kính, màn hình phồng và nứt, sau đó thoát ra trong 1,4 giây bằng `FamilyAnimation/RunLunge`. Mother truy đuổi trên NavMesh bằng `RunNormal` ở 2,7 m/s và chuyển thẳng sang `Attack` khi bắt được Player. Pha áp sát không có `RunLunge` hoặc tăng tốc riêng. Chỉ bắt khi tiếp xúc trong 0,7 m và không bị vật cản che khuất; dùng death API/hồi sinh hiện có. Rời phạm vi phòng (mặc định qua z=-4,5 ở lối vào) hoặc sau 20 giây truy đuổi sẽ hủy nguy hiểm. Inspector `IR2_18_MotherFromTelevision` cho phép chỉnh thời gian và tốc độ. Agent, feed và pose Mother đều được dọn khi F2/chuyển anomaly/disable. NavMesh được bake bởi menu chủ động, không có hook tự xây hoặc tự lưu scene.

IR2-13 được gán bằng menu `Tools > Zero > Living Room > Add Painting Anomaly 13`. `IR2_DetailLandscapeNorth/Landscape` hiển thị toàn bộ `IR2_13_Landscape_00_Normal.png` khi bình thường. Khi anomaly bật, ba texture có người lần lượt hiện theo thứ tự Distant → Closer → Close. Sau khi nhìn thấy tranh, quay đi ít nhất 0,4 giây rồi nhìn lại để tiến thêm một nấc; nhìn liên tục không đổi ảnh. F2/reset/disable khôi phục normal. Component `ApproachingPaintingAnomaly` cho phép chỉnh `Look Away Seconds`. Menu chỉ đánh dấu scene thay đổi, người dùng tự lưu.

- **IR2-10:** đệm giữa sofa dài `IR2_DetailSofa` lún, nhịp thở 3,8 giây; gối dịch sang bên. Mesh bình thường được khôi phục khi reset.
- **IR2-15:** bàn chân lấy từ model Children dưới rèm. Đến trong khoảng 1,4 m sẽ làm rèm phồng rồi xẹp trong 1,2 giây, chân rút ra sau rèm.
- **IR2-16:** khuôn mặt lấy từ Children trong bóng dưới bàn trà. Đến gần 1,4 m hoặc nhìn xuống trong khoảng 2,4 m làm khuôn mặt rút vào trong 0,7 giây.
- **IR2-17:** Father ngồi trên ghế đơn A. Đến trong khoảng 1,8 m và không có vật cản sẽ bắt đầu cảnh báo 2,5 giây, rig đầu quay dần về vị trí camera Player. Có thể kéo thả bone đầu vào `Head Bone` trên component `SeatedFatherAnomaly` của `IR2_17_SeatedFather`; để trống sẽ dùng Humanoid Head. Sau đó dùng `SitToStand` và `Attack` hiện có để đứng dậy/lao ngắn. Lùi xa hơn 2,2 m trước khi hết cảnh báo sẽ tránh đòn. Chỉ tiếp xúc trong tầm 0,65 m và không bị che khuất mới gọi death API hiện có; demo fade đen và hồi sinh tại cửa vào. Không truy đuổi vô hạn.

Các anomaly dùng `AnomalyData` riêng, cùng lifecycle bật/tắt/reset của phòng. Model Father/Mother/Children gốc giữ làm nguồn authoring, được ẩn để không xuất hiện ở phòng bình thường. Actor của IR2-17 dùng controller `FamilyAnimation` hiện tại. Không thay đổi campaign readiness và không tự lưu scene.

### IR2-06 — bóng Father và ghế, 2026-10-17

Bóng được lấy từ model Father với state Sit của controller FamilyAnimation hiện tại, căn hông vào đệm ghế sofa `IR2_DetailArmchairA`. Mesh pose được bake thành asset dùng `ShadowsOnly`; đèn cây đổ bóng mềm thật theo vị trí hiện tại, ghế sofa thật tạo phần bóng ghế. Không chạy thêm Animator trong gameplay. F3 lần thứ sáu hiện bóng người; F2/chuyển anomaly/disable preview ẩn bóng người và giữ bóng ghế bình thường.

Menu `Tools > Zero > Living Room > Replace Reader Shadow From Father` lấy lại pose từ controller đang gán trên Father, căn vào ghế hiện tại và thay phần trình bày IR2-06. Sau khi di chuyển ghế, chạy lại menu để căn lại mesh bóng; di chuyển đèn sẽ đổi hướng bóng theo ánh sáng thật. `Review Reader Shadow` chụp kiểm tra rồi khôi phục anomaly. Các menu chỉ chạy chủ động và không lưu scene. Ba model gia đình gốc không bị chỉnh sửa.

Bản blockout phòng khách duy nhất, dựng trực tiếp trong Unity bằng UnitySkills REST và ProBuilder. Đây là demo bố cục/hình ảnh để chỉnh sửa tiếp, chưa phải chặng campaign hoàn chỉnh.

## Lưu và thử demo

### Bổ sung IR2-07, 08, 09 và 11 — 2026-10-16

F3 hiện duyệt **10 mục: IR2-01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 11 → 01**.
F2 khôi phục phòng và đưa lần F3 tiếp theo về IR2-01. IR2-10 chưa triển khai.

- **IR2-07:** xoay riêng ấm trà 180 độ, gồm thân, vòi, nắp và tay cầm. Các tách/đĩa không di chuyển.
- **IR2-08:** khung tranh phong cảnh tường Bắc nghiêng 5 độ; tranh và khung đi cùng nhau.
- **IR2-09:** một tách tự nâng 30 cm, nghiêng như đang uống rồi trở lại đĩa; chu kỳ 6 giây, có mặt trà và gợn nhẹ.
- **IR2-11:** hình mắt từ `Assets/Game/Art/Eye.png` hiện trên LCD của điện thoại. Màn hình chập chờn ngắn theo nhịp không đều; F2 trả về LCD tối bình thường.

Bốn `AnomalyData` mới: `ScriptableObjects/Anomalies/InfinityRoom2/IR2_07.asset`, `IR2_08.asset`,
`IR2_09.asset` và `IR2_11.asset`. Chúng dùng cùng `AnomalyBehaviour`/`AnomalyData` với sáu loại đầu.
Phần bộ trà được tách thành ba mesh dùng chung trong `Prefabs/InfinityRoom2Demo/Anomalies`, giữ nguyên mesh nguồn.
Các tham số nâng/chu kỳ/nghiêng nằm ở `FloatingTeaCupAnomaly`; nhịp chập chờn ở `PhoneEyeAnomaly`.

Menu chủ động: `Tools > Zero > Living Room > Add Anomalies 07 08 09 11` nối thêm reference, không nhân bản khi chạy lại.
`Review Added Anomalies` chụp preview rồi khôi phục pose; `Fit Existing Telephone LCD` căn module theo bề mặt điện thoại hiện có.
Các menu không tự lưu scene. Bộ test F3/F2 kiểm tra vòng duyệt, reset và dọn phần trình bày của cả 10 mục.

### Test sáu anomaly đầu — 2026-10-16

Scene `InfinityRoom2_LivingRoom_Demo` có nhóm `IR2_Anomalies` và `LivingRoomAnomalyPreview`.
Vào Play, click Game View rồi nhấn **F3** để lần lượt chọn:

1. **IR2-01 — Đồng hồ chạy ngược:** hai kim thay thế quay ngược theo nấc, không xoay cả mặt số.
2. **IR2-02 — Tách trà của người thứ tư:** thêm tách, đĩa, trà, hơi nước và vệt trà xuống mép bàn.
3. **IR2-03 — Ghế quay vào tường:** ghế đơn phía Đông đổi vị trí/hướng; collider đi cùng visual.
4. **IR2-04 — Đồ sứ úp mặt:** sáu bình bị lật ngược hoặc đặt nghiêng, đáy mới vẫn khớp mặt kệ.
5. **IR2-05 — Kệ sách bị quay lưng:** bốn cụm sách được thay bằng các trang giấy quay ra ngoài và một sách mở.
6. **IR2-06 — Chiếc bóng thừa:** xuất hiện bóng người ngồi đọc sách cạnh khu đèn cây trên tường Tây.

Trong bản sáu loại ban đầu, F3 sau loại 6 quay lại loại 1; thứ tự hiện tại sau khi bổ sung được ghi ở mục trên. **F2** dọn anomaly và trả về baseline; F3 tiếp theo bắt đầu lại ở IR2-01.
HUD góc trên hiện ID/tên và số thứ tự. Chuyển loại không cộng tiến độ campaign hoặc teleport Player.
Sáu loại này không gây chết. Thoát Play, disable preview hoặc F2 đều dọn pose, vật thể và hơi nước.

Asset dùng chung ở `Prefabs/InfinityRoom2Demo/Anomalies`: một prefab tách trà,
một prefab sách đảo và một prefab sách mở; sách giống nhau là instance cùng prefab.
Data ở `ScriptableObjects/Anomalies/InfinityRoom2/IR2_01.asset` đến `IR2_06.asset`.
Hai behavior `ReverseClockAnomaly` và `LivingRoomArrangementAnomaly` kế thừa `AnomalyBehaviour`;
chưa đưa scene demo vào campaign hoặc bật Content Ready.

Menu `Tools > Zero > Living Room > Add First Six Anomalies To Demo` chỉ bổ sung khi được chủ động gọi,
từ chối dựng lại nếu đã có `IR2_Anomalies`, và không tự lưu scene. Không chạy lại để reset phòng;
dùng F2 trong Play. Kế hoạch đầy đủ: [20 anomaly Room2](../Engineering/INFINITY_ROOM2_ANOMALY_PLAN.md).

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
- Có sáu anomaly test độc lập bằng F3/F2; chưa có roster campaign, thang/cầu thang, commit trigger, NavMesh hay chuyển chặng.
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
