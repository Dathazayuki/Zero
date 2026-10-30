# Kế hoạch 18 anomaly — Infinity Room 5: The Crimson Cabaret

## Định hướng

**Chủ đề: “Buổi diễn chưa từng kết thúc.”** Căn cabaret ban đầu có vẻ chỉ là một quán bar cũ với sân khấu nhỏ, rèm nhung đỏ và các mannequin trưng bày. Khi anomaly xuất hiện, căn phòng dần biến thành một buổi diễn kỳ quái: nhạc cụ tự chơi, đồ vật phản ứng với người chơi, mannequin thành khán giả, rồi một vị chủ trì bí ẩn chọn người chơi làm tiết mục tiếp theo.

Giữ đúng ngôn ngữ hình ảnh của demo: gỗ tối, đồng xỉn, ánh đèn hổ phách, nhung đỏ cũ và sân khấu đỏ sẫm. Dùng ánh sáng, âm thanh và chuyển động có chủ đích; trạng thái nguy hiểm phải đọc được cả bằng hình ảnh. Bốn anomaly nhỏ cần tinh tế nhưng vẫn nhận ra được khi người chơi quan sát kỹ. Sáu anomaly gây chết người đều có tín hiệu báo trước và đường né rõ ràng; cảnh chết chỉ xảy ra sau khi người chơi thực sự bị bắt hoặc trúng hazard.

## Boss: The Smiling Host — Người Dẫn Chương Trình

Một người đàn ông cao, gầy, mặc vest đen vừa vặn, sơ mi trắng, găng tay và giày da. Khuôn mặt bị che bởi nhiễu đen chuyển động như màn hình mất tín hiệu; chỉ để lộ chiếc miệng đang mỉm cười. Không biến đầu thành TV. Cử chỉ lịch thiệp và chậm rãi: nghiêng đầu, chỉnh cổ tay áo, mời lên sân khấu, vỗ tay.

Boss xuất hiện trong năm anomaly: IR5-11, IR5-12, IR5-16, IR5-17 và IR5-18. Hai anomaly đầu là chạm trán an toàn để người chơi nhận diện hình dáng và hành vi; ba anomaly cuối là encounter có thể gây chết. Không cần thanh máu hay hệ thống chiến đấu.

## Danh sách anomaly

Phân nhóm: **A — 4 anomaly nhỏ**, **B — 5 anomaly rõ ràng**, **C — 3 anomaly kinh dị**, **D — 6 anomaly nguy hiểm**. Tổng cộng 18; trong đó có 5 anomaly liên quan đến Boss và 6 anomaly có thể giết người chơi. IR5-13 và IR5-14 được chuyển từ C sang D.

### A — Bất thường nhỏ, cần quan sát kỹ

