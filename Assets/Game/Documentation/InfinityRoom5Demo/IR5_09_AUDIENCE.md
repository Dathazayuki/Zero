# IR5-09 — Khán giả đã vào chỗ

F3 lượt thứ tư trong demo chọn IR5-09. Chọn anomaly chỉ đưa nó vào trạng thái chờ.

1. Player bước vào `IR5_09_Audience/StageEntryZone` trên sân khấu.
2. Camera hướng vào trong sân khấu (+Z). Nếu đi lùi lên sân khấu, anomaly tiếp tục chờ.
3. Player quay đầu về khu ghế: camera phải nhìn thấy `AudienceLookTarget` và
   không bị vật thể che. Sáu mannequin ngồi mới hiện; mannequin trưng bày được ẩn.

Mannequin xuất hiện một lần, giữ nguyên cho đến F2/đổi anomaly. Không tấn công.
F2 khôi phục mannequin trưng bày, hướng ghế và ẩn toàn bộ khán giả. Disable cũng reset.

Các reference trên `IR5_Anomalies/IR5_09_Audience` được gán trực tiếp. Có thể chỉnh
vùng stage và điểm nhìn trong scene. Khi đã ghi nhận lên sân khấu nhìn vào trong,
cơ chế giữ trạng thái chờ quay đầu kể cả nếu Player đã bước ra khỏi vùng stage.

Sáu instance dùng chung `PF_IR5_SeatedMannequin.prefab`, từ mesh mannequin nền
`IR5V2_FullMannequin.obj` của dự án, author thành pose ngồi tĩnh. Ba vị trí trên ghế
cafe và ba vị trí trên booth; các ghế cafe xoay về sân khấu khi anomaly hiện.
Không thêm collider khán giả chặn đường di chuyển.

Logic chờ/nhận biết thứ tự nằm trong `CabaretAudienceRules`; adapter
`CabaretAudienceAnomaly` xử lý camera, vật cản và presentation. Data riêng:
`ScriptableObjects/Anomalies/InfinityRoom5/IR5_09.asset`.

Đã xem preview tư thế ngồi trong Edit Mode. Có test cho thứ tự trigger, chống
callback lặp và reset, nhưng chưa chạy Test Runner/Play Mode. Scene lưu thủ công.
