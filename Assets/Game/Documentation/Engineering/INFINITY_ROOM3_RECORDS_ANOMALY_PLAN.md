# Kế hoạch 18 anomaly — Infinity Room 3: The Records Room

## 1. Hướng thiết kế

Chủ đề xuyên suốt: **“Phòng hồ sơ đang lưu trữ những người từng bước vào đây.”**

Giữ cảm giác một văn phòng lưu trữ cũ: đèn huỳnh quang, kim loại xỉn màu, giấy ngả vàng và tủ hồ sơ dày đặc. Horror tăng dần từ sai lệch nhỏ trong đồ vật, đến dấu vết cơ thể, rồi sự hiện diện trực tiếp của những người bị lưu trữ. Ưu tiên hình thể, khuôn mặt và tư thế gây bất an; không dựa vào gore.

Pool gồm đúng 18 anomaly: **4 thay đổi nhỏ khó nhận ra, 5 bất thường rõ nhưng không gây chết, 6 horror không gây chết và 3 anomaly có thể giết Player**. Mỗi lượt chỉ có một anomaly hoạt động; Player không cần gặp đủ 18 anomaly mới hoàn thành phòng. Độ khó nhận biết và độ đáng sợ là hai trục riêng: anomaly nhỏ vẫn phải quan sát công bằng ở góc nhìn và ánh sáng chơi bình thường.

Giữ luật quan sát đang có: **có anomaly thì đi ngược mũi tên; phòng bình thường thì đi theo mũi tên**. Ba anomaly gây chết đều báo trước bằng hình ảnh. Cách sống sót tùy encounter: IR3-15 yêu cầu quay lưng ngay khi đầu bắt đầu mọc chân; IR3-17 cần né đường lao của tủ; IR3-18 buộc Player chạy tới lối lên tầng kế tiếp sau khi sinh vật hoàn tất hình thành.

## 2. Danh sách 18 anomaly

### Nhóm A — Sai lệch nhỏ, cần ghi nhớ kỹ

| ID | Anomaly | Diễn biến và cách nhận biết | Asset cần có |
|---|---|---|---|
| IR3-01 | **Nhãn hồ sơ sai thứ tự** | Hai nhãn ngăn kéo liền kề, chẳng hạn A-12 và A-13, đổi chỗ; các nhãn khác giữ nguyên. | Nhãn tách rời khỏi tủ; font và vật liệu nhãn dùng chung. |
| IR3-02 | **Chiếc bút quay đầu** | Một cây bút cạnh sổ trực bị xoay 180 độ nhưng vẫn ở đúng vị trí. | Dùng prefab bút hiện có; lưu pose chuẩn và pose xoay. |
| IR3-04 | **Ngăn kéo chưa khép kín** | Một ngăn kéo hé khoảng 2,5 cm, chỉ lộ khe tối, không có sinh vật. | Mặt ngăn kéo, tay nắm và lòng ngăn kéo tách được. |
| IR3-05 | **Góc giấy bị gấp sai** | Góc tờ thông báo vốn gấp bên phải nay gấp bên trái; nội dung, ghim và vị trí không đổi. | Hai biến thể mesh hoặc hình ảnh gấp giấy. |

> Các dấu hiệu nhỏ phải nhìn được ở khoảng cách quan sát thông thường (mục tiêu ban đầu 0,8–1,2 m), không biến thành bài kiểm tra thị lực. Không dùng nhấp nháy hay âm thanh để tiết lộ đáp án.

### Nhóm B — Bất thường rõ, chưa có nguy hiểm

