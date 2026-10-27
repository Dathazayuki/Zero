# Kế hoạch 18 anomaly — Infinity Room 5: The Crimson Cabaret

## Định hướng

**Chủ đề: “Buổi diễn chưa từng kết thúc.”** Căn cabaret ban đầu có vẻ chỉ là một quán bar cũ với sân khấu nhỏ, rèm nhung đỏ và các mannequin trưng bày. Khi anomaly xuất hiện, căn phòng dần biến thành một buổi diễn kỳ quái: nhạc cụ tự chơi, đồ vật phản ứng với người chơi, mannequin thành khán giả, rồi một vị chủ trì bí ẩn chọn người chơi làm tiết mục tiếp theo.

Giữ đúng ngôn ngữ hình ảnh của demo: gỗ tối, đồng xỉn, ánh đèn hổ phách, nhung đỏ cũ và sân khấu đỏ sẫm. Dùng ánh sáng, âm thanh và chuyển động có chủ đích; trạng thái nguy hiểm phải đọc được cả bằng hình ảnh. Bốn anomaly nhỏ cần tinh tế nhưng vẫn nhận ra được khi người chơi quan sát kỹ. Năm anomaly gây chết người đều có tín hiệu báo trước và đường né rõ ràng; cảnh chết chỉ xảy ra sau khi người chơi thực sự bị bắt hoặc trúng hazard.

## Boss: The Smiling Host — Người Dẫn Chương Trình

Một người đàn ông cao, gầy, mặc vest đen vừa vặn, sơ mi trắng, găng tay và giày da. Khuôn mặt bị che bởi nhiễu đen chuyển động như màn hình mất tín hiệu; chỉ để lộ chiếc miệng đang mỉm cười. Không biến đầu thành TV. Cử chỉ lịch thiệp và chậm rãi: nghiêng đầu, chỉnh cổ tay áo, mời lên sân khấu, vỗ tay.

Boss xuất hiện trong sáu anomaly: IR5-11, IR5-12, IR5-14, IR5-16, IR5-17 và IR5-18. Ba anomaly đầu là chạm trán an toàn để người chơi nhận diện hình dáng và hành vi; ba anomaly cuối là encounter có thể gây chết. Không cần thanh máu hay hệ thống chiến đấu.

## Danh sách anomaly

Phân nhóm: **A — 4 anomaly nhỏ**, **B — 5 anomaly rõ ràng**, **C — 4 anomaly kinh dị**, **D — 5 anomaly nguy hiểm**. Tổng cộng 18; trong đó có 6 anomaly liên quan đến Boss và 5 anomaly có thể giết người chơi. IR5-13 được chuyển từ C sang D.

### A — Bất thường nhỏ, cần quan sát kỹ

| ID | Tên | Biểu hiện | Asset cần có |
|---|---|---|---|
| IR5-01 | Dấu son trên các ly | Nhiều ly ở quầy bar và bàn có 2–3 dấu son đỏ sẫm trên mỗi ly, gần miệng ly. | `Assets/Game/Art/Kiss.png`, mesh dấu son cong và material dùng chung. Demo hiện gán 12 ly, tổng 30 dấu; F3 bật, F2 khôi phục ly sạch. |
| IR5-02 | Phong bì thư kỳ lạ | Một phong bì giấy ngà cũ, nếp gấp rõ và dấu sáp đỏ mang nụ cười đen xuất hiện trên bàn café giữa `IR5V2_Table1`. Normal không có thư; anomaly hiện thư, không gây chết và không yêu cầu nhặt/mở. | Model `IR5V2_StrangeEnvelope.obj` kích thước 24 × 15 cm, material giấy/nếp gấp/sáp/mực; `IR5_02.asset`, dùng lại `LivingRoomArrangementAnomaly`. F3 mục 10 bật, F2 ẩn thư. |
| IR5-03 | Đồng hồ sai một số | Mặt đồng hồ có một số La Mã bị đổi từ VI thành IX, tạo hai số IX; kim và thời gian không đổi. | Hai biến thể mặt đồng hồ. |
| IR5-04 | Đàn thỏ dưới quầy bar | Tám con thỏ của Minigame 3 — Catch the Rabbit đứng tụ trong hốc dưới quầy bar do người dùng dựng, quay ra lối đi phía sau quầy. Giữ pose chờ, không bỏ chạy hoặc tấn công. Normal không có thỏ; F2 ẩn cả đàn. Người chơi cần quan sát xuống hốc để phát hiện. | Tám instance dùng chung `PF_IR5_BarRabbit`, lồng prefab `CallingCorridorDemo/Character/Rabbit` và giữ material gốc; Animator tắt để giữ pose chờ. `IR5_04.asset`, dùng lại `LivingRoomArrangementAnomaly`, không thêm script runtime. F3 mục 12. |

