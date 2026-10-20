# 20 anomaly cho Infinity Room 2 — Living Room

## 1. Hướng thiết kế

Chủ đề xuyên suốt: **“Căn phòng vẫn đang được một gia đình sử dụng, nhưng họ không muốn bạn nhìn thấy.”**

Giữ phong cách phòng khách cũ: ánh sáng vàng, sofa olive, thảm đỏ sẫm, TV CRT, đồ sứ và điện thoại bàn. Horror phát triển từ dấu vết sinh hoạt đến sự xuất hiện của những người trong gia đình.

Chốt **8 bất thường nhẹ, 8 horror không gây chết và 4 horror có thể gây chết**. Đây là pool 20 nội dung để hệ thống chọn; không xuất hiện đồng thời và không bắt Player gặp đủ 20 loại mới vượt phòng.

Luật quan sát hiện tại tiếp tục áp dụng: **có anomaly → ngược mũi tên; phòng bình thường → theo mũi tên**. Các bất thường nhẹ vẫn tính là anomaly.

## 2. Danh sách 20 anomaly

### Nhóm A — Có gì đó không đúng, chưa gây nguy hiểm

| ID | Anomaly | Diễn biến và cách nhận biết | Asset cần thêm |
|---|---|---|---|
| IR2-01 | **Đồng hồ chạy ngược** | Hai kim quay ngược từng nấc rõ ràng. Nhìn khoảng 2 giây là nhận biết được; không yêu cầu nhớ giờ của phòng trước. | Tách kim nếu mesh đang gộp; dùng đồng hồ hiện có. |
| IR2-02 | **Tách trà của người thứ tư** | Xuất hiện thêm một tách trà còn nóng ở vị trí vốn không có ghế. Vệt nước kéo từ tách xuống mép bàn. | Nhân prefab tách hiện có, steam VFX nhẹ và decal vệt trà. |
| IR2-03 | **Chiếc lót ly lệch chỗ** | Chỉ một chiếc lót ly bị trượt khỏi vòng hằn nước trên bàn khoảng 6–8 cm. Không nhấp nháy hay tự di chuyển; nhận ra bằng cách nhớ vị trí của bộ trà. | Dùng bộ trà và vết vòng nước có sẵn; thêm một pose dịch lót ly. |
| IR2-04 | **Đồ sứ úp mặt** | Những đồ sứ trong tủ đồng loạt xoay ngược hoặc bị đặt úp, tạo thay đổi nhận ra từ cửa phòng. | Dùng đồ sứ hiện có và bố trí variant. |
| IR2-05 | **Kệ sách bị quay lưng** | Toàn bộ gáy sách biến mất; các trang giấy đều quay ra ngoài. Một cuốn nằm mở trên kệ. | Variant bố trí sách và mesh cuốn sách mở đơn giản. |
| IR2-06 | **Chiếc bóng thừa** | Ghế sofa IR2_DetailArmchairA không có ai ngồi nhưng bóng đổ từ đèn cây lại hiện một người đang ngồi trên ghế. | Mesh Father ở pose Sit hiện tại trong FamilyAnimation, căn vào đệm sofa, dùng ShadowsOnly và bóng mềm từ đèn cây; giữ bóng ghế thật và lifecycle hiện có. |
| IR2-07 | **Tay cầm ấm trà đổi hướng** | Ấm trà xoay 180 độ tại chỗ khiến tay cầm đổi hướng; vòi đi cùng ấm, còn các tách và đĩa đứng yên. Dấu hiệu nhỏ, cần nhớ hướng đặt ấm thường ngày. | Dùng ấm hiện có, tách khỏi mesh bộ trà để xoay riêng. |
| IR2-08 | **Khung ảnh hơi nghiêng** | Một khung ảnh lệch khoảng 4–5 độ so với đường viền tường. Các khung khác vẫn thẳng. | Dùng khung ảnh hiện có; lưu pose chuẩn để so sánh và reset. |
| IR2-14 | **Chậu cây mini (mini PottedPlant)** | Chậu cây cảnh (`PF_IR2_Meshy_PottedPlant`) bị thu nhỏ bất thường xuống còn kích cỡ 0.6 (60% kích thước ban đầu). Nhận biết khi quan sát góc phòng hoặc vị trí đặt chậu cây. | Dùng prefab `PF_IR2_Meshy_PottedPlant` hiện có; chỉnh scale còn 0.6 khi anomaly kích hoạt. |