| ID | Anomaly | Diễn biến và cách nhận biết | Asset cần có |
|---|---|---|---|
| IR3-03 | **Tài liệu trên kệ tự rơi** | Một cụm tài liệu ở kệ giữa phòng đổ khỏi mép rồi rơi xuống sàn dù không có ai ở gần. Mỗi tập dùng collider và Rigidbody: kích hoạt chỉ bật trọng lực, không thêm lực đẩy hay xoay; trọng lực/va chạm quyết định toàn bộ chuyển động và vị trí nằm lại. Bố trí chênh mép kệ để hồ sơ có thể tự mất cân bằng; vật nằm vững trên kệ vẫn được kệ đỡ. F2 trả chúng về kệ. | Dùng binder/tài liệu hiện có, BoxCollider và Rigidbody; không dùng quỹ đạo hay marker đáp cố định. |
| IR3-06 | **Ba tập giấy mang lời cảnh báo** | Ở cuối bàn có ba tập giấy xếp riêng. Trên tờ trên cùng lần lượt hiện chữ **R**, **U**, **N**, viết bằng những vệt máu khô, nét kéo nhòe và không đều. Giữ nguyên vị trí các tập giấy giữa trạng thái bình thường và bất thường; dấu hiệu phải đọc được từ góc quan sát thông thường. | Dùng ba chồng giấy hiện có; tạo ba texture/decal chữ R, U, N bằng vệt máu sẫm. Không cần model mới. |
| IR3-07 | **Dấu tay trên tủ hồ sơ** | Nhiều dấu bàn tay in rải rác trên các cánh tủ và mặt ngăn kéo, như có người đã cố bám hoặc đập tay từ bên trong. Dấu có kích thước và độ nhòe khác nhau, tập trung quanh tay nắm và mép ngăn; không có bàn tay hay sinh vật xuất hiện. | Decal/vật liệu dấu tay bám theo mặt tủ và ngăn kéo; các vị trí cần dễ thấy khi quan sát dãy tủ. |
| IR3-08 | **Máy in không ngừng nhả giấy** | Một dải giấy trắng liên tục trườn từ máy in xuống sàn; vệt giấy vẫn nằm đó sau khi chuyển động dừng. | Dải giấy có thể cuộn/animate và một cụm giấy đọng trên sàn. |
| IR3-14 | **Đèn bàn chập chờn** | Đèn bàn nhấp nháy liên tục với nhịp không đều trong suốt anomaly. Ánh sáng chỉ chập chờn tại chiếc đèn này, không làm nhấp nháy toàn cảnh hay che khuất lối đi; không có đòn tấn công. | Dùng đèn bàn hiện có; điều khiển độ sáng/nhấp nháy và khôi phục trạng thái sáng chuẩn khi reset. |

### Nhóm C — Horror, không giết Player

| ID | Anomaly | Diễn biến và cách nhận biết | Asset cần có |
|---|---|---|---|
| IR3-09 | **Mặt cười nguệch ngoạc bằng máu** | Trên các gáy hồ sơ xuất hiện những khuôn mặt cười méo mó được vẽ nguệch ngoạc bằng máu. Nét vẽ khác kích thước và độ lệch, trông như vẽ vội bằng tay; chúng chỉ là dấu vết trên giấy, không biến thành sinh vật hay tấn công. | Decal/texture nét máu và mặt cười vẽ tay, đặt trên các gáy dễ nhìn; F2 khôi phục gáy sạch. |
| IR3-10 | **Khuôn mặt giữa gáy hồ sơ** | Một khuôn mặt người bị ép giữa hai tập hồ sơ dõi theo Player. Khi Player tới gần, khuôn mặt rút vào khe; vệt trán hoặc ngón tay còn lộ lại. | Dùng chung đầu người bị lưu trữ, mắt theo dõi, khe tối và animation thụt vào. |
| IR3-11 | **Ảnh thẻ đang hình thành** | Ảnh nhân viên trên bảng hồ sơ ban đầu trống; khi Player quay đi rồi nhìn lại, khuôn mặt dần xuất hiện với mắt nhắm, sau đó mở mắt. Trạng thái cuối giữ nguyên. | Một tờ hồ sơ với 3 trạng thái ảnh: trống, mắt nhắm, mắt mở. |
| IR3-12 | **Hộp hồ sơ vùng vẫy** | Một hộp hồ sơ trên kệ rung giật từng đợt, như có người bị nhốt bên trong đang cố thoát ra. Nắp và thành hộp cong nhẹ theo lực đẩy; hộp không bật mở và không tấn công Player. | Hộp hồ sơ có nắp/thành tách rời hoặc mesh biến dạng nhẹ; chuyển động rung theo nhịp, không cần lộ sinh vật. |
| IR3-13 | **Các bộ phận trong ngăn kéo** | Khi Player tiến lại gần dãy ngăn kéo bên phải, chúng tự mở lần lượt từ gần đến xa, tạo nhịp khám phá có chủ đích. Mỗi ngăn chứa một bộ phận riêng của nhân viên lưu trữ được tách từ model trong scene; không ngăn nào chứa đầu. Các phần cơ thể trông như được xếp và lưu hồ sơ, không tấn công hay gây chết. | Dùng model nhân viên lưu trữ hiện có; tách và đặt riêng thân, tay/cẳng tay, chân/bàn chân (hoặc các phần phù hợp với rig/model). Cần lòng ngăn kéo đủ sâu, pose/bộ phận riêng cho mỗi khay và chuyển động mở tuần tự. Không tạo hoặc đặt đầu cho anomaly này. |
| IR3-16 | **Biểu tượng quỷ trên trần** | Trên trần xuất hiện một vòng tròn bao quanh ngôi sao năm cánh, vẽ bằng máu. Hình phải đọc rõ từ sàn và trông như dấu vẽ bằng tay; không có quái vật xuất hiện hay tấn công. | Decal/texture hoặc mesh mỏng đặt sát trần, gồm vòng tròn và pentagram bằng nét máu; có trạng thái sạch để reset. |

### Nhóm D — Horror có thể giết Player

