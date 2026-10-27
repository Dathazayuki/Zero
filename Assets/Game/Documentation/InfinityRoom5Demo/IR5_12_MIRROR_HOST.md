# IR5-12 — Boss chỉ thấy trong gương

`IR5_Anomalies/IR5_12_MirrorHost/ReflectionOnlyHost` là bản sao TheSmilingHost
đứng ở `(-2.75, 0, -0.6)` sau quầy bar, quay mặt ra phòng (+X).
Model gốc vẫn giữ nguyên transform/controller và được ẩn khỏi phần chơi.

- Toàn bộ hierarchy Boss, gồm mặt và dải nhiễu, thuộc `MirrorReflectionOnly`.
- Camera Player không render layer này; ba PlanarMirrorRenderer vẫn bao gồm layer đó.
- Không có collider và không đổ bóng ngoài thế giới thường.
- Animator dùng avatar sẵn của Host, controller `Animation/IR5_12_MirrorHost.controller`
  chỉ có state Idle. Clip lặp `IR5_12_HostIdle.anim` đã author thành đứng thẳng,
  hai chân chụm, mũi giày cùng hướng và hai tay vòng ra sau lưng; thở nhẹ theo chu kỳ
  4 giây. Giữ nguyên reference clip nên controller tự dùng pose mới.
  Bản cũ: `Animation/IR5_12_HostIdle_BeforeFormalPose.anim`.
  Controller Host gốc chỉ có Sitting nên không dùng nó cho anomaly này.
- Root motion tắt để Boss đứng yên. Animator Always Animate để tiếp tục idle khi
  bị camera trực tiếp loại khỏi hình, nhưng vẫn nhìn thấy trong camera phản chiếu.
- Dùng `LivingRoomArrangementAnomaly` và AnomalyData riêng như các anomaly trước;
  F2/đổi anomaly/disable đều ẩn Boss. Không truy đuổi và không gây chết.

F3 thứ sáu: IR5-01 → IR5-05 → IR5-06 → IR5-09 → IR5-10 → IR5-12.
Không cần lại gần quầy để bật; anomaly hiện trong gương ngay khi được chọn.
Khung quầy vẫn che phần cơ thể Boss theo hình học thật trong phản chiếu.

Đã kiểm tra reference/layer và render pose mới từ Animator trong Editor, cả
[phía trước](HostFormalIdle_Front.png) và [phía sau](HostFormalIdle_Back.png).
Chưa kiểm tra hình phản chiếu hoặc idle trong Play Mode. Scene lưu thủ công bằng
Ctrl+S; tool setup/pose tạm được xóa sau khi gán. Clip pose mới đã lưu thành asset.