| ID | Tên | Biểu hiện | Asset cần có |
|---|---|---|---|
| IR5-01 | Dấu son trên các ly | Nhiều ly ở quầy bar và bàn có 2–3 dấu son đỏ sẫm trên mỗi ly, gần miệng ly. | `Assets/Game/Art/Kiss.png`, mesh dấu son cong và material dùng chung. Demo hiện gán 12 ly, tổng 30 dấu; F3 bật, F2 khôi phục ly sạch. |
| IR5-02 | Phong bì thư kỳ lạ | Một phong bì giấy ngà cũ, nếp gấp rõ và dấu sáp đỏ mang nụ cười đen xuất hiện trên bàn café giữa `IR5V2_Table1`. Normal không có thư; anomaly hiện thư, không gây chết và không yêu cầu nhặt/mở. | Model `IR5V2_StrangeEnvelope.obj` kích thước 24 × 15 cm, material giấy/nếp gấp/sáp/mực; `IR5_02.asset`, dùng lại `LivingRoomArrangementAnomaly`. F3 mục 10 bật, F2 ẩn thư. |
| IR5-03 | Player bị thu nhỏ gần sân khấu | Khi Player bước vào phòng, kích thước giảm dần theo khoảng cách ngang tới mép sân khấu: càng tiến gần càng nhỏ, tối thiểu còn **0,4 lần kích thước gốc**. Khi đi xa sân khấu, kích thước tăng mượt trở lại. Collider và tầm mắt cùng thu nhỏ theo Player, tốc độ đi/chạy giữ nguyên. Khi lớn trở lại, kiểm tra khoảng trống để tránh xuyên gầm bàn/quầy; nếu bị chắn thì chờ tới khi đủ chỗ. F2, rời phòng hoặc death khôi phục kích thước gốc. | `CabaretShrinkAnomaly`, luật khoảng cách C# thuần, CharacterController hiện có, collider sân khấu và vùng phòng; không cần model mới. `IR5_03.asset`; F3 mục **16**, F2 normal. Inspector: `Minimum Scale = 0.4`, `Full Size Distance = 10.5 m`, `Scale Change Speed = 0.7/giây`. |
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
| IR5-10 | Những bàn tay sau rèm | 12 bàn tay bám mép và gấu hai bên rèm. Khi Player bước lên bục diễn, cả hai cụm rụt sau rèm cùng lúc trong 0,65 giây rồi ẩn. F2 khôi phục; không gây chết. | Model `Hand` trong scene được bake pose thành một prefab dùng chung; hai cụm dùng lại `IR5_15_StageZone` và đường rút riêng. |
| IR5-11 | Boss — Vị khách cuối quầy | Boss ngồi trên ghế cuối quầy, phát `Sitting`; rig `mixamorig:Neck` hướng về sân khấu, không quay toàn thân. Sau khi Player đã thấy Boss, nhìn đi liên tục ít nhất 0,3 giây rồi nhìn lại thì hắn biến mất; chiếc ly gần đó rung nhẹ rồi dừng. Không gây chết. | Tái sử dụng TheSmilingHost, `Host.controller`/`Sitting`, ghế cuối quầy và ly hiện có; target sân khấu riêng. Đã gán mục 8 của F3; F2 khôi phục. |
| IR5-12 | Boss — Người đứng sau quầy | Boss đứng idle sau quầy bar. Nhìn trực tiếp không thấy hắn, nhưng nhìn qua ba gương phản chiếu thì thấy hắn đứng tại đó. Chỉ xuất hiện khi anomaly được chọn, không tấn công; F2/đổi anomaly ẩn Boss. | TheSmilingHost, animation idle, ba gương planar và layer MirrorReflectionOnly; không tạo bóng đổ hay collider lộ vị trí trong thế giới thường. |

### D — Nguy hiểm, có thể giết người chơi