### B — Anomaly rõ ràng, không gây chết

| ID | Tên | Biểu hiện | Asset cần có |
|---|---|---|---|
| IR5-05 | Piano tự biểu diễn | Khi người chơi đến gần đàn, các phím tự nhấn theo một câu nhạc ngắn; ghế đàn trống. Giữ nắp đàn ở vị trí hiện tại, không nâng/mở nắp. | 36 phím trắng và 26 phím đen riêng biệt; audio câu nhạc tổng hợp trong demo, có thể thay bằng clip piano. |
| IR5-06 | Những chiếc ly tự tráo vị trí | Khi Player lại gần ba ly trên quầy, chúng tự trượt hoán đổi vị trí từng cặp như trò tráo ly. Một đợt bảy lần đổi, nhanh dần rồi dừng; không yêu cầu chọn ly và không gây chết. F2 trả tất cả về vị trí cũ. | Dùng ba root ly hiện có, đường trượt vòng tránh nhau trên mặt quầy; không dùng bộ trống. |
| IR5-07 | Tất cả mannequin nhìn theo | Khi Player ở trong phòng, mannequin trang trí đứng yên và xoay cổ chậm rãi hướng mặt theo camera Player. Không di chuyển, truy đuổi hoặc gây chết. F2 trả cổ từng mannequin về pose normal. | Bảy instance dùng chung prefab mannequin có rig `PF_IR5_DecorMannequin`; mảng bone `mixamorig:Neck`, trục mặt và vùng phòng. Không bật các mẫu cũ đang ẩn, không điều khiển Boss hoặc khán giả IR5-09. |
| IR5-08 | Nhà pha chế tàng hình | Khi Player lại gần phía khách của quầy, chai rượu trên kệ bay xuống; một ly trống trượt tới khoảng trống trên quầy gần Player. Nắp chai tự bật lên, chai nghiêng rót rượu và mức rượu trong ly tăng dần. Sau đó ly được đẩy thêm 23 cm về phía khách, chai đóng nắp và bay về kệ. Chạy một lần mỗi lượt chọn anomaly; không gây chết. F2 trả chai, nắp, ly và chất lỏng về trạng thái normal. | Chai riêng `PF_IR5_PouringBottle` dùng vật liệu cùng bộ kệ, nắp tách rời; tái sử dụng ly `IR5_BarDrink_6 (3)` và mesh Water; LineRenderer dòng rượu, material rượu hổ phách. `IR5_08.asset`, `CabaretBartenderAnomaly` và luật thời gian C# thuần. F3 mục 13. |
| IR5-09 | Khán giả đã vào chỗ | Player phải bước lên sân khấu, nhìn vào trong sân khấu, rồi quay đầu nhìn lại khu ghế khán giả thì sáu mannequin mới xuất hiện ngồi hướng về sân khấu. Trước thời điểm đó mannequin trưng bày vẫn ở vị trí bình thường. Không tấn công; lối giữa vẫn thông thoáng. | Một prefab mannequin ngồi dùng chung tại sáu vị trí; vùng sân khấu và điểm quan sát khán giả. |

### C — Kinh dị, không gây chết

