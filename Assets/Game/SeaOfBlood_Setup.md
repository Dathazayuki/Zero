# Sea of Blood — cấu hình Unity

Runtime đã có; cần gán prefab và asset trong Editor. Không chạy Scene Builder.

1. Chờ Unity compile, chọn **Tools > Zero > Sea of Blood > Create Missing Assets**.
   Menu tạo `A18_SeaOfBlood.asset` (Environment, Major, difficulty 3, weight 0.65, lethal)
   và `M_SeaOfBlood.mat` nếu chưa có; không ghi đè asset cũ hoặc lưu scene/prefab.
2. Mở **Room.prefab** đang được PF_FloorSegment sử dụng. Tạo root `SeaOfBlood`
   dưới AnomalyContent và thêm **SeaOfBloodAnomaly** lên root. Root để active.
3. Tạo các child cùng cấp: BloodStartPoint, BloodEndPoint, BloodSurface,
   EntryTrigger, DeathPresentation, AudioSource và RiseParticles (hai mục cuối tùy chọn).
   Không đặt Start/End làm con của BloodSurface vì chúng phải đứng yên khi surface dâng.
4. BloodStartPoint thấp hơn sàn 0.15m; EndPoint ở trên đầu Player/gần trần.
   BloodSurface dùng plane nằm ngang + MeshRenderer, phủ vùng cần ngập, bỏ collider.
   Gán material vừa tạo. Với phòng không hình chữ nhật, dùng mesh theo footprint để tránh xuyên tường.
5. Material: gán Noise texture grayscale và Normal Map, cả hai Wrap Mode Repeat;
   Normal Map phải import theo Texture Type Normal map. Shader cuộn hai lớp, Fresnel nhẹ,
   distortion là UV bề mặt, không phải refraction camera. Không cần camera phản chiếu.
6. EntryTrigger: BoxCollider Is Trigger + SeaOfBloodStartTrigger. Đặt trong cửa phòng,
   tránh overlap commit trigger. Dùng layer có thể tương tác với layer Player trong Physics.
   Giữ GameObject active; runtime quản lý enabled của collider/component.
   Nếu cặp collider không tạo callback, thêm Rigidbody kinematic, tắt Use Gravity trên EntryTrigger.
7. DeathPresentation: AnomalyDeathPresentation, **Motion Style = Suffocation**;
   gán choking audio nếu có, bỏ trống FocusTarget và JumpscareCameraAnchor.
8. Gán tất cả reference vào SeaOfBloodAnomaly, Data = A18_SeaOfBlood.
   Escape Duration = 12, Danger Duration = 3. Rise Audio dùng loop, Play On Awake tắt.
   Particle Play On Awake tắt, không collision; parent vào surface nếu cần đi theo mặt máu.
9. Thêm component anomaly vào `RoomAnomalyController.anomalySlots`. Apply prefab thủ công.
   Manager tự bind Player/camera và owner floor. Player phải có PlayerDeathController
   với blackout, blood overlay, camera và world references như các death hiện có.
10. Pacing Major chỉ cho xuất hiện từ progress 3; difficulty tối đa phải cho phép 3.
    Test bằng công cụ force anomaly hiện có nếu cần, không thêm phím tắt mới.

## Kiểm tra

- Chọn **Tools > Zero > Sea of Blood > Validate Lifecycle (Isolated)** ngoài Play Mode.
  Validator tạo preview scene tạm trong bộ nhớ rồi đóng, không mở/lưu scene game.
  Có warning death rejected có chủ đích ở test CanKillPlayer=false.
- Play Mode: reserve không dâng; vào phòng current chỉ bắt đầu một lần; mặt máu đạt End
  sau 12 giây gameplay; ba giây cuối breathing/stumble tăng nhưng tốc độ không đổi.
- Commit Up/Down, kể cả chọn sai, trước timeout: máu/audio/particle dừng. Nếu commit và
  deadline cùng frame, kiểm tra current ở LateUpdate ưu tiên escape.
- Ở lại: một death request, rung/chìm 1.1s, blackout, về tầng 0; FOV/input/blood overlay phục hồi.
- Reset/recycle rồi thử lại; reset reserve không được xóa breathing của anomaly current.
- Kiểm tra Console không có shader errors và Profiler không có GC/material instance tăng mỗi frame.

Các log chính có prefix `SEA_OF_BLOOD_`; death có `DEATH_SUFFOCATION_STARTED`.
Validator lifecycle không thay thế test collider, âm thanh, shader và chuỗi respawn trong Play Mode.