### Nhóm B — Horror, ám ảnh nhưng không giết Player

| ID | Anomaly | Diễn biến và cách nhận biết | Asset cần thêm |
|---|---|---|---|
| IR2-09 | **Người đang uống trà** | Tách nâng nhẹ khỏi đĩa, nghiêng như có người uống rồi đặt xuống. Không có bàn tay nào xuất hiện. | Animation transform tách, gợn trà và tiếng sứ chạm nhẹ. |
| IR2-10 | **Có người trên sofa** | Đệm sofa lún xuống thành dáng người; gối dịch sang bên và mặt đệm nhịp nhẹ như đang thở. | Variant đệm có blend shape hoặc mesh biến dạng; dùng sofa hiện có. |
| IR2-11 | **Con mắt chập chờn trên LCD** | Màn hình điện thoại bàn hiện image con mắt đã có sẵn. LCD chớp nhiễu ngắn theo nhịp không đều; mỗi lần hình ổn định lại, con ngươi đổi hướng nhìn về phía Player. Ống nghe vẫn nằm yên. | Tái sử dụng `Assets/Game/Art/Eye.png`; thêm LCD nhỏ nếu điện thoại chưa có màn hình, material phát sáng và hiệu ứng flicker nhẹ. Không tạo image mắt mới. |
| IR2-12 | **Chương trình gia đình** | TV chiếu một gia đình ngồi bất động nhìn thẳng ra ngoài. Sau vài giây, tất cả đồng loạt nghiêng đầu theo cùng một hướng. | Clip TV ngắn, material màn hình CRT; dùng các nhân vật chung của bộ asset gia đình. |
| IR2-13 | **Họ đang đến gần trong tranh** | Tranh phong cảnh có thêm ba người ở xa. Mỗi lần Player quay đi rồi nhìn lại, họ tiến gần thêm một bước; bước cuối chỉ còn những khuôn mặt sát khung. | Ba phiên bản texture cho cùng khung tranh. |
| IR2-15 | **Đôi chân dưới rèm** | Rèm kín nhưng có hai bàn chân nhỏ ở dưới. Khi đến gần, rèm phồng lên rồi xẹp xuống, đôi chân biến mất. | Bàn chân hoặc phần chân silhouette, animation rèm và bóng sau rèm. |
| IR2-16 | **Đứa trẻ dưới bàn** | Một khuôn mặt tối nhìn ra từ dưới bàn trà. Khi Player cúi nhìn hoặc đến gần, nó rút vào bóng tối; chỉ để lại tiếng ghế kéo. | Một model trẻ em dạng bóng tối với hai pose: nhìn ra và rút vào. |

### Nhóm C — Bốn anomaly có thể giết Player

Mỗi loại phải có **tín hiệu hình ảnh trước đòn đánh**. Chỉ nhận ra anomaly rồi rút lui là đủ; không yêu cầu Player tiến sát hoặc tương tác với vật nguy hiểm.