| ID | Tên | Biểu hiện | Asset cần có |
|---|---|---|---|
| IR5-10 | Những bàn tay sau rèm | 12 bàn tay bám mép và gấu hai bên rèm. Mỗi cụm rụt ra sau rèm độc lập khi Player tới trong 1.8 m, mất 0.65 giây rồi ẩn. F2 khôi phục; không gây chết. | Model `Hand` trong scene được bake pose thành một prefab dùng chung; hai cụm và đường rút riêng. |
| IR5-11 | Boss — Vị khách cuối quầy | Boss ngồi trên ghế cuối quầy, phát `Sitting`; rig `mixamorig:Neck` hướng về sân khấu, không quay toàn thân. Sau khi Player đã thấy Boss, nhìn đi liên tục ít nhất 0,3 giây rồi nhìn lại thì hắn biến mất; chiếc ly gần đó rung nhẹ rồi dừng. Không gây chết. | Tái sử dụng TheSmilingHost, `Host.controller`/`Sitting`, ghế cuối quầy và ly hiện có; target sân khấu riêng. Đã gán mục 8 của F3; F2 khôi phục. |
| IR5-12 | Boss — Người đứng sau quầy | Boss đứng idle sau quầy bar. Nhìn trực tiếp không thấy hắn, nhưng nhìn qua ba gương phản chiếu thì thấy hắn đứng tại đó. Chỉ xuất hiện khi anomaly được chọn, không tấn công; F2/đổi anomaly ẩn Boss. | TheSmilingHost, animation idle, ba gương planar và layer MirrorReflectionOnly; không tạo bóng đổ hay collider lộ vị trí trong thế giới thường. |
| IR5-14 | Boss — Cái bóng đang vỗ tay | Boss đứng trên sân khấu, hai tay buông xuôi nhưng bóng của hắn vỗ tay độc lập. Khi người chơi đến gần mép sân khấu, tiếng vỗ dừng, Boss nghiêng đầu; đèn sân khấu tắt thoáng chốc và hắn biến mất. | Boss dùng chung; bóng được author sẵn; nhịp đèn và âm thanh. |

### D — Nguy hiểm, có thể giết người chơi

| ID | Tên | Tín hiệu và cách thoát | Asset cần có |
|---|---|---|---|
| IR5-13 | Bàn tay thả đèn chùm | Bật anomaly: `Hand` xuất hiện trên `IR5V2_Chandelier`, đèn rung nhẹ. Player vào vùng dưới đèn: rung mạnh trong 1,1 giây rồi rơi tăng tốc trong 1,05 giây; vị trí rơi cố định. Rung/rơi chưa trúng không điều khiển camera hoặc khóa Player; né được không có cutscene. Chỉ khi va trúng mới khóa Player, camera ngước lên trong 0,35 giây rồi cú dập cuối cắt đen bằng kiểu chết dùng chung của IR3-17 (`Freeze Duration = 0`). Bàn tay ẩn khi bắt đầu rơi. | Dùng đèn và bàn tay đã bố trí trong scene, volume tiếp cận và quét va chạm chống xuyên giữa hai bước. Không sửa rig/clip Hand. F2 trả đèn và ánh sáng về vị trí gốc, ẩn bàn tay. |
| IR5-15 | Vũ công gãy khớp | Mannequin gần sân khấu giật cục và kéo lê chân trong **1,5 giây**. Sau đó nó lao thẳng theo một hướng cố định dọc lối giữa. Người chơi né sang khu vực bar hoặc bàn; nếu bị tóm mới phát cảnh bị ghì xuống. | Rig mannequin, animation giật/lung/ghì, collider bắt. |
| IR5-16 | Boss — Cú búng tay | Boss ban đầu ngồi trên ghế. Khi Player lại gần trong 3 m và không bị vật cản che, hắn đứng dậy rồi nâng tay phải cạnh đầu búng một lần; tay còn lại sau lưng. Ngay cú búng, hố đen xuất hiện tại vị trí chân Player và báo trước 1,2 giây. Vị trí hố được chốt, không tiếp tục bám theo Player. Hố bán kính 0,95 m, mở trong 2 giây; rời vùng trước khi mở là sống, còn đứng trong vùng hoặc bước vào hố đang mở thì rơi tự do rồi chết. Không khóa điều khiển trước khi thực sự rơi. | TheSmilingHost và ghế dùng lại; controller riêng tái sử dụng các clip đã đặt sẵn. Shader và ống sâu hố đen tạo trong Unity; dùng hệ thống chết/hồi sinh hiện có. Đã gán vào demo, mục thứ 7 của F3; F2 khôi phục bình thường. |
| IR5-17 | Boss — Ánh đèn chọn diễn viên | Boss mời người chơi lên sân khấu. Spotlight khóa theo người chơi trong **2,5 giây cảnh báo**; Boss vỗ tay và đèn chuyển đỏ báo nguy hiểm. Nấp sau bar/booth để che tia sáng hoặc rời vùng. Chỉ khi tiếp xúc liên tục với ánh đỏ **1 giây** mới kích hoạt cảnh bị nhấc lên như con rối. Nấp khuất sẽ xóa tiến trình phơi sáng. | Boss mời/vỗ tay, spotlight kiểm tra che khuất, dây rối và cảnh treo người chơi. |
| IR5-18 | Boss — Tiết mục hạ màn | Boss cúi chào trong **2 giây báo trước**, rồi đi nhanh đuổi người chơi qua khu vực sân khấu. Nhiễu lan từ mặt xuống cổ, nụ cười rộng dần. Người chơi chạy vòng qua nội thất đến cửa tầng tiếp theo; không dịch chuyển xuyên tường. Khi bị bắt, camera hướng vào nụ cười, nhiễu tràn màn hình rồi cắt đen ngay. | Animation cúi chào/đi nhanh/bắt, vùng đường đi hợp lệ, hiệu ứng nhiễu mặt và màn hình. |