| ID | Tên | Tín hiệu và cách thoát | Asset cần có |
|---|---|---|---|
| IR5-13 | Bàn tay thả đèn chùm | Bật anomaly: `Hand` xuất hiện trên `IR5V2_Chandelier`, đèn rung nhẹ. Player vào vùng dưới đèn: rung mạnh trong 1,1 giây rồi rơi tăng tốc trong 1,05 giây; vị trí rơi cố định. Rung/rơi chưa trúng không điều khiển camera hoặc khóa Player; né được không có cutscene. Chỉ khi va trúng mới khóa Player, camera ngước lên trong 0,35 giây rồi cú dập cuối cắt đen bằng kiểu chết dùng chung của IR3-17 (`Freeze Duration = 0`). Bàn tay ẩn khi bắt đầu rơi. | Dùng đèn và bàn tay đã bố trí trong scene, volume tiếp cận và quét va chạm chống xuyên giữa hai bước. Không sửa rig/clip Hand. F2 trả đèn và ánh sáng về vị trí gốc, ẩn bàn tay. |
| IR5-14 | Ban nhạc thôi miên | Player đến giữa phòng thì piano, trống và vĩ kéo tự biểu diễn. Nhạc khiến mắt tối mờ dần, camera chao và thở dốc bằng cơ chế mở đầu game. Có 18 giây (chỉnh Inspector) chạy qua lối vào phía nam; ra kịp thì nhạc dừng và hồi phục, quá hạn thì cắt đen và chết. Rời vùng giữa phòng không ngừng đếm. | Nhạc vòng lặp piano/bass/trống tự tổng hợp; phím đàn và vĩ kéo có sẵn, hai dùi trống, CentreZone/EscapeZone; SceneFadeController, API thở dốc/chao của Player và PlayerDeathController dùng chung. F2 reset toàn bộ; F3 mục 14 chọn anomaly. |
| IR5-15 | Mannequin đuổi trong ánh tối | Player bước lên bục diễn thì màn hình chớp đen 0,18 giây rồi sáng 0,8 giây, lặp đến khi thoát hoặc chết. Trong lần tối đầu, sáu mannequin chase lấy vị trí và hướng đứng thực tế của sáu mannequin trang trí được gán trong Inspector, ẩn các mannequin trang trí rồi hiện nhóm chase. Mỗi lần tối, chúng tiến từng đoạn 1,1 m theo đường NavMesh để vòng qua nội thất; khi sáng, vị trí và pose animation đều bất động. Trong tối, mannequin tới gần 0,85 m và không bị vật cản che sẽ bắt Player và cắt đen gây chết. Ra qua lối vào phòng thì an toàn, ngừng chớp và ngừng đuổi. F2/reset ẩn nhóm chase và khôi phục trạng thái ban đầu của nhóm trang trí. | Tái sử dụng `Cistern_ChaseMannequin` và Animator của Minigame 5, một prefab `PF_IR5_BlinkMannequin` dùng chung cho sáu instance có NavMeshAgent; NavMesh của phòng, StageZone, EscapeZone, SceneFadeController và PlayerDeathController dùng chung. Mỗi `CabaretBlinkMannequin` có ô `Standing Mannequin` để kéo thả mannequin trang trí tương ứng; demo gán `IR5_DisplayMannequin_1` đến `_6`, giữ mannequin không hậu tố ở sân khấu. Các thời gian và bước di chuyển chỉnh trong Inspector. F3 chọn IR5-15. |
| IR5-16 | Boss — Cú búng tay | Boss ban đầu ngồi trên ghế. Khi Player lại gần trong 3 m và không bị vật cản che, hắn đứng dậy rồi nâng tay phải cạnh đầu búng một lần; tay còn lại sau lưng. Ngay cú búng, hố đen xuất hiện tại vị trí chân Player và báo trước 1,2 giây. Vị trí hố được chốt, không tiếp tục bám theo Player. Hố bán kính 0,95 m, mở trong 2 giây; rời vùng trước khi mở là sống, còn đứng trong vùng hoặc bước vào hố đang mở thì rơi tự do rồi chết. Không khóa điều khiển trước khi thực sự rơi. | TheSmilingHost và ghế dùng lại; controller riêng tái sử dụng các clip đã đặt sẵn. Shader và ống sâu hố đen tạo trong Unity; dùng hệ thống chết/hồi sinh hiện có. Đã gán vào demo, mục thứ 7 của F3; F2 khôi phục bình thường. |
| IR5-17 | Boss — Dụ đòn dưới ánh đèn đỏ | Khi Player bước vào phòng, cửa đôi đóng sập; đèn phòng tắt lần lượt trong **4 giây**, sau đó bật spotlight Player và Host. Host biểu diễn `Host_Conducting`; **12 flying eye** bay lên trong **1,6 giây**, **232 dấu mắt** phủ tường/trần. Spotlight **bám theo Player** trong thời lượng ngẫu nhiên quanh giá trị Inspector `Tracking Seconds` (mặc định 6 giây, biên độ `Tracking Variation Seconds` 1,5 giây), sau đó chuyển vàng và **khóa vị trí 1,25 giây**. Player phải dụ vùng sáng phủ một mắt đặc biệt rồi rời vùng đã khóa. Đèn chuyển đỏ trong **0,6 giây**, đàn mắt bắn vào vùng sáng đã khóa; từ giây **0,25** của đòn bắn, Player còn chạm vùng này sẽ chết, còn mắt đặc biệt trong vùng sẽ bị phá. Mắt chạy né từng quãng quanh vị trí đặt khi đèn đang bám, dừng chạy trong cảnh báo/đòn bắn; tròng mắt đảo liên tục. Mỗi mắt bị phá khép lại và biến mất, đồng thời bốn flying eye rơi xuống; đèn bám nhanh hơn và chu kỳ chuẩn bị ngắn lại. Sau nhịp nghỉ **0,8 giây**, đèn tiếp tục bám Player. Phá đủ ba mắt: Host ngồi xuống cúi người, sau **2,5 giây** cửa mở và đèn phòng trở lại. | Ba mắt đặc biệt dùng chung `PF_IR5_RitualSpecialEye`, ghép hai sprite `Assets/Game/Art/Eyelid.png` và `Eyeball.png`; giữ `Eye.png` cho mắt trang trí trên tường/trần. Tái sử dụng Host, flying eye, cửa và shader hiện có. `CabaretRitualRules` giữ luật; `CabaretRitualEye` trình bày mắt và chạy né trên NavMesh. F3 mục **18**; F2 khôi phục vị trí, sprite, ánh sáng, cửa và toàn bộ encounter. |
| IR5-18 | Boss — Tiết mục hạ màn | Host đứng trên sân khấu với clip `Host_Idle` (state `IR5_12_HostIdle` trong Animator `Host`). Khi Player tới gần 3,5 m, phát `CelebrateBow`; đợi cúi chào xong mới lan vùng nhiễu từ vị trí đầu Boss. Vùng nhiễu đen như mặt Host phình rộng 1,8 m/giây, tối đa bán kính 18 m, nuốt dần sân khấu và cả phòng. Hình ảnh bị xé ngang, lệch màu và giật nhịp; ranh giới khối nhiễu hiện rõ trước vùng gây chết. Chạm vùng nhiễu bằng bất kỳ phần nào của capsule Player sẽ cắt đen và chết. Chạy qua lối vào phía nam thì an toàn, ngừng hiệu ứng và ngừng lan. | `Host.controller`/`CelebrateBow`, bản sao TheSmilingHost, shader URP `CabaretGlitchSpaceURP`, một mesh khối nhiễu và lớp xé hình riêng cho camera Player; `CabaretCurtainCallAnomaly`, luật bán kính C# thuần và `PlayerDeathController`. F3 mục **17**, F2 khôi phục Host, hiệu ứng và tùy chọn camera. Khoảng kích hoạt, tốc độ lan, bán kính tối đa và cường độ nhiễu chỉnh trong Inspector; không đổi thiết lập render toàn dự án. |

