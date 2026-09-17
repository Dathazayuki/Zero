# Hướng Dẫn Cấu Hình Anomaly Speed Time (A-20)

Anomaly **Speed Time** (Mã hiệu: **A-20**): Kim giờ và kim giây (cùng kim phút nếu có) của đồng hồ trong phòng sẽ xoay với tốc độ chóng mặt.

---

## 1. Thông Tin ScriptableObject A-20
- **File Asset**: `Assets/Game/ScriptableObjects/Anomalies/A20_SpeedTime.asset`
- **ID**: `clock.speed_time`
- **Display Name**: `Speed Time`
- **Type**: `ObjectChange` (0)
- **Pacing Tier**: `MinorAnomaly` (1)
- **Difficulty**: `1`
- **Spawn Weight**: `1.0`
- **Can Kill Player**: `false`
- **Description**: *"The hour and second hands of the clock spin at a dizzying speed, as if time itself has gone berserk."*

---

## 2. Thông Số Xoay (Chóng Mặt)
- **Kim giây (`secondHandSpeed`)**: `2160` độ/giây (tương đương **6 vòng/giây**).
- **Kim giờ (`hourHandSpeed`)**: `360` độ/giây (tương đương **1 vòng/giây**).
- **Kim phút (`minuteHandSpeed`)**: `720` độ/giây (tương đương **2 vòng/giây**).
- **Trục xoay (`rotationAxis`)**: `(0, 0, 1)` (trục Z local). Có thể đổi sang trục khác nếu pivot model đồng hồ của bạn dùng trục khác.
- **Chiều xoay (`rotateClockwise`)**: `true` (quay thuận chiều kim đồng hồ).

---

## 3. Các Bước Thiết Lập Thủ Công Trên Inspector

1. **Chuẩn bị kim đồng hồ**:
   - Đảm bảo đồng hồ trong phòng có các Transform riêng biệt cho kim giờ (`HourHand`) và kim giây (`SecondHand`).
2. **Gắn Component**:
   - Tạo một GameObject con trong phòng, đặt tên là `Anomaly_SpeedTime`.
   - Thêm component `SpeedTimeAnomaly`.
   - **Data**: Kéo asset `Assets/Game/ScriptableObjects/Anomalies/A20_SpeedTime.asset` vào ô Data.
   - **Hour Hand**: Kéo Transform kim giờ vào.
   - **Second Hand**: Kéo Transform kim giây vào.
   - **Minute Hand** *(Tùy chọn)*: Kéo kim phút vào nếu có.
   - **Fast Tick Audio** *(Tùy chọn)*: Kéo AudioSource phát tiếng tíc tắc nếu muốn.
3. **Đăng ký vào RoomAnomalyController**:
   - Chọn GameObject chứa `RoomAnomalyController`.
   - Trong mảng **`Anomaly Slots`**, thêm `Anomaly_SpeedTime` vào slot mới.
