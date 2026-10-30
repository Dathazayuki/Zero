# Hotel Corridor — Lobby

Scene: `Assets/Game/Scenes/Demos/Lobby.unity`.

Prefab bố cục: `Assets/Game/Prefabs/HotelCorridor/PF_HotelCorridor.prefab`.

## Bố cục

Hành lang trong Lobby rộng 8,4 m, dài 35 m, cao 4,4 m. Nhìn từ khu thang về cuối hành lang:

| Cửa | Vị trí | Trang trí nhận diện |
| --- | --- | --- |
| IR1 | Bên trái, gần đầu | Tranh có khung, đèn tranh, bàn console |
| IR2 | Bên phải, gần đầu | Tông ấm, tranh phong cảnh, ghế bành, bình sứ |
| IR4 | Bên trái, phía xa | Gạch xanh, kệ nồi, xe đẩy bếp |
| IR3 | Bên phải, phía xa | Tủ hồ sơ, hộp lưu trữ, xe sách |
| IR5 | Chính giữa cuối hành lang | Rèm đỏ kéo sang hai bên, bàn và đèn Cabaret |

Cả năm cửa là nested instance của `Assets/Game/Prefabs/Floor/Door_Target.prefab`.
Các đoạn sàn, tường, cửa, phào cột, đèn và khung tranh dùng prefab chung trong
`Prefabs/HotelCorridor`.

## Tiếp giáp PF_FloorSegment

- Giữ instance `PF_FloorSegment` hiện có trong Lobby.
- Root `HotelCorridor`: world position `(0.7159548, 0.185, 6.98929)`.
- Đầu hành lang nối sàn PF tại khoảng world Z `-14.01071`.
- Mặt collider của hai bên cùng world Y `0.185`; có nẹp nối mỏng ở ngưỡng.
- Nhịp đệm `*_Entry` dài 7 m nằm trước Bay_0, gồm sàn, trần, hai vách kín,
  phào cột, đèn tường và đèn trần; tái sử dụng các module prefab hiện có.
  Cụm cửa IR1–IR2 ở world Z `-3.51071`. Hành lang cũ và năm room root
  được dời cùng 7 m theo +Z; `PF_FloorSegment` và `HC_Connection` giữ nguyên
  vị trí tiếp giáp. Đây là chỉnh sửa instance trong Lobby.
- Đã kiểm tra 11 điểm mặt sàn từ Z `-16` đến `13.5`: tất cả có sàn tại Y `0.185`.
- Dải giữa rộng 2,5 m không có collider đặc cản đường. Trigger tay nắm cửa IR5
  nằm ở cuối dải kiểm tra.

## Điểm ghép nội dung phòng

`HotelCorridor/HC_RoomAnchors` chứa năm connector có sàn, vách và trần nhận cửa.
Trong mỗi connector, `HC_RoomAttachAnchor_IR#` nằm tại ngưỡng cửa; trục Z hướng
ra khỏi hành lang để căn entrance của phòng tương ứng.

Trong bản Lobby đang ghép, IR2–IR5 được căn theo ngưỡng cửa của hành lang.
Các marker `IR#_LobbyDock` nằm trên root nội dung phòng để xác định điểm ghép.
Những vách khoang nhận cửa trùng với phòng đã ghép được tắt.

## Chuẩn trần và cửa IR2–IR5

- Chiều cao thông thủy: **4,4 m** tính từ mặt sàn, bằng hành lang.
- Với mặt sàn Lobby Y `0.185`, mặt dưới trần ở Y `4.585`.
- `Assets/Game/Prefabs/Floor/PF_IR_EntranceDoor.prefab` bọc một nested instance
  của `Door_Target.prefab`. Door_Target giữ cùng scale `1.25` với các cửa Lobby;
  khung ngoài khoảng `1,75 × 3,08 m`.
- Lỗ tường rộng `1,6 m`, đầu tường trên cửa bắt đầu tại cao độ tương đối `3,02 m`.
  Mép khung che phần tiếp giáp để kín cả phía phòng và hành lang.
- Trong Lobby, cửa chung nằm dưới
  `HC_DoorPortals/<khu phòng>/IR#_Entrance`. Cửa hành lang cũ chồng tại cùng ngưỡng
  được tắt; mỗi ngưỡng có một cánh cửa hoạt động.
- Trong bốn scene demo gốc, `IR#_Entrance` nằm dưới root của phòng.
- Đèn và chi tiết gắn trần được nâng theo trần. Hai mesh `IR5_AlignedTrim_*`
  trong `Models/HotelCorridor` đưa phào Cabaret lên cao và chừa ô cửa mới.

IR2–IR4 đang được ẩn trong bản authoring Lobby; cửa nằm ngoài root nội dung phòng
nên vẫn hiển thị. Việc chọn phòng và điều phối các demo gameplay chưa được thay đổi.

IR5-17 sử dụng chính cánh cửa chung qua `CabaretRitualDoor`: khóa cửa sẽ tạm dừng
`PushDoorController`, mở khóa/reset sẽ trả quyền điều khiển và tư thế mở. Adapter
chấp nhận một cánh và vẫn hỗ trợ dữ liệu hai cánh cũ. Năm EditMode test liên quan
đã pass; Code Policy báo PASS, không tăng baseline. Chưa chạy PlayMode cho lần sửa này.

## Chỉnh sửa

Hierarchy được chia thành `HC_Architecture`, `HC_DoorPortals`, `HC_Decor`,
`HC_Lighting`, `HC_Connection` và `HC_RoomAnchors`.

Sửa module prefab khi muốn áp dụng cho tất cả vị trí. Biển IR1–IR5 và vật dụng
đặc trưng nằm trên instance của từng khu. `HC_LocalReflection` là probe realtime
128 px cập nhật lúc khởi tạo; không cập nhật liên tục mỗi frame.

Lưu scene thủ công trong Unity bằng Ctrl+S. Chưa chạy PlayMode cho bản dựng này.

Ảnh: `Lobby_Corridor_Preview.png` và `Lobby_Corridor_Connection.png` trong thư mục này.
