# IR5-07 — Tất cả mannequin nhìn theo Player

Nhóm B, không gây chết. Bảy mannequin trang trí sử dụng chung
`Prefabs/InfinityRoom5Demo/PF_IR5_DecorMannequin.prefab`, được tạo từ mannequin có
rig trong scene. Mẫu được chỉnh đứng thẳng, tay buông, chân chạm mặt sàn hoặc bục.
Mẫu người dùng đặt trùng một vị trí cũ nên vị trí đó dùng một instance prefab,
tránh hai model chồng nhau.

- F2 → bấm F3 **11 lần** để chọn IR5-07. F2/đổi anomaly/disable phục hồi pose cổ.
- Khi Player ở trong phòng, từng bone `mixamorig:Neck` xoay dần nhìn theo camera
  Player. Thân, tay, chân, vị trí và hướng gốc của model đứng yên.
- Tốc độ mặc định 90 độ/giây; giới hạn xoay ngang 180 độ và ngước/cúi 45 độ.
  Player ra khỏi phòng: cổ giữ hướng cuối; vào lại tiếp tục nhìn theo.
- `IR5_Anomalies/IR5_07_WatchingMannequins`: chỉnh `Turn Speed`, `Max Yaw`,
  `Max Pitch`, mảng `Necks`, `Local Facing Directions` và `Room Volume` trong Inspector.
- `RoomArea` dùng BoxCollider disabled làm dữ liệu vùng phòng; script kiểm tra tọa
  độ local, không dùng collider này để chặn Player hoặc tham gia raycast.
- Animator của prefab tắt để giữ pose đứng; bone cổ được điều khiển ở LateUpdate.
  Các mẫu decor cũ đang ẩn, Boss và khán giả IR5-09 giữ nguyên trạng thái.
- Data: `ScriptableObjects/Anomalies/InfinityRoom5/IR5_07.asset`.
- Model gốc có 6.915 vertex; bảy instance dùng chung mesh và vật liệu.

Kiểm tra trong Editor: bảy instance dùng đúng prefab; bảy cổ hướng theo camera,
thân đứng yên; chân mesh chạm mặt sàn/bục; năm trường hợp kiểm tra logic quay,
vùng phòng, reset, model ẩn/mất và reload đều đạt. Chưa chạy PlayMode Test Runner.
Người dùng lưu scene bằng Ctrl+S; tool authoring tạm được xóa sau khi gán reference.
