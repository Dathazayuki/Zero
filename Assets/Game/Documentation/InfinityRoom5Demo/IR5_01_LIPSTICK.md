# IR5-01 — Dấu son trên các ly

Scene `InfinityRoom5_CrimsonCabaret_Demo`, root `IR5_Anomalies` dùng lại
`LivingRoomAnomalyPreview` và `LivingRoomArrangementAnomaly` như các phòng trước.
Data: `Assets/Game/ScriptableObjects/Anomalies/InfinityRoom5/IR5_01.asset`.

- Play Mode: F3 lần đầu bật IR5-01; F2 khôi phục phòng bình thường. Các lần F3 tiếp
  chuyển qua IR5-05, IR5-06, IR5-09 và IR5-10 rồi quay vòng.
- 12 ly dưới `IR5V2_DetailPass/IR5_BarDrinkware` có các nhóm `IR5_01_KissMarks`;
  xen kẽ 2 và 3 dấu mỗi ly, tổng 30 dấu. Các nhóm tắt trong trạng thái normal.
- Dấu dùng chung mesh cong `Art/InfinityRoom5/IR5_LipstickMark.asset` và material
  `M_IR5_Lipstick.mat`, lấy texture gốc `Assets/Game/Art/Kiss.png`.
- Shader `Zero/Cabaret Lipstick` loại nền tối bằng màu đỏ để tránh mảng chữ nhật
  quanh dấu son; không sửa file ảnh nguồn.
- Chỉnh vị trí/góc từng dấu trong nhóm con của ly. Khi di chuyển ly, dấu đi cùng ly.
- Ảnh kiểm tra Edit Mode: [IR5_01_LipstickPreview.png](IR5_01_LipstickPreview.png).

Đã kiểm tra shader và hình ảnh cận ly. Chưa chạy Play Mode; lưu scene thủ công
trước khi người dùng kiểm tra F2/F3. Tool setup tạm đã được xóa.