Mỗi encounter có cảnh báo hình ảnh rõ ràng và đường rút lui công bằng. Thời gian và khoảng cách dưới đây là thông số khởi đầu để playtest, cần xác nhận với kích thước scene, animation và tốc độ Player.

| ID | Anomaly | Cảnh báo, tấn công và cách sống sót | Asset cần có |
|---|---|---|---|
| IR3-15 | **Cái đầu sáu chân trong hộp** | Một chiếc hộp trên tủ rung rồi rơi xuống, bật nắp và làm lăn ra một cái đầu. Khi sáu chân bắt đầu mọc bung ra, đây là tín hiệu buộc Player phải quay lưng và chạy khỏi khu vực. Nếu Player vẫn nhìn đầu cho đến khi chân mọc xong, sinh vật lập tức lao tới và giết Player; tiếp tục lùi hoặc chạy khi vẫn nhìn nó không giúp sống sót. | Hộp đặt trên tủ có nắp bật; model đầu quái có sáu chân gập/mở được; animation rơi, mọc chân, telegraph và cú lao; trigger xác định Player vẫn nhìn trong giai đoạn mọc chân để gây chết. |
| IR3-17 | **Tủ hồ sơ nghiền ép** | `IR3_Cabinet1 (8)` bắt đầu rung mạnh như bị kéo căng. Khi Player tiến gần theo phương ngang, `IR3_Cabinet1 (5)` bất ngờ lao ngang về phía tủ `(8)` để nghiền Player nằm trên đường lao. Ngay trước cú lao có cảnh báo hình ảnh ngắn: tủ `(8)` rung dữ hơn và tủ `(5)` giật khỏi vị trí. Camera của Player xoay mượt về phía tủ `(5)` trong lúc nó lao tới để người chơi thấy hướng nguy hiểm. Va chạm với tủ đang lao gây chết; Player sống nếu rời khỏi hành lang va chạm sau cảnh báo. | Hai tủ có chuyển động ngang và collider đồng bộ; tín hiệu rung/giật trước đòn; cú lao có giới hạn hành trình và cleanup; camera xoay mượt về nguồn tấn công, không cắt đột ngột. |
| IR3-18 | **Các bộ phận tự ghép hình người** | Các bản sao của những bộ phận đã thấy ở IR3-13 rơi vương vãi trên sàn, rồi dần dần lết về biểu tượng vòng tròn quỷ ở cuối phòng. Tại đó chúng ghép thành một hình người không đầu. Sau khi ghép xong, sinh vật truy đuổi để giết Player; chạy sang tầng tiếp theo là lối thoát duy nhất. Chỉ các bản sao được dùng cho cảnh này: những bộ phận gốc của IR3-13 vẫn nằm trong ngăn kéo và không bị tiêu thụ. | Tái sử dụng mesh/material các phần ở IR3-13 dưới dạng bản sao; chuyển động kéo lê trên sàn và ráp thành thân người không đầu tại dấu pentagram; rig/animation truy đuổi, hit/death và cửa/lối chuyển tầng. |

### 2.1. Mười phương án thay thế để cân nhắc

Các ý tưởng dưới đây chỉ là **ứng viên**, chưa thuộc pool 18 anomaly, chưa có ID và không thay thế mục nào khác. Chọn phương án phù hợp rồi mới đổi roster; danh sách 18 hiện tại vẫn được giữ để đối chiếu.

