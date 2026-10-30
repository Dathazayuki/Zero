# IR5-08 — Nhà pha chế tàng hình

F2 rồi F3 13 lần để chọn IR5-08. Đứng gần phía khách của quầy để bắt đầu.
Anomaly không gây chết và không yêu cầu nhặt hoặc uống rượu.

Chai trên kệ bay xuống trong 2,4 giây, đồng thời ly trống trượt trên quầy.
Nắp chai chỉ nhấc nhẹ để rời cổ chai, rồi bay theo vòng cung ngang sang bên
quầy như được bartender cầm bằng tay còn lại trong 0,9 giây, sau đó
giữ vị trí lơ lửng độc lập khi chai nghiêng. Chai nghiêng rót trong 3,2 giây;
dòng rượu và mức chất lỏng trong ly cập nhật cùng tiến trình. Ly được đẩy
thêm 23 cm về phía Player trong 1,1 giây; chai đứng lại trước khi nắp bay
theo vòng cung ngang trở về rồi hạ nhẹ xuống miệng chai.
Cuối cùng chai bay về kệ trong 2 giây. Ly đầy ở lại trên quầy.

Mỗi lần bật anomaly chỉ phục vụ một lần. Vị trí phục vụ được chốt khi Player
lại gần, không tiếp tục đuổi theo Player. Ba vị trí an toàn nằm giữa các đèn
quầy; hệ thống chọn vị trí gần Player nhất. Ly đi qua làn phía trước để tránh
các ly và đèn đã có, luôn giữ độ cao mặt quầy.

Component: `IR5_Anomalies/IR5_08_InvisibleBartender` → `CabaretBartenderAnomaly`.

| Inspector | Ý nghĩa |
|---|---|
| Observer | Transform Player đã dùng bởi các anomaly khác |
| Bottle / Cap | Chai và nắp riêng, chai xuất phát trên kệ |
| Glass / Liquid | Mesh Glass và Water của `IR5_BarDrink_6 (3)` |
| Stream | LineRenderer dòng rượu |
| Serving Surface | Mốc mặt quầy, forward hướng về phía khách |
| Serving Lanes | Ba khoảng trống phục vụ theo trục X local của mốc |
| Approach Distance | Khoảng tiếp cận để bắt đầu; mặc định 1,65 m |
| Fetch / Uncap / Pour / Serve / Return Seconds | Thời lượng từng giai đoạn |
| Bottle Mouth | Điểm miệng chai theo tọa độ local của chai |
| Liquid Bottom / Height, Rim Height | Kích thước mesh chất lỏng và ly |
| Cap Arc Width / Side Offset / Lift | Độ cong vòng cung ngang, khoảng giữ nắp bên cạnh và độ nhấc nhẹ khỏi miệng chai |

`PF_IR5_PouringBottle` dùng vật liệu cùng bộ kệ. Chai trên kệ cũ nằm trong mesh
gộp nên chai phục vụ được dựng riêng, có nắp và miệng chai mở. Không sửa mesh
kệ gốc. Ly sử dụng mesh và material kính hiện có; Water được chuyển vào Glass
và để ẩn ở normal để ly bắt đầu trống. Water phải là con của Glass; chuyển
động của ly tự đưa Water đi cùng, không có code bám vị trí riêng.
Nhóm dấu son IR5-01 đi theo Glass,
giữ nguyên reference của IR5-01.

F2, đổi anomaly hoặc disable component đều trả các transform về trạng thái
đã ghi nhận, ẩn rượu và tắt dòng rót. Không có coroutine, NPC ẩn hoặc package mới.
Scene được lưu thủ công trong Unity.
