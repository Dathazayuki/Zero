# IR5-16 — Cú búng tay của The Smiling Host

## Chơi trong demo

- Mở `InfinityRoom5_CrimsonCabaret_Demo`, Play rồi bấm F3 tới **7/7 — IR5-16**. F2 trả phòng bình thường.
- Boss ngồi gần phía trước sân khấu. Tới trong **3 m** và có đường nhìn không bị che, hắn đứng dậy rồi búng tay. Không chỉnh các state/clip nguồn của Boss trong gương.
- Hố chốt dưới chân Player đúng lúc búng, báo trước **1,2 giây**, bán kính **0,95 m**, mở **2 giây**. Rời vòng đen trước lúc mở để sống. Đi ngược vào hố khi còn mở vẫn bị bắt.
- Trúng hố: khóa điều khiển, camera rơi nhanh vào ống tối trong **1,15 giây**, cắt đen ngay rồi dùng `PlayerDeathController` hồi sinh về điểm đầu demo. Không cắt hay xóa collider sàn chung.
- F2/đổi anomaly/disable khi chưa giao quyền cho hệ thống chết: hủy hiệu ứng, trả material sàn và camera/input. Boss và ghế chỉ hiện ở IR5-16.

## Chỉnh Inspector

Chọn `IR5_Anomalies/IR5_16_HostSnap` → `CabaretBlackHoleAnomaly`: `Approach Distance`, `Stand Seconds`, `Snap Clip Seconds`, `Warning Seconds`, `Open Seconds`, `Fall Seconds`, `Hole Radius`, `Fall Gravity`.

`BlackHole` → `CabaretBlackHoleView` chứa danh sách mặt sàn cần cắt hình ảnh và collider sàn được phép mở hố. Nếu điểm dưới chân không có đủ mặt sàn cho bán kính hố thì hủy đòn, không kéo Player xuyên tường hay đồ đạc.

## Root motion khi đứng dậy

IR5-16 bật `Apply Root Motion` trong encounter, không ép vị trí hoặc xoay thân mỗi frame. Boss đi theo chuyển động của clip; chỉ trở về transform đã author khi F2/reset. Thiết lập root motion ban đầu cũng được khôi phục khi reset.

State `SitToStand` dùng bản sao `Assets/Game/MixamoAnimation/Host/IR5_16_SitToStand.anim`: giữ nguyên keyframe và chuyển động XZ; bật **Root Transform Position (Y) → Bake Into Pose** để giữ chuyển động đứng lên trong pose, không cộng chiều cao ấy lần nữa vào root khi sang clip búng tay. Clip `SitToStand.anim` gốc không bị sửa. Không dùng ground offset hoặc ép position Y trong code.

Regression sau sửa: 3/3 EditMode (gồm đánh giá Animator thật qua chuỗi ngồi → đứng → búng tay) và 1/1 PlayMode (kiểm tra root XZ có dịch chuyển, root Y không bị cộng đôi, F2 trả đúng transform, né/rơi chết/hồi sinh). Tổng 4/4 PASS cho lượt sửa root motion.

## Asset

- Clip: `Assets/Game/MixamoAnimation/Host/StandingFingerSnap.anim`.
- Humanoid, 60 fps, dài 2,2 giây; không loop, không dịch chuyển root.
- Dựa trên formal idle hiện tại: đứng thẳng, hai chân chụm, hai tay sau lưng. Tay phải nâng cao bên cạnh đầu, búng một lần rồi hạ về sau lưng; tay trái và dáng đứng được giữ.
- Bản trước khi nâng tay được giữ tại `Assets/Game/MixamoAnimation/Host/StandingFingerSnap_BeforeRaisedHand.anim`.
- Không sửa clip `IR5_12_HostIdle.anim` hoặc controller của IR5-12.

## Mốc chuyển động

| Thời gian | Chuyển động |
|---|---|
| 0–0,55 s | Đưa tay phải từ sau lưng lên cao cạnh đầu. |
| 0,55–1,02 s | Chuẩn bị ngón tay và giữ nhịp ngắn. |
| 1,02–1,10 s | Búng ngón tay một lần. |
| 1,10–1,30 s | Giữ dáng sau cú búng. |
| 1,30–2,20 s | Trả tay về dáng idle. |

Hố đen đồng bộ tại **1,10 s** của state `StandingFingerSnap`, không dùng Animation Event. `IR5_16_HostSnap.controller` dùng các state `Sitting`, `SitToStand`, `StandingFingerSnap`, `Idle`; controller được sao chép từ Animator đã author rồi bổ sung state búng tay, không sửa controller nguồn.

## Kiểm tra và phạm vi

Shader sàn cắt hố giữ đầy đủ properties và lighting variants của URP/Lit đang cài. Regression render `CabaretPortalLightingTests` so màu sàn ngoài hố trước/sau (không đổi) và xác nhận tâm hố vẫn bị cắt. Không sửa material gốc hoặc project rendering settings.

Preview native Unity: `HostFingerSnap_Raised.png`, `IR5_16_SeatedPreview.png`, `IR5_16_HolePreview.png`. Các tool setup/author chỉ chạy từ menu và được xóa sau khi hoàn tất; không có code tự động rebuild/lưu scene.

Kiểm thử: 4/4 EditMode cho rules, 2/2 EditMode cho phạm vi bắt/reset, 1/1 PlayMode trong scene thật cho né hố, F2 khôi phục, rơi chết và hồi sinh; tổng 7/7 PASS. Hai shader không có lỗi compile. Lượt PlayMode dùng Player demo và các state Animator đã gán, không thay clip của người dùng.

Kế hoạch encounter nằm trong [INFINITY_ROOM5_ANOMALY_PLAN.md](INFINITY_ROOM5_ANOMALY_PLAN.md), mục IR5-16. Đây là demo độc lập, không thay đổi campaign readiness hay Build Settings.