## Nhịp độ và khả năng đọc tình huống

- Dùng đường đi trung tâm làm tuyến chính; bar, booth và bàn tạo các chỗ né/che có thể nhìn thấy trước. Không để đạo cụ hoặc mannequin chặn lối thoát.
- Tăng căng thẳng theo nhịp có khoảng nghỉ: giới thiệu dấu hiệu nhỏ, cho nhạc cụ tự diễn, chuyển sang hiện tượng kinh dị, rồi mới đặt người chơi trước hazard hoặc Boss. Tránh để nhiều encounter chết người nối tiếp không có nhịp thở.
- Mỗi hazard có dấu hiệu hình ảnh nhất quán: ánh đỏ cho nguy cơ, bóng/đường vùng rơi cho phạm vi, cử động rõ và khoảng báo trước cho truy đuổi. Âm thanh bổ trợ chứ không phải tín hiệu duy nhất.
- Boss không cần xuất hiện theo thứ tự bắt buộc. Hai lần chạm trán an toàn giới thiệu hình ảnh và cử chỉ; ba encounter nguy hiểm vẫn phải tự giải thích được dấu hiệu và đường thoát, không đòi hỏi người chơi đã gặp Boss trước đó.
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
3. Làm một mẫu Boss trong ngữ cảnh cabaret, rồi tái sử dụng rig/asset cho IR5-11, IR5-12, IR5-16, IR5-17 và IR5-18.
4. Làm nhóm B và C, sau đó nhóm nguy hiểm D; xác nhận từng cảnh báo và đường thoát trong bố cục hiện có.
5. Đưa 18 anomaly vào hệ thống chọn anomaly hiện tại của Infinity Room; reset phải khôi phục đầy đủ actor, prop, camera, VFX và audio, gọi lặp vẫn an toàn.
6. Kiểm tra riêng tình huống né được, bị bắt, che khuất spotlight, phạm vi rơi và truy đuổi; bảo đảm không có va chạm gây chết xuyên tường.

## Giả định và phạm vi

- Đây là kế hoạch cho demo độc lập `InfinityRoom5_CrimsonCabaret_Demo`, chưa đánh dấu campaign-ready và không thay đổi campaign/build settings.
- Mỗi anomaly được chọn độc lập theo hệ thống phòng hiện tại; danh sách ID không ép thứ tự xuất hiện.
- Tốc độ lan vùng nhiễu của IR5-18 cần được cân theo tốc độ di chuyển và tuyến thoát thực tế trong scene trước khi chốt thông số.
- Mọi thay đổi scene về sau được lưu thủ công trong Unity; công cụ setup chỉ chạy khi người dùng chủ động gọi.

## IR5-17 — Bố trí mắt trên tường và trần