| # | Ý tưởng | Cách thể hiện trong Records Room | Mức độ gợi ý |
|---:|---|---|---|
| 1 | **Hồ sơ mới có dấu tay từ bên trong** | Một bìa hồ sơ sạch xuất hiện năm dấu ngón tay lồi nhẹ dưới lớp giấy, như có người ấn từ mặt trong. | Horror, không gây chết |
| 2 | **Máy dập ghim kẹp vào ngón tay** | Một bàn tay tái nhợt nằm dưới máy dập ghim; mỗi lần Player quay đi, máy hạ xuống thêm một chút nhưng không tấn công. | Horror, không gây chết |
| 3 | **Ngăn hồ sơ có tiếng gõ bằng hình ảnh** | Một ngăn tủ rung rất nhẹ theo nhịp ba lần; khe nhãn phồng ra như có thứ đang gõ từ bên trong. | Horror, không gây chết |
| 4 | **Bóng người bị kẹt dưới lớp giấy** | Chồng hồ sơ trên bàn phẳng, nhưng bóng bên dưới tạo thành khuôn mặt đang há miệng; góc nhìn thay đổi làm bóng vẫn bám theo. | Horror, không gây chết |
| 5 | **Thẻ nhân viên ghi ngày mất** | Một thẻ nhân viên trong khay lật mặt; ảnh vẫn là người bình thường nhưng dòng ngày nghỉ việc trùng với ngày hiện tại của game. | Bất thường/horror nhẹ |
| 6 | **Tủ hồ sơ tự khóa người chơi ở giữa lối** | Hai dãy tủ bắt đầu trượt chậm lại gần nhau sau cảnh báo bằng giấy rơi và ray rung; người chơi cần lùi ra khỏi đoạn hành lang trước khi lối bị ép kín. | Có thể gây chết nếu ở lại |
| 7 | **Bàn tay kéo rèm hồ sơ** | Một bàn tay thò khỏi khe giữa hai tủ, nắm lấy mép rèm/bao phủ hồ sơ rồi kéo mạnh; vùng nguy hiểm được báo trước bằng các bìa hồ sơ trượt ra. | Có thể gây chết nếu đứng gần |
| 8 | **Người ngồi sau quầy chỉ xuất hiện trong kính** | Quầy trống khi nhìn trực tiếp, nhưng kính phía sau phản chiếu một nhân viên ngồi quay lưng; phản chiếu từ từ quay đầu. | Horror, không gây chết |
| 9 | **Các gáy hồ sơ cùng nghiêng về phía Player** | Một dãy gáy hồ sơ nghiêng đồng loạt theo hướng Player dù các tập vẫn đứng yên; khi nhìn thẳng, một gáy ở giữa hé ra như một khe mắt. | Bất thường/horror |
| 10 | **Hồ sơ rơi tạo thành đường dẫn tới hốc tối** | Hồ sơ lần lượt rơi khỏi kệ, tạo một lối nhìn dẫn đến hốc lưu trữ; bóng người xuất hiện ở cuối hốc rồi lao ra sau cảnh báo rõ. | Có thể gây chết nếu không rút lui |

Các ứng viên cần được rà lại để tránh trùng cơ chế với anomaly đang giữ. Nếu chọn ý tưởng gây chết, thiết kế phải có dấu hiệu báo trước rõ, vùng nguy hiểm hữu hạn và đường rút lui công bằng.

## 3. Kế hoạch asset

### Tái sử dụng

Tận dụng các tủ, ngăn kéo, nhãn, ghế, bàn, kệ hồ sơ, binder, máy in, điện thoại, thông gió và hộp lưu trữ đã có trong `InfinityRoom3_RecordsRoom_Demo` cùng prefab hiện hữu. Mỗi loại vật thể lặp lại dùng một prefab gốc và đặt nhiều instance; anomaly chỉ tham chiếu hoặc chuyển sang pose/variant cần thiết.

Trước khi tạo mesh mới, kiểm tra các model trong project. Dùng binder và giấy hiện có cho IR3-03/09, đèn bàn có sẵn cho IR3-14, và mesh các bộ phận của IR3-13 làm bản sao cho IR3-18. Biểu tượng trần IR3-16 nên làm bằng decal/texture hoặc mesh mỏng; không cần model nhân vật mới.

### Bộ asset dùng chung

| Bộ asset | Sản phẩm cần tạo | Dùng cho |
|---|---|---|
| Người bị lưu trữ | Tận dụng model nhân viên lưu trữ hiện có. Tách các bộ phận phù hợp với mesh/rig để dùng riêng trong ngăn kéo; giữ đầu tách biệt và không dùng đầu ở IR3-13. Tạo bản sao các phần cho cảnh ráp hình người không đầu ở IR3-18. | IR3-10, IR3-13, IR3-18 |
| Pose/animation người | Thở nhẹ, mắt dõi theo, mặt rút vào khe, các bộ phận được đặt trong ngăn kéo, kéo lê trên sàn và ghép lại thành hình người không đầu, rồi truy đuổi. IR3-13 chỉ mở ngăn kéo theo thứ tự, không cần animation tấn công. | IR3-10, IR3-13, IR3-18 |
| Tủ, kệ và đèn | Ngăn kéo có mặt/tay nắm/nhãn/lòng tách rời; kệ dùng cho tài liệu rơi; hai tủ có thể rung và lao ngang với collider đồng bộ; đèn bàn có thể nhấp nháy; decal mặt cười trên hồ sơ. | IR3-03, IR3-04, IR3-07, IR3-09, IR3-14, IR3-17 |
| Giấy, nhãn và dấu máu | Atlas nhãn, nét vẽ mặt cười, biểu tượng pentagram trần, tờ giấy gấp hai phía, ba chồng giấy với chữ R/U/N bằng vệt máu, giấy máy in chuyển động và giấy đọng. | IR3-03, IR3-05, IR3-06, IR3-08, IR3-09, IR3-16 |
| Đèn bàn | Điều khiển nhấp nháy có giới hạn cho anomaly IR3-14 và trạng thái sáng bình thường để reset. | IR3-14 |
| Hộp hồ sơ | Hộp có nắp/thành đủ chắc để rung; một biến thể có nắp bật để thả đầu quái sáu chân. | IR3-12, IR3-15 |
| Đầu quái sáu chân | Một cái đầu có sáu chân gập sát khi nằm trong hộp, bung ra rồi lao ngắn; silhouette cần đọc rõ từ góc nhìn Player. | IR3-15 |
| Hiệu ứng và âm thanh | Bụi/giấy nhẹ, tiếng kim loại căng, máy in và hơi thở. Hình ảnh phải đủ để nhận ra cảnh báo kể cả khi tắt tiếng. | IR3-08, IR3-10 đến IR3-18 |