## Nhịp độ và khả năng đọc tình huống

- Dùng đường đi trung tâm làm tuyến chính; bar, booth và bàn tạo các chỗ né/che có thể nhìn thấy trước. Không để đạo cụ hoặc mannequin chặn lối thoát.
- Tăng căng thẳng theo nhịp có khoảng nghỉ: giới thiệu dấu hiệu nhỏ, cho nhạc cụ tự diễn, chuyển sang hiện tượng kinh dị, rồi mới đặt người chơi trước hazard hoặc Boss. Tránh để nhiều encounter chết người nối tiếp không có nhịp thở.
- Mỗi hazard có dấu hiệu hình ảnh nhất quán: ánh đỏ cho nguy cơ, bóng/đường vùng rơi cho phạm vi, cử động rõ và khoảng báo trước cho truy đuổi. Âm thanh bổ trợ chứ không phải tín hiệu duy nhất.
- Boss không cần xuất hiện theo thứ tự bắt buộc. Ba lần chạm trán an toàn giới thiệu hình ảnh và cử chỉ; ba encounter nguy hiểm vẫn phải tự giải thích được dấu hiệu và đường thoát, không đòi hỏi người chơi đã gặp Boss trước đó.
- Cửa thoát không bị khóa bởi encounter. Không tạo trạng thái bị kẹt vĩnh viễn nếu người chơi né hoặc mất dấu Boss.

## Hướng asset và tái sử dụng

- Duyệt một mẫu hình ảnh của Boss trong ánh sáng cabaret trước khi làm toàn bộ biến thể. Tách miệng cười khỏi lớp nhiễu đen để vẫn đọc rõ nét miệng ở khoảng cách chơi.
- Dùng một rig Boss cho pose ngồi, đứng, búng tay, nghiêng đầu, mời, vỗ tay, cúi chào, đi nhanh và bắt. Dùng lại mannequin, tay, ly, nhạc cụ và các đạo cụ có sẵn khi phù hợp.
- Giữ thay đổi trạng thái rõ ở kích thước nhìn trong game; không dựa vào chi tiết texture chỉ thấy khi phóng to.
- Ưu tiên asset hiện có trước khi đặt yêu cầu sản xuất; các phần đã triển khai được ghi trong tài liệu riêng. IR5-16 đã có clip búng tay được author trực tiếp trong Unity, không phát sinh chi phí dịch vụ.