- Dùng 232 instance của `PF_IR5_RitualEye`: mỗi tường bên 48 mắt, tường bắc 32 mắt, tường nam 24 mắt và trần 80 mắt.
- Rải lệch vị trí, thay đổi góc nghiêng và kích thước từ 0,65 đến 4,6 lần, xen một số mắt đảo ngược. Không xếp thành hàng hoặc ô lưới; khoảng cửa vào được chừa trống.
- Các instance dùng chung mesh, material và ảnh gốc `Assets/Game/Art/Eye.png`; bật static batching cho phần trang trí cố định này.
- Khi phá cả ba mắt đặc biệt, mắt trên tường/trần biến mất và toàn bộ mắt bay rơi xuống. F2 ẩn mọi mắt của encounter và khôi phục phòng.

### IR5-17 — Chu kỳ dụ đòn và độ khó

| Số mắt đã phá | Tốc độ bám đèn | Thời gian bám ngẫu nhiên | Báo vàng, khóa vị trí |
|---|---:|---:|---:|
| 0 | 1,8 m/s | 4,5-7,5 giây | 1,25 giây |
| 1 | 2,52 m/s | khoảng 3,21-5,36 giây | khoảng 1,09 giây |
| 2 | 3,24 m/s | khoảng 2,5-4,17 giây | khoảng 0,96 giây |

- Vùng bắn bán kính 1,35 m. Kiểm tra Player gồm cả bán kính CharacterController; đứng sát mép nhưng thân còn chạm vùng đỏ vẫn nguy hiểm.
- Điểm bắn khóa ngay khi chuyển vàng, không kéo theo Player trong lúc cảnh báo hoặc laser đỏ. Đứng ngoài vùng sáng ở nhịp bám không bị phạt.
- Cả ba mắt đứng yên ở lượt đầu. Sau khi mắt đầu tiên bị phá và khép lại, từ nhịp bám đèn kế tiếp, những mắt còn lại bắt đầu chạy đến các điểm ngẫu nhiên khắp vùng sàn dưới sân khấu; đích mới cách vị trí hiện tại ít nhất 2,8 m và giữ khoảng cách an toàn 2,5–3 m với Player lúc chọn đích. Mắt ở gần ưu tiên chạy xa Player hơn; mắt đã ở xa vẫn chạy ngẫu nhiên, không bị buộc phải tìm nơi xa hơn nữa. Mắt trốn Player, không trốn tâm ánh đèn; nếu Player chặn đầu trong 3 m thì chọn lại tuyến. Mắt tiếp tục chạy dù ánh đèn đã ở xa, nghỉ ngắn 0,6 giây rồi chọn điểm mới; không chỉ bước né theo hướng ánh đèn. Không giới hạn khoảng cách với điểm xuất phát (`Roam Radius = 0`), tốc độ 0,95 m/s. `Movement Area` trỏ đến BoxCollider `IR5_17_EyeMovementArea`, chừa mép phòng và kết thúc trước bậc sân khấu. Tuyến NavMesh phải hoàn chỉnh, toàn bộ các góc tuyến ở trong vùng và cùng độ cao sàn, tránh đi vòng qua bục sân khấu. Chuyển vàng khóa điểm và lúc bắn đỏ thì mắt tạm dừng, nhịp bám kế tiếp tiếp tục tuyến đang chạy. F2 xóa tuyến và khôi phục vị trí; dùng seed để lặp lại đường chạy khi playtest. Không có tuyến hợp lệ thì chờ chọn lại, không đi xuyên vật cản.
- Khi bị phá, viền mắt khép trong 0,65 giây, tròng mắt co theo rồi cả mắt biến mất. Mắt đã phá không kích hoạt lần nữa; reset trả cả ba mắt về trạng thái ban đầu.
- Sau mắt đầu tiên, cả hai mắt sống di chuyển độc lập. Khi mắt thứ hai bị phá, mắt sống cuối cùng tăng tốc 1,5 lần tốc độ gốc: mặc định từ 0,95 lên khoảng 1,43 m/s. F2 trả hệ số tốc độ về bình thường.
- Sau khi đèn phòng đã tắt hoàn toàn, trước khi hiện các mắt và spotlight, tắt `IR5_CrimsonCabaret/IR5V2_DetailPass/Detail` cùng các object con. Hoàn tất anomaly sẽ khôi phục Detail ngay lúc đèn phòng sáng lại; F2, bị bắt chết hoặc vô hiệu hóa encounter cũng khôi phục đúng trạng thái ban đầu. Không bật/tắt riêng lẻ từng object con.