Không mặc định cần Meshy hay phát sinh credit. Chỉ cân nhắc tạo model ngoài nếu project không có asset dùng lại được và chất lượng hiện tại không đáp ứng silhouette/pose.

### IR3-08 — cấu hình demo

- Menu authoring: `Tools > Zero > Records Room > Add Printer Anomaly 08`, chỉ chạy chủ động và không tự lưu scene.
- `EndlessPrinterAnomaly` dùng `AnomalyData` IR3-08 và được thêm sau năm anomaly hiện có trong danh sách F3. F2 thu dọn giấy; kích hoạt lại bắt đầu nhả từ đầu.
- `Feed Speed` là tốc độ nhả (m/s), `Paper Width` là bề rộng; các marker `PaperRoute/Feed_00..` xác định miệng máy, đường qua mép bàn và các nếp giấy trên sàn. Điều chỉnh marker trong Edit Mode nếu di chuyển máy in/bàn.
- Giấy chạy hết tuyến thì dừng, vẫn giữ nguyên trên sàn đến khi reset. Mesh riêng được tạo một lần cho mỗi instance và được giải phóng khi instance bị hủy.
- Model `Mayin` hiện là máy đánh chữ: đầu tuyến giấy căn theo trục cuốn và tờ giấy dựng phía sau, không lấy mép bounding box. Bản `PrinterModel` nằm trong IR3-08, thay máy thường trong lúc anomaly hoạt động và ẩn khi reset; máy thường khôi phục trạng thái ban đầu.
- Lệnh căn lại riêng cho pose model hiện tại: `Tools > Zero > Records Room > Align Printer 08 To Actual Roller`. Mốc Feed_00 tại world `(-2.937, 1.219, -0.771)`, tuyến hướng 315°, bề rộng giấy 0.366 m; cần căn lại nếu đổi model hoặc di chuyển máy thường.

### IR3-06/07/12/15 — cấu hình demo

- Công cụ chủ động: `Tools > Zero > Records Room > Add Horror Anomalies 06 07 12 15`. Scene được lưu thủ công; chạy lại không tạo thêm bản trùng.
- Thứ tự F3: IR3-01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11 → 12 → 13 → 14 → 15 → 16 → 17 → 18. F2 trả phòng về bình thường.
- IR3-06 dùng đúng ba chồng `Tailieu (1..3)` trên bàn cuối phòng, chữ R/U/N bằng vệt máu chỉ xuất hiện khi anomaly bật. IR3-07 là dấu bàn tay **màu đen**, gắn trên mặt ngăn kéo để đi cùng ngăn kéo khi thay đổi pose.
- Prefab chung `Prefabs/InfinityRoom3Demo/Anomalies/PF_IR3_HollowArchiveBox.prefab` có đáy, bốn thành, khe tay thật và nắp riêng. Chọn `LidHinge`, chỉnh local X dương để mở nắp. IR3-12 và IR3-15 dùng hai instance của cùng prefab.
- IR3-12 thay hộp trang trí trên Cabinet3 bằng hộp rỗng, rung hộp và hé nắp nhẹ; không có đòn tấn công.
- IR3-15 thay hộp trên Cabinet4 để nắp không đè vào cây và đầu xuất hiện ở lối đi thoáng. Dùng bản bake riêng của `vintage_woman_3d_model/Head` cùng vật liệu gốc; sáu chân tự dựng và chuyển động bằng code. Model người gốc không bị thay đổi.
- Trên component `ArchiveHeadAnomaly`, `Trigger Distance` mặc định 4 m theo khoảng cách trên mặt sàn. Khi IR3-15 đang bật, Player bước vào khoảng cách này sẽ kích hoạt hộp dù không nhìn hộp; mặt tủ che đáy hộp không chặn kích hoạt. `Warning Seconds` 1,2 giây, `Fall Seconds` 0,7 giây, `Grow Seconds` 2,5 giây. Nhìn cái đầu đến hết lúc mọc chân sẽ khóa kết quả chết: sinh vật vồ tới trong 0,35 giây kể cả Player tiếp tục lùi. Quay đi trước khi mọc xong thì không kích hoạt cú vồ. Chỉ kiểm tra hướng nhìn và vật cản ở giai đoạn mọc chân.
- IR3-15 dùng `PlayerDeathController` và blackout của demo, hồi sinh ở điểm bắt đầu. F2/reset/disable trả hộp, nắp, đầu và chân về baseline; prefab hộp vẫn hiện như vật trang trí bình thường.