| ID | Anomaly | Cảnh báo, hành vi và điều kiện gây chết | Asset cần thêm |
|---|---|---|---|
| IR2-17 | **Người cha trên ghế** | Một người đàn ông ngồi quay lưng. Khi Player vào gần khoảng 1,8 m, tay ông bấu chặt ghế, đầu bắt đầu xoay và thân nghiêng dậy. Sau khoảng 2,5 giây, ông bật dậy lao ngắn; chỉ gây chết nếu đòn đánh chạm Player. Lùi ra trước đòn là thoát. | Nhân vật người cha; animation ngồi, quay đầu, đứng bật dậy và lao đánh. |
| IR2-18 | **Người bước ra từ TV** | Một người áp mặt vào kính từ phía trong, màn hình phồng và nứt. Sau khoảng 3 giây báo trước, cơ thể bò qua màn hình rồi truy đuổi trong phòng. Player có thể rút về lối vào; quái không đuổi xuyên sang segment khác. | Nhân vật dùng chung, animation bò ra/bò đuổi/tấn công; TV variant màn hình vỡ và VFX nhiễu. |
| IR2-19 | **Người mẹ sau rèm** | Rèm căng thành hình người, hai bàn tay in rõ lên vải. Tiến sát khoảng 2,2 m sẽ khiến tay trượt lên và rèm tách dần trong khoảng 2,5 giây; sau đó người mẹ chồm ra vồ. Rời vùng tầm với trước đòn thì sống. | Nhân vật người mẹ; tay sau vải, animation kéo rèm/chồm vồ; dùng rèm hiện có. |
| IR2-20 | **Chỗ ngồi của bạn** | Hai người lớn xuất hiện trên sofa, bóng đứa trẻ dưới bàn; ghế trống tự kéo về phía Player. Khi đứng trên thảm, hoa văn tối dần từ mép vào chân trong 3 giây. Nếu vẫn ở trên thảm khi tín hiệu hoàn tất, tay đứa trẻ trồi lên; Mother và Father teleport đến trước mặt Player, cả hai phát `Idle`. Camera xoay xuống tay rồi ngẩng lên hai người đang đứng nhìn trước khi blackout/hồi sinh. Bước khỏi thảm trước khi hết cảnh báo sẽ ngắt nguy hiểm. | Gia đình dùng chung, animation kéo ghế, material thảm chuyển trạng thái, mesh tay Children, `FamilyAnimation/Idle`, camera death hai nhịp. |

## 2.1. Ý tưởng nhỏ dự phòng — chưa thuộc pool 20

Các ý tưởng này ưu tiên so sánh với bố cục bình thường. Chỉ đưa vào roster sau khi chọn thay thế hoặc mở rộng pool; không cộng vào phân bổ 8/8/4 hiện tại.

| Ý tưởng | Dấu hiệu cần ghi nhớ | Cách giữ công bằng |
|---|---|---|
| **Một quyển sách thò ra** | Một gáy sách nhô khỏi hàng khoảng 3–4 cm, trong khi các quyển còn lại thẳng hàng. | Giữ cùng màu và độ sáng; đặt ở tầng kệ người chơi nhìn thấy từ cửa. |
| **Khe rèm không đều** | Hai mép rèm vốn khép gần nhau nay hở thêm một khoảng nhỏ; phía sau vẫn tối, không hiện vật thể mới. | Duy trì silhouette khe sáng vừa đủ để nhìn thấy ở cự ly quan sát bình thường. |

Các anomaly nhỏ nên tạo cảm giác “mình nhớ vật đó nằm khác đi” chứ không biến thành bài kiểm tra thị lực. Mỗi dấu hiệu phải phân biệt được từ góc quan sát thông thường và khi ánh sáng gameplay đang dùng.

Các mốc thời gian trên là cấu hình khởi điểm để playtest. Vùng nguy hiểm phải khớp animation và có đường rút thực sự, không chỉ dựa vào thời gian trên giấy.

## 3. Kế hoạch asset

### Tái sử dụng trước

Tận dụng các prefab hiện có của Room2: **đồng hồ, TV CRT, điện thoại, chậu cây, đồ sứ**, cùng sofa, ghế, bàn trà, sách, tranh, rèm và gương trong scene.

Các vật có chuyển động cần tách phần phù hợp: kim đồng hồ, ấm trà, tách trà, đệm sofa và hai nửa rèm. Với IR2-11, dùng `Assets/Game/Art/Eye.png`, thêm LCD nhỏ cho điện thoại bàn nếu model chưa có màn hình và điều khiển flicker bằng material/emission. Mỗi vật chỉ có một prefab gốc; các anomaly dùng variant hoặc reference đến phần con.

### Asset mới dùng chung

