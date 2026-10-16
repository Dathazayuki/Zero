# Backroom chase / replay

## Cấu hình thủ công

1. Mở `BackRoomScene`, đợi Unity compile. Chạy **Tools > Zero > Setup Backroom Monster Chase Gameplay** một lần.
2. Menu không save scene, không bake, không chạy Scene Builder. Có Undo. Chạy lại giữ các reference và transform đã chỉnh; không rebuild phòng.
3. Tool nhận `HorrorGirrl` qua Animator Tattered Wraith, giữ world pose và đưa dưới BackRoomRoot. `MonsterSpawnPoint` lấy vị trí hiện tại của model, không tự chọn tọa độ sâu trong mê cung. Hãy đặt điểm này trên sàn, sâu trong mê cung, cách điểm hồi sinh đủ xa để Player có thời gian phản ứng.
4. Kiểm tra `ChaseRespawnPoint`: mặc định cách ArrivalAnchor 1.5m vào Backroom, hướng ngược forward của ArrivalAnchor. Đặt nó trên sàn trống, không trong tường/cửa hoặc trigger start. Không đặt ở cabin vì cabin có thể đã đóng.
5. `BackroomChaseStart`: mặc định cách ArrivalAnchor 3.5m. Di chuyển để phủ toàn lối đi ngay sau điểm hồi sinh; Player bước tiếp phải chạm trigger. Không overlap điểm hồi sinh hoặc exit.
6. `BackroomChaseEscape`: trigger độc lập tại marker GT_Exit_Door_Zone; marker gốc inactive vẫn giữ nguyên. Chỉnh volume phủ đúng cửa thoát, không xuyên hành lang bên cạnh.
7. NavMeshSurface thu collider dưới BackRoomRoot. Kiểm tra Agent Type trùng NavMeshAgent, collider sàn/tường, chiều cao/radius agent và đường đi liên tục. Khuyến nghị bake thủ công trước khi chạy. Nếu chưa có data, runtime build một lần trong cabin sau khi căn scene; không build mỗi replay.
8. Animator giữ controller cũ, state mặc định `Base Layer.HorrorGirl`. Nếu đổi controller, sửa Chase State tương ứng. Runtime tắt root motion và dùng NavMeshAgent di chuyển. Kiểm tra kích thước agent so với model scale 2 hiện tại.
9. Ctrl+S sau khi tự xác nhận các thay đổi.

## Luồng

Đi qua trigger -> chase -> bắt trong 1.4m và không bị tường che -> khóa input/nhìn quái vật/rung -> blackout -> teleport về điểm hồi sinh, reset monster -> fade sáng -> đi qua trigger để chơi lại.

Đến exit khi đang chase -> dừng quái vật. Không tải Office, không reset tầng 0, không đổi Time.timeScale. Tốc độ chase mặc định 4.6m/s; chỉnh sau khi thử đường chạy bằng tốc độ sprint thực tế của Player.

Canvas blackout được tạo trong Play Mode nếu chưa gán, không ghi vào scene. PlayerDeathController cũ không được dùng cho replay này.

## Checklist Play Mode

- Đi từ elevator: BACKROOM_CHASE_READY phải xuất hiện sau khi scene đã align; bước qua start chỉ có một BACKROOM_CHASE_STARTED.
- Đứng gần quái vật ở hai phía tường: không bị bắt xuyên tường.
- Bị bắt: BACKROOM_PLAYER_CAUGHT, không thấy teleport trước blackout, rồi BACKROOM_REPLAY_READY; camera/FOV/input trở lại bình thường.
- Lặp ba lần: quái vật về đúng spawn, trigger rearm, không bake lại và không duplicate chase.
- Đi qua exit: BACKROOM_PLAYER_ESCAPED, quái vật dừng cả di chuyển lẫn animation.
- Nếu BACKROOM_CHASE_NAVIGATION_INVALID: kiểm tra spawn/respawn cách NavMesh dưới 2m, agent type và path nối hai điểm. Không coi runtime đã sẵn sàng khi có log này.
- Chạy menu lần hai: không tạo thêm hệ thống/trigger, không di chuyển các marker đã chỉnh.

Chưa chạy menu setup hoặc Play Mode tự động để bảo vệ scene đang có thay đổi chưa lưu. Cần kiểm chứng các vị trí/collider trên map thật sau khi cấu hình.
