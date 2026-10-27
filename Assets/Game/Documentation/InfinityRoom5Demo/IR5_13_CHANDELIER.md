# IR5-13 — Bàn tay thả đèn chùm (nhóm D)

F3 mục 9, F2 khôi phục phòng normal. Can Kill Player bật, death style FreezeBlackout.

- Giữ nguyên bố trí `IR5V2_DetailPass/IR5V2_Chandelier/Hand` do người dùng đặt. Hand chỉ hiện khi anomaly bật; không sửa asset model, rig hoặc animation. Giữ trạng thái bật/tắt Animator do Inspector cấu hình, để animation mặc định chạy khi tay xuất hiện; không ép tạm dừng Animator. Trả trạng thái khi reset.
- Đèn rung nhẹ. Khi Player vào `IR5_Anomalies/IR5_13_Chandelier/ApproachVolume`, rung mạnh 1,1 giây rồi rơi tại chỗ trong 1,05 giây, tăng tốc theo thời gian. Rời vùng không hủy cú rơi đã kích hoạt; né sang bên sẽ sống.
- Khi đèn rung hoặc đang rơi nhưng chưa trúng: không điều khiển camera, không khóa di chuyển. Né được thì không có cutscene hay chuyển trả camera. Bàn tay ẩn từ lúc bắt đầu rơi.
- Quét bounds từ vị trí trước tới sau trong mỗi bước vật lý để tránh bỏ sót va chạm. Chỉ khi đã va trúng vùng đầu mới khóa Player và bắt đầu cảnh bắt: camera ngước lên mượt trong `Look Seconds` (0,35 giây), đèn dập xuống thêm rồi gọi `TryBeginFreezeDeath(this, 0f)` cắt đen ngay tại cú dập cuối, không thêm freeze 1–2 giây. Góc nhìn dùng pitch -85° theo hướng thân, không dùng trục up nằm ngang dễ gây lật.
- F2/đổi anomaly/disable: khôi phục đèn, ánh sáng, camera và ẩn tay. Reset gọi hai lần vẫn an toàn.

Inspector `CabaretChandelierAnomaly`: `Warning Seconds`, `Fall Seconds`, `Look Seconds`, `Floor Height`. Chỉnh kích thước/vị trí `ApproachVolume` để đổi vùng kích hoạt; `StrikeVolume` bám kích thước đèn. Dùng lại luật một lần cảnh báo → lao/rơi → kết thúc của `ArchiveCabinetCrushRules`, không thêm hệ thống hazard chung.

Không tự lưu scene; người dùng Ctrl+S trước khi chạy test scene. Không thay đổi campaign/build settings hoặc cài package.

Regression bao gồm: quét va chạm, reset thiếu reference, hướng camera không roll với bốn hướng thân; PlayMode kiểm tra camera không đổi khi rung/né, chỉ va trúng mới bắt đầu cutscene, cú dập cuối cắt đen, hồi sinh trả camera thẳng. Tool setup tạm đã xóa.