### IR3-09/10/11 — cấu hình demo

- Menu chủ động: `Tools > Zero > Records Room > Add Dossier Anomalies 09 10 11`; gán thêm ba `AnomalyData`, sắp thứ tự F3 theo ID và không tự lưu scene.
- IR3-09 đã thay dấu chữ bằng mặt cười từ `Art/InfinityRoom3/Smile.png`, dùng texture mask nét đỏ và material chung; nhóm `BloodSmiles` ẩn khi F2/reset. Giữ tên node `IR3_09_ClosedDossiers` để bảo toàn tham chiếu cũ.
- IR3-10: dùng mesh và vật liệu từ `vintage_woman_3d_model/Head`. Bốn bản sao dùng chung mesh hồ sơ mở một khe ở `IR3_File_A_L3_S2_B0..4`, tầng trên kệ A. Khi bật anomaly, năm hồ sơ thường được thay bằng cụm có khe; khi reset khôi phục đúng trạng thái ban đầu. Mặt quay theo Player trong phạm vi 25°, tới gần 1,35 m sẽ rút vào trong 0,8 giây; đầu ngón tay còn lại làm dấu hiệu.
- IR3-11: một hồ sơ nhân viên ở vị trí tờ thông báo phía trên bên phải bảng cuối phòng. Ảnh bình thường trống; khi bật anomaly, mỗi lần đã nhìn ảnh rồi quay đi ít nhất 0,4 giây và nhìn lại sẽ tăng một trạng thái: trống → mắt nhắm → mắt mở. Trạng thái mắt mở giữ nguyên; F2 khôi phục ảnh trống.
- Hai ảnh thẻ được tạo bằng imagegen từ concept nhân viên và lưu tại `Art/InfinityRoom3/IR3_11_Employee_01_EyesClosed.png`, `IR3_11_Employee_02_EyesOpen.png`; prompt và nguồn nằm trong `IR3_11_GENERATION.md` cùng thư mục.
- IR3-09/10/11 đều không gây chết. Kiểm tra nhìn ảnh cho phép collider của chính Player ở cuối tia nhìn, còn vật cản trong phòng vẫn che được ảnh.

### IR3-14/16/17/18 — thiết kế cập nhật

- IR3-03/09/14/16 được gán bằng menu chủ động `Tools > Zero > Records Room > Update Anomalies 03 09 14 16`. IR3-03 dùng `ArchiveFallingFilesAnomaly`: năm tài liệu `IR3_File_A_L3_S0_B0..4` có BoxCollider/Rigidbody, chỉ bật trọng lực và rơi bằng Unity Physics, không cộng lực đẩy hoặc xoay; reset khôi phục pose và trạng thái vật lý. Menu `Use Physics For Falling Files 03` di trú riêng IR3-03, bỏ marker Landing và cấu hình lực/quỹ đạo cũ khi lưu scene. IR3-14 dùng `DeskLampFlickerAnomaly`; IR3-09/16 dùng `LivingRoomArrangementAnomaly` để bật/tắt lớp dấu. Scene vẫn lưu thủ công.
- IR3-16 dùng `Art/InfinityRoom3/SatanCircle.png`, đặt sát trần cuối phòng. Hai ảnh nguồn được giữ nguyên; texture mask riêng loại nền đen/ô caro và chỉ giữ nét đỏ. IR3-18 dùng thêm một instance của material vòng tròn dưới sàn tại chỗ ghép cơ thể, độc lập với dấu trên trần của IR3-16.
- IR3-14 dùng đèn bàn sẵn có, chỉ nhấp nháy đèn đó theo nhịp không đều; khi reset trả đúng cường độ sáng ban đầu. Không có vùng sát thương.
- IR3-16 là hình vòng tròn và ngôi sao năm cánh bằng máu ở sát trần. Hình cần nhìn rõ từ mặt sàn; dùng decal/texture hoặc mesh mỏng và không kích hoạt đòn tấn công.
- IR3-17 gán vai trò riêng: Cabinet `(8)` là tủ cảnh báo rung; Cabinet `(5)` là tủ lao ngang về phía `(8)`. Cảnh báo phải cho Player thời gian nhận biết đường va chạm và né sang bên. Camera xoay mượt về `(5)` trong cú lao, không teleport góc nhìn. Va chạm với tủ gây chết; reset đưa hai tủ và camera về baseline.
- IR3-18 dùng bản sao mesh của các bộ phận IR3-13. Các phần nằm rải trên sàn, tự kéo tới pentagram cuối phòng, ghép thành thân người không đầu rồi mới truy đuổi. Lối lên tầng kế tiếp phải tiếp cận được trong lúc chạy; đó là đường thoát duy nhất. Không di chuyển hoặc tiêu thụ các bộ phận gốc đang dùng ở IR3-13.
- Menu authoring chủ động `Tools > Zero > Records Room > Add Encounters 17 18` gán hai anomaly vào F3 và bake NavMesh; không tự lưu scene. IR3-17 cảnh báo 0,9 giây và lao 0,55 giây, kiểm tra vùng quét để không bỏ lọt Player giữa hai bước vật lý. IR3-18 rơi 1,5 giây, lết về vòng tròn 9 giây, ghép 3 giây rồi dùng NavMeshAgent chase không timeout.
- Bộ phận IR3-18 mang nhãn giấy hồ sơ; chín mesh cũ được dùng chung, bổ sung phần hông tách từ cùng model để ghép kín thân. Bản sao rigged giấu mesh Head, giữ nhãn theo bone và dùng Animator `RecordRoomWoman`: `Running Crawl` khi đuổi, `Attack` khi bắt. Chỉ bắt khi gần và không bị đồ đạc chắn; camera xoay mượt về thân người trong cảnh death. Bản demo độc lập chưa có luồng lên tầng: F2 kiểm tra cleanup; khi tích hợp campaign, reset/deactivate room lúc chuyển tầng sẽ kết thúc truy đuổi.

