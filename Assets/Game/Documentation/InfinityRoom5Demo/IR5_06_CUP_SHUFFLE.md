# IR5-06 — Những chiếc ly tự tráo vị trí

Thay thế ý tưởng bộ trống không người đánh. Đây là anomaly quan sát, không thêm
luật chọn đáp án hoặc gây chết.

- Ba ly có sẵn: `IR5_Drink_01`, `IR5_BarDrink_2`, `IR5_BarDrink_3`, dưới
  `IR5V2_DetailPass/IR5_BarDrinkware`. Di chuyển cả root để nước và lót ly đi cùng.
- Khi Player tới trong 1.7 m theo mặt phẳng ngang, tính từ ly giữa, một đợt tráo bắt đầu.
- Hai ly đổi chỗ theo hai đường vòng ngược nhau, giữ nguyên độ cao mặt quầy.
  Các cặp vị trí 0–1 và 1–2 luân phiên, tổng bảy lần. Thời gian mỗi lần giảm
  từ 0.85 xuống 0.45 giây. Sau đó các ly đứng yên ở vị trí mới.
- Trạng thái normal không đổi bố cục. F2, đổi anomaly hoặc disable trả từng root
  về vị trí ban đầu; chọn lại IR5-06 sẽ chạy lại được.

Component `CabaretCupShuffleAnomaly` trên `IR5_Anomalies/IR5_06_CupShuffle`:
`Approach Distance`, `First Swap Seconds`, `Last Swap Seconds`, `Swap Count`,
`Lane Width` là các thông số chỉnh trong Inspector. Giữ Lane Width ít nhất 0.055 m
để hai lót ly không chồng nhau khi lướt qua; tăng quá cao có thể vượt mép quầy.
Ba reference phải cùng parent, đứng cùng cao độ và được xếp theo thứ tự dọc quầy.

Data: `ScriptableObjects/Anomalies/InfinityRoom5/IR5_06.asset`.
F3 theo thứ tự: IR5-01 → IR5-05 → IR5-06 → IR5-09 → IR5-10 → IR5-12. F2 normal.
Test kiểm tra khoảng cách lúc đổi chỗ, cao độ, điểm kết thúc và reset/disable;
chưa chạy Test Runner hay Play Mode. Scene lưu thủ công; tool setup tạm được xóa.
