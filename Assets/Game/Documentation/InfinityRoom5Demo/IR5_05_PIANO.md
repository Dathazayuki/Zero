# IR5-05 — Piano tự biểu diễn

`IR5V2_Piano/IndividualKeys` chứa 36 phím trắng và 26 phím đen. Mỗi phím có pivot
riêng ở phía sau và object con `KeyMesh` dùng mesh cube gốc Unity. Phần thân đen
dùng mesh dẫn xuất đã bỏ phím đen gộp; renderer phím trắng gộp cũ được tắt.
Mesh nguồn vẫn giữ nguyên. Nắp, thân, ghế và collider đàn giữ vị trí đang có.

`IR5_Anomalies/IR5_05_SelfPlayingPiano` dùng `CabaretPianoAnomaly` và data
`ScriptableObjects/Anomalies/InfinityRoom5/IR5_05.asset`.

- F3 lần đầu: IR5-01; lần hai: IR5-05. F2 dừng âm thanh và trả phím về pose ban đầu.
- Khi Player tới trong khoảng **Activation Distance** (mặc định 2.8 m), đàn chơi
  một câu 16 nốt, một lần cho mỗi lượt kích hoạt anomaly.
- Nhịp nhấn phím đồng bộ theo thời gian AudioSource; không mở nắp đàn.
- **Press Angle** chỉnh độ nhấn phím trong Inspector.
- Clip `Art/InfinityRoom5/IR5_PianoPhrase.wav` là âm tổng hợp do tool nội bộ tạo,
  không tải từ bên ngoài. Audio đi qua Sfx mixer. Có thể thay clip, nhưng để đồng bộ
  phím cần giữ chuỗi 16 nốt với nhịp 0.36 giây/nốt hoặc chỉnh phần trình bày tương ứng.
- Đổi anomaly, F2 hoặc disable đều dừng audio và phục hồi phím đang nhấn.

Chưa chạy Play Mode. Tool setup chỉ chạy thủ công và đã xóa sau khi gán scene;
người dùng lưu scene bằng Ctrl+S trước khi thử F2/F3.