### IR3-13 — cấu hình demo

- Menu chủ động: `Tools > Zero > Records Room > Add Body Drawers Anomaly 13`; chỉ gán thêm anomaly vào scene đang mở và không tự lưu scene.
- `IR3_13_BodyDrawers` dùng `ArchiveBodyDrawersAnomaly`, tham chiếu chín ngăn rải trên đủ tám prefab đang bật `IR3_Cabinet1 (8)`–`(15)` ở dãy bên phải. Thứ tự kích hoạt từ phía cửa vào là `(15)/Drawer1`, `(14)/Drawer2`, `(13)/Drawer0`, `(12)/Drawer2`, `(11)/Drawer0`, `(11)/Drawer2`, `(10)/Drawer1`, `(9)/Drawer3`, `(8)/Drawer1`. Chỉ số Drawer khớp tên object trong Unity, từ 0 đến 3. Mỗi khay chứa một nhóm `IR3_13_Contents_*`, đi cùng khay khi mở; các tủ blockout cũ đang tắt không được dùng làm target.
- Chín bộ phận lấy từ bản bake của `vintage_woman_3d_model`: hai bàn tay, hai cánh tay, hai giày, thân trên và hai chân tách từ mesh thân dưới. Giữ vật liệu/UV nguồn; không dùng đầu và không thay đổi model gốc.
- Mặc định `Trigger Distance` 2,4 m trên mặt sàn, `Open Seconds` 0,8 giây, `Interval Seconds` 1 giây, `World Open Offset` (-0,53; 0; 0) m. Ngăn tiếp theo chỉ bắt đầu khi đã đủ nhịp và Player ở gần ngăn đó; ngăn đã mở tiếp tục mở hết và giữ nguyên khi Player lùi đi.
- Bố cục trông ngẫu nhiên nhưng được đặt cố định để kiểm tra lặp lại: các ngăn khác hàng, `Extension Multipliers` lần lượt 1,18 / 0,94 / 1,31 / 1,08 / 1,24 / 0,97 / 1,16 / 1,12 / 1,27 (độ rút khoảng 0,50–0,69 m). Tủ `(11)` có hai khay rút lệch nhau; phần chân trong ngăn trên cùng của tủ `(9)` được đặt nghiêng để dễ thấy hơn từ bên ngoài.
- F2/reset/disable ẩn các bộ phận, đóng mọi khay về đúng vị trí ban đầu và xóa tiến độ chuỗi; kích hoạt lại bắt đầu từ ngăn đầu. IR3-13 không gây chết.

### Tủ hồ sơ có lòng thật

- Prefab chung `Prefabs/MINIGAME/Deco/IR3_Cabinet1.prefab`: thân đặc cũ tắt renderer/collider; `Body/HollowHousing` có lưng, nóc, đáy, hai vách và ray cố định, để hở phía trước.
- Mỗi `IR3_Cabinet*_Drawer0..3` chứa `DrawerTray` (đáy, lưng, hai thành), tay nắm và nhãn. Dịch chuyển local X của Drawer để rút cả khay; ray và thân tủ giữ nguyên.
- Migration IR3-04 chỉ di chuyển Drawer3, tránh cộng hai lần độ dịch chuyển lên tay nắm/nhãn đã làm con của Drawer3. Reference IR3-01 giữ nguyên trên hai nhãn.
- Công cụ chủ động: `Tools > Zero > Records Room > Upgrade Hollow Cabinet Prefab And Scene`. Chỉ sửa prefab được chỉ định và các tủ trong scene Records Room đang mở; scene vẫn cần người dùng lưu thủ công.

