# IR5-11 — Vị khách cuối quầy

F3 mục **8/8** trong `InfinityRoom5_CrimsonCabaret_Demo`; F2 khôi phục phòng bình thường.

Boss dùng `Sitting` trên ghế `IR5V2_Stool3` cuối quầy. Rig cổ hướng `StageLookTarget` trên sân khấu, áp dụng sau Animator trong LateUpdate để không bị animation ghi đè. Không quay toàn thân, không ép position mỗi frame; dùng root motion của Sitting.

Player phải nhìn thấy Boss trước. Nhìn đi liên tục ít nhất 0,3 giây rồi nhìn lại: Boss biến mất, `IR5_BarDrink_5` rung nhẹ trong 0,7 giây rồi trở về rotation gốc. Không gây chết. Đổi anomaly, F2 hoặc disable đều hủy rung, ẩn Boss, khôi phục transform và root motion ban đầu.

Hierarchy: `IR5_Anomalies/IR5_11_BarGuest`, gồm `SeatedBarHost` và `StageLookTarget`. Inspector `CabaretBarGuestAnomaly` có reference `Neck`, `Stage Target`, `Glass` để kéo thả/chỉnh điểm nhìn. Các clip gốc và Boss của IR5-12/16 không bị thay đổi.

Dùng ScriptableObject `ScriptableObjects/Anomalies/InfinityRoom5/IR5_11_BarGuest.asset`, cùng hệ thống preview F2/F3 hiện có. Không thêm package hoặc campaign readiness. Tool setup tạm đã được xóa; scene lưu thủ công.
