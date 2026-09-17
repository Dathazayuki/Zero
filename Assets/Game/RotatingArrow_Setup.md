# Hướng Dẫn Cấu Hình Anomaly Rotating Arrow (A-19)

Anomaly **Rotating Arrow** (Mã hiệu: **A-19**): Mũi tên thật bị ẩn với mắt thường và chỉ nhìn thấy qua gương (dùng layer `MirrorReflectionOnly`), thay vào đó là mũi tên giả xoay tròn liên tục không ngừng.

---

## 1. Thông Tin ScriptableObject A-19
- **File Asset**: `Assets/Game/ScriptableObjects/Anomalies/A19_RotatingArrow.asset`
- **ID**: `arrow.rotating`
- **Display Name**: `Rotating Arrow`
- **Type**: `ObjectChange` (0)
- **Pacing Tier**: `MinorAnomaly` (1)
- **Difficulty**: `2`
- **Spawn Weight**: `1.0`
- **Can Kill Player**: `false`
- **Description**: *"The direction arrow rotates endlessly in the room and can only be seen pointing correctly in the mirror."*

---

## 2. Các Bước Thiết Lập Thủ Công Trên Inspector

1. **Tạo Mũi Tên Giả (Fake Arrow)**:
   - Trong Room, tìm GameObject mũi tên thật (con của `CorrectArrow` hoặc được gán trong `CorrectArrowController`).
   - Duplicate (`Ctrl + D`) GameObject đó, đổi tên thành `[Fake]CorrectArrow`.
   - Xóa bỏ mọi Component `Collider` hoặc script không liên quan trên mũi tên giả (chỉ giữ `MeshFilter` + `MeshRenderer`).
   - Đặt trạng thái ban đầu là tắt (`SetActive(false)`).
2. **Gắn Component Anomaly**:
   - Tạo một GameObject con trong Room đặt tên là `Anomaly_RotatingArrow`.
   - Thêm component `RotatingArrowAnomaly`.
   - **Data**: Kéo `Assets/Game/ScriptableObjects/Anomalies/A19_RotatingArrow.asset` vào.
   - **Arrow Controller**: Kéo `CorrectArrowController` của phòng vào.
   - **Planar Mirror**: Kéo `PlanarMirrorRenderer` của gương vào.
   - **Fake Arrow Transform**: Kéo GameObject mũi tên giả vừa tạo ở bước 1 vào.
   - **Rotation Axis**: `(0, 0, 1)` (trục Z).
   - **Rotation Speed**: `360` (độ/giây).
3. **Đăng Ký Slot**:
   - Chọn GameObject chứa `RoomAnomalyController`.
   - Trong mảng **`Anomaly Slots`**, tăng size và kéo `Anomaly_RotatingArrow` vào slot mới.