| Bộ asset | Sản phẩm cần tạo | Anomaly sử dụng |
|---|---|---|
| **Người cha** | Một nhân vật rigged, quần áo ở nhà cũ, khuôn mặt ít biểu cảm; khoảng 20–35k tam giác sau tối ưu. | 12, 13, 17, 18, 20 |
| **Người mẹ** | Một nhân vật rigged cùng phong cách, silhouette nhận ra được qua rèm và bóng tối. | 12, 13, 19, 20 |
| **Đứa trẻ dạng bóng** | Một model đơn giản, ưu tiên dáng người và pose; chưa cần bộ locomotion đầy đủ. | 12, 13, 15, 16, 20 |
| **Animation gia đình** | Ngồi, nghiêng đầu, quay đầu, đứng dậy, bò, chồm đánh và cảnh bắt. Kiểm tra clip sẵn có trước khi tạo mới. | 12, 15–20 |
| **Hình ảnh bị ám** | Ba trạng thái tranh, clip TV gia đình và `Assets/Game/Art/Eye.png` trên LCD điện thoại. | 11–13, 18 |
| **Đồ vật biến đổi** | Đệm sofa lún, sách mở, màn hình TV nứt; bàn tay dùng lại từ nhân vật nếu phù hợp. | 5, 10, 18, 20 |
| **VFX và material** | Hơi trà, vệt nước, bóng người, nhiễu CRT, rèm phồng và thảm chuyển trạng thái. | 2, 6, 9–10, 12–15, 18–20 |
| **Âm thanh** | Sứ chạm, nhiễu điện tử, ghế kéo, hơi thở, vải căng và tiếng báo trước đòn. Không dùng âm thanh làm dấu hiệu duy nhất. | Các anomaly động |

Giữ texture nhân vật chủ yếu ở 2K và chia sẻ material khi phù hợp. Chỉ cân nhắc 4K cho khuôn mặt xuất hiện sát camera sau khi kiểm tra thực tế. Đây là danh sách sản xuất; chưa đặt tác vụ tạo model hoặc cam kết chi phí Meshy.

## 4. Triển khai và nhịp chơi

- Gắn 20 ID ổn định vào roster Room2 và các slot tương ứng. Tám loại đầu dùng `UneasyNormal`, tám loại giữa dùng `MinorAnomaly`, bốn loại cuối dùng `MajorAnomaly` và bật quyền gây chết.
- Giữ cơ chế chọn nội dung và nhịp hồi phục hiện có. Không phát 20 anomaly theo thứ tự cố định; không ép quái xuất hiện liên tục để tăng khó.
- Chỉ khởi chạy diễn biến động khi Player thực sự vào phòng hiện tại. Vùng cửa vào và tuyến rút luôn thông; không kích hoạt đòn đánh ngay khi spawn.
- Bốn anomaly gây chết chỉ hoạt động trong phạm vi Room2. Khi Player rời phòng, chết hoặc segment được tái sử dụng, dừng truy đuổi, âm thanh, VFX và khôi phục toàn bộ baseline.
- Dùng `AnomalyBehaviour` và death API hiện có. Bổ sung binding context rõ ràng cho mechanic mới; không tìm Player ngầm hoặc thêm singleton.
- Làm theo ba đợt: **8 loại nhẹ → 8 loại horror → 4 loại gây chết**. Duyệt một nhân vật và một cảnh horror tiêu biểu trước khi sản xuất cả bộ.
- Room2 hiện chưa có roster/segment hoàn chỉnh. Chỉ bật `Content Ready` sau khi kiểm tra loop, reset và đường rút; kế hoạch này chưa tự đưa Room2 vào campaign.

## 5. Nghiệm thu

- Mỗi anomaly nhận ra được ở khoảng cách quan sát bình thường; anomaly tinh tế cần trí nhớ về vị trí/chiều của vật, không phụ thuộc chi tiết vài pixel hoặc bắt buộc nghe âm thanh.
- Phòng bình thường có baseline rõ ràng. Không để ánh sáng lỗi, mesh chồng hoặc vật thể thiếu bị hiểu nhầm thành anomaly.
- Từng anomaly bật/tắt và reset hai lần đều trở về đúng trạng thái; không còn actor, tiếng động hoặc material từ lượt trước.
- Cả bốn loại gây chết có thể thoát bằng đường đi thật sau khi thấy cảnh báo, ở tốc độ Player hiện có. Không có đòn xuyên tường, giết lúc fade hoặc giết sau khi đã rời phòng.
- Playtest ghi lại: người chơi nhận ra ở đâu, hiểu tín hiệu nguy hiểm không và có cảm thấy cái chết công bằng không. Dùng kết quả để chỉnh thời gian báo trước, tầm đánh và trọng số xuất hiện.
