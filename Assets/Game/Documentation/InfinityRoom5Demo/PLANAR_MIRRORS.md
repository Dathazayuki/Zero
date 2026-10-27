# Ba gương phản chiếu trong Crimson Cabaret

`IR5V2_Mirror0`, `IR5V2_Mirror1`, `IR5V2_Mirror2` dùng lại
`Zero.EnvironmentLoop.PlanarMirrorRenderer` như `MirrorRoot` trong
`Assets/Game/Prefabs/Environment/Room.prefab`.

- Mỗi gương có `MirrorPlane`, `MirrorCamera` riêng và reference camera Player rõ ràng.
- `MirrorPlane` sát phía trong phòng của mặt kính, pháp tuyến hướng -X.
- Giữ nguyên mesh kính, khung và vị trí gương.
- Material chung: `Art/InfinityRoom5/M_IR5_PlanarMirror.mat`, shader
  `Zero/Planar Mirror URP`, chiếu phản xạ theo vị trí thật (không dùng UV mesh cũ).
- Sao chép cấu hình từ MirrorRoot: HDR, khớp độ phân giải camera nguồn, giới hạn
  tối đa 2048 px mỗi chiều, clip offset 0.03, tối đa 30 m, chỉ render khi kính được nhìn thấy.
- Mặt kính ở layer `MirrorSurface`; reflection mask dựa trên MirrorRoot nhưng bỏ
  layer này để ba gương không render hình ảnh gương lẫn nhau.
- Frame vẫn ở layer cũ và tiếp tục xuất hiện trong phản chiếu.

Camera phụ tắt trong bản author; script sẽ tạo RenderTexture riêng và cập nhật camera
khi vào Play Mode. Không cần bake. Nếu muốn giảm tải ba camera, có thể tắt
`Match Source Resolution` và đặt Texture Width/Height trên component mỗi gương.

Đã kiểm tra các reference và mặt phẳng trong Editor, chưa chạy Play Mode.
Scene cần người dùng lưu Ctrl+S. Tool setup tạm được xóa sau khi gán.