## 4. Kế hoạch triển khai

1. Chụp trạng thái chuẩn của bốn khu vực quan sát trong phòng để so sánh anomaly với bố cục bình thường.
2. Triển khai IR3-01 đến IR3-09 bằng prefab/pose/texture variant; IR3-03 dùng chuyển động tài liệu rơi, IR3-09 dùng mặt cười máu; kiểm tra reset trả mọi vật thể về đúng trạng thái ban đầu.
3. Làm một người bị lưu trữ và thử IR3-10 trước; duyệt silhouette, rig và khe giấu trước khi làm đủ animation.
4. Triển khai IR3-10 đến IR3-13; giữ các chuyển động nhỏ, không có hitbox gây chết. Với IR3-13, mở các ngăn kéo bên phải theo thứ tự khi Player tới gần; không mở đồng loạt và không đặt đầu trong các ngăn.
5. Triển khai IR3-14 đến IR3-18 theo nhóm mới: IR3-14 là đèn bàn chập chờn, IR3-16 là dấu pentagram không gây chết; chỉ IR3-15/17/18 gây chết. Với IR3-17 kiểm tra đường lao của tủ và góc nhìn camera; IR3-18 dùng bản sao các phần của IR3-13 và chỉ thoát bằng lối lên tầng tiếp theo.
6. Gắn 18 cấu hình vào hệ anomaly hiện có. Dữ liệu cấu hình giữ ID, mô tả, độ khó, pacing và `CanKill`; hành vi Unity tập trung vào trình bày/trigger, luật có thể kiểm tra riêng.
7. Giữ `InfinityRoom3.asset` chưa sẵn sàng cho campaign cho đến khi nội dung và scene được duyệt riêng.

## 5. Kiểm thử và tiêu chí nghiệm thu

- Mỗi anomaly có thể bật rồi reset về trạng thái chuẩn nhiều lần mà không để lại transform, vật liệu, animation, collider, âm thanh hoặc coroutine sót lại.
- IR3-01, IR3-02, IR3-04 và IR3-05 nhận ra được từ góc nhìn gameplay bình thường và chỉ thay đổi đúng chi tiết chủ đích. IR3-03 rơi rõ ràng nhưng không gây sát thương.
- IR3-09 hiển thị các mặt cười vẽ tay bằng máu trên gáy hồ sơ; IR3-14 làm nhấp nháy riêng đèn bàn và không gây chết; IR3-16 hiển thị vòng tròn pentagram bằng máu trên trần và không gây chết.
- IR3-10 đến IR3-13 tạo cảm giác horror nhưng không gọi luồng death.
- IR3-13 chỉ kích hoạt dãy ngăn kéo bên phải; từng ngăn mở tuần tự khi Player đến gần, mỗi ngăn để lộ một bộ phận khác nhau của nhân viên lưu trữ và không có đầu. Các ngăn không chồng lấn/chặn lối đi; reset đóng và khôi phục tất cả ngăn về pose ban đầu.
- IR3-17 chỉ gây chết khi Player còn nằm trên đường lao ngang sau cảnh báo; camera xoay mượt theo tủ `(5)` lao về `(8)`.
- IR3-18 chỉ bắt đầu truy đuổi sau khi các bộ phận bản sao ghép xong; Player chỉ thoát khi chạy sang tầng tiếp theo.
- IR3-15: nếu Player tiếp tục nhìn cái đầu trong suốt giai đoạn mọc sáu chân thì phải chết; quay lưng ngay khi chân bắt đầu mọc mới cho phép thoát.
- IR3-17 kiểm tra va chạm thật, tín hiệu báo trước, hướng camera và đường né khỏi cú lao ngang; không để tủ chặn đường trước cảnh báo.
- Không có đòn đánh xuyên tường, kéo Player về phía quái, hay truy đuổi ngoài phạm vi đã cảnh báo.
- Playtest kiểm tra thời gian phát hiện, thời gian cảnh báo và độ rõ của dấu hiệu ở ánh sáng scene thực tế.
- Chạy compile và Code Policy cho mọi thay đổi code; không tăng baseline vi phạm.

## 6. Giả định và giới hạn

- Đây là pool anomaly cho demo đơn người chơi; chưa thiết kế đồng bộ multiplayer.
- Một anomaly xuất hiện mỗi encounter; các ID không nhất thiết phải chạy theo thứ tự.
- Chỉ IR3-15, IR3-17 và IR3-18 được phép gây chết; IR3-14/16 đã chuyển thành anomaly không gây chết.
- Khoảng cách và thời gian cảnh báo là giá trị khởi đầu để thử trong scene, không phải bảo đảm cuối cùng trước khi kiểm tra với collider và animation.
- `InfinityRoom3.asset` hiện chưa được đánh dấu Content Ready; kế hoạch này không tự bật campaign readiness.