## Chi tiết IR5-16 — phạm vi hiện tại

- Chuỗi dự kiến: **ngồi → Player tới gần → đứng dậy → búng tay → hố mở tại điểm đã chốt → né được hoặc rơi chết**. Không còn dùng đèn chùm rơi.
- Tái sử dụng `Assets/Game/MixamoAnimation/Host/Sitting.anim` và `Assets/Game/MixamoAnimation/SitToStand.anim` từ Animator hiện có. Controller riêng `IR5_16_HostSnap.controller` giữ nguyên controller/clip của IR5-12.
- Clip: `Assets/Game/MixamoAnimation/Host/StandingFingerSnap.anim`, Humanoid, dài **2,2 giây**, không loop. Bắt đầu/kết thúc theo dáng formal idle hiện tại, tay phải nâng cạnh đầu búng một lần, tay trái sau lưng. Điểm búng: **1,10 giây**, được đọc từ thời gian state Animator; không cần Animation Event.
- Thông số demo: khoảng tiếp cận **3 m**, báo trước **1,2 giây**, bán kính **0,95 m**, mở **2 giây**, cảnh rơi **1,15 giây** rồi cắt đen. Điểm mở lấy từ raycast dưới chân và chỉ nhận collider sàn được gán; nếu không đủ mặt sàn để chứa toàn bộ hố thì hủy lượt tấn công an toàn.
- Né thành công thì hố đóng lại sau một khoảng ngắn, không truy đuổi và không tái khóa theo Player. Khi bị rơi, chuyển sang cảnh rơi tự do rồi chết; F2/đổi anomaly phải khôi phục sàn, actor, camera và input.
- Encounter đã được gán trong `IR5_Anomalies/IR5_16_HostSnap`. F3 chọn mục thứ 7, F2 reset; runtime rules là C# thuần, adapter điều khiển Animator và camera. Hố cắt phần hiển thị sàn bằng material tạm thời và có ống sâu bên dưới; cảnh rơi di chuyển camera khi đã bị bắt, không phá collider sàn chung. Reset khôi phục material gốc, camera và input.

## Kế hoạch triển khai sau khi duyệt

1. Ghi nhận snapshot trạng thái bình thường của các đạo cụ liên quan; dựng blockout tín hiệu, vùng nguy hiểm và lối né trước khi hoàn thiện hiệu ứng.
2. Triển khai bốn anomaly nhỏ và kiểm tra độ dễ nhận ra trong ánh sáng hiện tại.
3. Làm một mẫu Boss trong ngữ cảnh cabaret, rồi tái sử dụng rig/asset cho IR5-11, IR5-12, IR5-14, IR5-16, IR5-17 và IR5-18.
4. Làm nhóm B và C, sau đó nhóm nguy hiểm D; xác nhận từng cảnh báo và đường thoát trong bố cục hiện có.
5. Đưa 18 anomaly vào hệ thống chọn anomaly hiện tại của Infinity Room; reset phải khôi phục đầy đủ actor, prop, camera, VFX và audio, gọi lặp vẫn an toàn.
6. Kiểm tra riêng tình huống né được, bị bắt, che khuất spotlight, phạm vi rơi và truy đuổi; bảo đảm không có va chạm gây chết xuyên tường.

## Giả định và phạm vi

- Đây là kế hoạch cho demo độc lập `InfinityRoom5_CrimsonCabaret_Demo`, chưa đánh dấu campaign-ready và không thay đổi campaign/build settings.
- Mỗi anomaly được chọn độc lập theo hệ thống phòng hiện tại; danh sách ID không ép thứ tự xuất hiện.
- Tốc độ đuổi của IR5-18 cần được cân theo tốc độ di chuyển và tuyến đường thực tế trong scene trước khi chốt thông số.
- Mọi thay đổi scene về sau được lưu thủ công trong Unity; công cụ setup chỉ chạy khi người dùng chủ động gọi.
