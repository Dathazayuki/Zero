# IR5-10 — Những bàn tay sau rèm

F3 lượt thứ năm chọn IR5-10 trong `InfinityRoom5_CrimsonCabaret_Demo`.
Thứ tự hiện tại: IR5-01 → IR5-05 → IR5-06 → IR5-09 → IR5-10. F2 khôi phục phòng normal.

- Mười hai bàn tay dùng chung `PF_IR5_CurtainHand.prefab`: mỗi bên rèm có bốn tay
  dọc mép trong và hai tay ở gấu rèm.
- Prefab lấy đúng mesh/material và pose từ model `Hand` được người dùng đặt trong scene.
  Mesh đã bake từ pose đó thành asset tĩnh, không chạy 12 Animator. Bản `Hand` gốc
  được giữ nguyên model/transform và tắt active để không xuất hiện như đạo cụ rời.
- Hai cụm `LeftHands`/`RightHands` ở `IR5_Anomalies/IR5_10_CurtainHands`.
  Khi Player tới gần một cụm trong 1.8 m theo mặt phẳng ngang, cụm đó rụt về phía
  sau và ra ngoài mép rèm trong 0.65 giây, rồi ẩn. Bên còn lại vẫn chờ riêng.
- Chỉnh `Approach Distance`, `Retreat Seconds`, `Retreat Offsets` trên
  `CabaretCurtainHandsAnomaly`; chỉnh vị trí từng instance trực tiếp trong scene.
- Không gây damage/death. F2, đổi anomaly hoặc disable đều trả pose ban đầu và ẩn tay.
  Bật lại IR5-10 sẽ cho hai cụm xuất hiện lại từ đầu.
- Không thay mesh/material của rèm. Không thêm collider cho các tay.

Data: `ScriptableObjects/Anomalies/InfinityRoom5/IR5_10.asset`.
Ảnh kiểm tra Edit Mode: [IR5_10_HandsPreview.png](IR5_10_HandsPreview.png).
Có test reset/disable và reactivation; chưa chạy Test Runner hay Play Mode.
Tool setup tạm được xóa sau khi gán, scene lưu thủ công bằng Ctrl+S.
