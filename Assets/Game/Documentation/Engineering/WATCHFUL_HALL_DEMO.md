# The Watchful Hall — Minigame 4 standalone gameplay demo

## Mở demo

Scene: `Assets/Game/Scenes/Demos/Minigame4_WatchfulHall_Demo.unity`.
Nhấn Play để đi kiểm tra bằng WASD, chuột nhìn và Shift chạy.
Scene dùng lại `FirstPersonController`, không cần mở campaign.

Demo đã có bốn ô nạp và khóa cửa đích, cùng sự kiện completion khi đi qua cửa đã mở.
Đã có hai Watcher trên cao qua mảng tùy chỉnh số lượng, hai Patrol dưới đất, phát hiện/capture trong tủ ở cự ly gần
và reset về entrance. Chưa nối campaign, multiplayer hoặc save checkpoint giữa lượt.
Không đánh dấu Minigame4 ready và
không thêm vào Build Settings. Tủ mở cố định để kiểm tra kích thước/lối vào;
không có interaction hoặc cơ chế cửa tự đóng trong bản này.

## Luật nạp đã triển khai: mở bốn khóa

- Cửa đích ban đầu bị khóa và có bốn biểu tượng khóa màu đỏ.
- Bốn ô kích hoạt màu đỏ đặt ở các khu vực khác nhau trên cùng map, mỗi ô tương ứng
  với một biểu tượng trên cửa. Player có thể chọn thứ tự kích hoạt.
- Đứng yên trên ô 1,5 giây để ổn định, sau đó nạp đủ 15 giây trên thanh năng lượng.
  Khi đầy, ô chuyển xanh và đúng một biểu tượng tương ứng trên cửa cũng chuyển xanh.
- Ô đã hoàn tất giữ trạng thái đến hết lượt. Đủ bốn ô xanh mới mở khóa cửa;
  Player phải đi qua cửa đã mở để thắng. Chạm cửa sớm hoặc chỉ nạp xong ô cuối chưa thắng.
- Di chuyển hoặc rời ô sẽ dừng nạp nhưng giữ tiến trình. Khi quay lại/đứng yên phải ổn định
  lại 1,5 giây. Ra/vào liên tục không cộng thời gian ổn định thành thời gian nạp.
- Có thể xoay camera nhìn quanh khi đứng yên. Player phải chạm đất và chân nằm trên ô;
  đi qua vùng trigger hoặc đứng trên console không được tính.
- `ResetAttempt()` xóa mọi tiến độ, trả ô/biểu tượng về đỏ, khóa cửa và reset AI.
  Capture đưa Player về entrance sau animation đánh và fade đen ngắn.

Ô nạp có cover gần đó, phân bố hai bên sảnh. AI tương lai cần cho phép cửa sổ nạp hữu ích
sau 1,5 giây ổn định và có đường rút về cover; không cần 15 giây liên tục vì giữ nạp dở.

## Bố cục

- Hall chính khoảng 60 × 84 m; trần cao 10 m. Chiều ngang tăng 2,5 lần
  từ 24 m bằng chỉnh từng phần kiến trúc, không scale toàn bộ map.
- Arrival tại (-5, 0.02, -44), exit marker tại (0, 0.02, 39).
  Khoảng cách thẳng tăng từ khoảng 43 m lên 83 m.
- Tám cụm vách lệch bên, 14 hốc trú và 39 tủ rỗng cung cấp cover.
  Bốn vách mới chắn xen kẽ tuyến giữa để khuyến khích đổi bên; không bắt buộc
  Player đi theo một đường ray vô hình.
- Ba gallery hiện tại được mở rộng ngang tới 60 m, giữ nguyên độ cao,
  vị trí dọc sảnh, lan can và dầm đỡ.
- `WH_Column` chính tăng từ 14 lên 28. Tính cả cột trong mười nhịp
  `WH_ExtensionBay`, tổng cột tăng từ 34 lên 68, bố trí ở X=±8 và ±22.
- Watcher giữ scale 2.7 trên gallery; Patrol dưới đất nằm trong
  `WH_Threats/WH_PatrolLeft/Right`, visual scale 1 (cao khoảng 2.2 m).
- Camera/Player chỉ dành cho demo. Khi tích hợp additive phải bỏ Player demo
  và bind Player thật qua bootstrap của campaign.

Skill level-design được dùng để tách nơi quan sát an toàn, các đoạn băng qua
khoảng trống, các vị trí nạp và đích thoát. Bốn lần nạp hoàn tất cần ít nhất
60 giây nạp cộng 6 giây ổn định; thời gian đi lại, trốn và ổn định lại cộng thêm. Mục tiêu cũ 3–5 phút
cần playtest lại với luật mở khóa và thời gian quét/nhìn đi nơi khác của AI.

## Chỉnh sửa

Root `WatchfulHallDemo` chia thành Architecture, Cover, ServiceDetail,
UpperGallery, Lighting, Markers và DemoPlayer.

- Tủ: `Assets/Game/Prefabs/WatchfulHallDemo/PF_WH_HidingCabinet.prefab`.
  Có vách, đáy, nóc, hai cửa mở, bản lề, tay nắm và khe thông gió.
  Cánh mở ra ngoài 100°, mép cánh nằm tại bản lề X=±0.8, Z=-0.73;
  tay nắm/khe gió được căn theo cùng phép xoay. Đây vẫn là pose tĩnh, chưa có
  hinge Transform hoặc animation đóng/mở. Chi tiết nhỏ không còn collider.
- Vách: `Assets/Game/Prefabs/WatchfulHallDemo/PF_WH_ServicePartition.prefab`.
  Cao 2.7 m, có nhánh che bên, khung thép, panel, chân đế, nẹp trên,
  hộp điện, conduit và khe thông gió. Bốn collider chính giữ cover kín;
  chi tiết trang trí không có collider active.
- Nhịp sảnh mở rộng: `Assets/Game/Prefabs/WatchfulHallDemo/PF_WH_ExtensionBay.prefab`.
  Mỗi nhịp dài 4 m; mười instance dùng chung mẫu tường, cột, dầm, gạch sàn,
  đường ống, máng gió và fixture đèn. Sàn/trần chịu lực vẫn là hai slab liên tục.
  Prefab đã cập nhật cho sảnh rộng 60 m; mỗi nhịp có bốn cột và 15 ô gạch.
- Cột bổ sung: `Assets/Game/Prefabs/WatchfulHallDemo/PF_WH_ColumnModule.prefab`.
  Một mẫu chung gồm thân cột và chân đế; các instance bổ sung giữ nguyên kích thước.
- Gạch bổ sung: `Assets/Game/Prefabs/WatchfulHallDemo/PF_WH_FloorTile.prefab`.
  Toàn sảnh có 315 ô trên lưới 4 m; không kéo giãn gạch để phủ chiều ngang mới.
- Đèn fill bổ sung: `Assets/Game/Prefabs/WatchfulHallDemo/PF_WH_FillLight.prefab`.
  14 instance ở hai hàng X=±22, tái sử dụng cấu hình đèn fill hiện có.
- Hốc kỹ thuật: `Assets/Game/Prefabs/WatchfulHallDemo/PF_WH_ServiceRecess.prefab`.
  Sáu instance `WH_ServiceRecess_Left/Right_1…3` thay toàn bộ 18 khối
  `WH_AlcoveBack/Cheek/Roof` cũ. Hốc rộng khoảng 2.8 m, sâu 2.9 m, cao 3.1 m;
  cửa vào hướng local -Z, có khung thép, panel trong/ngoài, nẹp chân,
  mái che và fixture. Lòng hốc để trống, không đặt tủ/xe đẩy xuyên panel.
  Đây là cover môi trường, không phải elevator hoặc cơ chế interaction.
- Material riêng: `Assets/Game/Materials/WatchfulHallDemo/`.
  Tái sử dụng texture bê tông có sẵn và model xe đẩy của Kitchen demo;
  không thay đổi material/model nguồn.
- Chi tiết đường ống, máng thoát, đèn và tủ điện nhỏ không có collider active.
  Tường, sàn, vách cover và tủ trú giữ collider vật lý.
- Chỉ spotlight quan sát có shadow; các fill light không có shadow.
- Các marker chỉ là Transform để nối gameplay sau; không có callback ngầm.

Không tạo script dựng scene, RSP, auto-save/rebuild hoặc package mới.
Các thay đổi scene được dựng qua UnitySkills và cần người dùng Ctrl+S.

### Ô nạp và cửa cơ khí

- Root: `WatchfulHallDemo/WH_Objectives`, component `WatchfulHallSession`.
  `Charge Seconds = 15`, `Settle Seconds = 1.5`, `Movement Tolerance = 0.03 m`.
  Tolerance loại sai số vị trí nhỏ; input di chuyển cũng dừng nạp, xoay camera thì không.
- Bốn instance `WH_ActivationPad_01…04` dùng chung `PF_WH_ChargeStation`:
  vị trí (X,Z) lần lượt (-11,-37), (19,-13), (-19,11), (12,30).
  `WatchfulHallChargePad` gán zone, Slider và năm renderer đổi màu.
- `PF_WH_LockedExitGate` tại (0,0,37.9), hai bản lề mở vào trong ±90°.
  Bốn glyph dùng chung `PF_WH_LockIndicator`; thứ tự khóa 01…04 khớp thứ tự pad.
- Ba prefab nằm trong `Assets/Game/Prefabs/WatchfulHallDemo/`.
  Reference gameplay hiện được gán ở scene instance, không dùng Apply All vào prefab.
- `WatchfulHallChargeRules` là chủ sở hữu tiến trình; session đọc vị trí/grounded và cập nhật
  thanh nạp/đèn/khóa. Chưa nối campaign hoặc đổi `Minigame4.asset` sang ready.

### Mảng Watcher và hai Patrol

- `WH_Threats > WatchfulHallThreatDirector > Watchers`: đổi Size và kéo thả mỗi Watcher một lần;
  không giới hạn một/hai con, không được để phần tử rỗng hoặc trùng reference. Director còn nối hai Patrol, 39 tủ,
  entrance và fade. Session gọi bind/reset; không tìm Player ngầm trong scene.
- Mỗi `WatchfulHallWatcher`: gán `Neck Bone` vào `mixamorig:Neck`, `Animator` vào Animator
  của model, `Patrol Start/End` vào hai mốc `WH_WatcherRoute/Start/End` dưới gallery tương ứng.
  Hai tuyến hiện nằm giữa cột, X từ -5 tới +5 m. Thân chỉ đổi hướng khi tới cuối tuyến;
  cổ quét ±75°, tốc độ 15°/s, không xoay liên tục toàn model.
  `Patrol Speed` (2.4 m/s), `Turn Degrees Per Second` (120), `Walk Animation Speed` (0.45)
  có thể chỉnh riêng từng con. Root motion tắt, Animator dùng Always Animate.
  Giữ `View Distance` (65 m), `View Angle` (100°), `Detection Seconds` (1.5).
  `WH_WatcherEye` nằm dưới `mixamorig:Head`, đi theo cổ và đèn dần đỏ khi thấy Player.
- Reference cũ `watcher`/`additionalWatcher` được chuyển tường minh sang `watchers` trong
  scene demo bằng Editor; không có hook tự rebuild hoặc tự lưu scene khi compile.
- Hai Patrol trong `WH_Threats`: chỉnh `Patrol Speed` (2.2), `Chase Speed` (5.6),
  `View Distance` (22 m), `View Angle` (110°), `Search Seconds` (8),
  `Cabinet Inspection Distance` (3 m), `Catch Distance` (1.15 m ngoài tủ).
  Trong tủ, bắt khi Patrol tới trong 1.4 m phía cửa vào có LOS rõ.
- `WH_PatrolRoutes/WH_RouteLeft/Right` chứa tám waypoint mỗi tuyến, sửa bằng Transform.
  Khi Watcher báo động, chỉ Patrol gần nhất theo độ dài đường đi nhận thông tin;
  cập nhật cùng Patrol đó khi còn nhìn thấy, không đọc vị trí xuyên cover khi đã mất dấu.
- Patrol tự thấy Player thì chase, mất dấu thì tới vị trí cuối, tìm quanh rồi về tuyến.
  Không cần rời tủ là lập tức được tha: đứng gần cửa tủ có thể bị kiểm tra.
- `WatchfulHallPatrol.controller` dùng clip Walk/Run/Attack sẵn có; đi/chạy đã bật Loop,
  chạy/đánh nhập Humanoid để khớp Avatar. Root motion tắt; NavMeshAgent điều khiển vị trí.
- `WH_Navigation` dùng PhysicsColliders và volume chỉ thu vùng sàn, không thu mặt gallery.
  Khi sửa geometry: chọn object này rồi chạy `Tools > Zero > Watchful Hall > Bake Selected Navigation`,
  sau đó Ctrl+S. Công cụ chỉ chạy qua MenuItem, không tự bake/rebuild/save scene khi compile.
- Capture: khóa input, dừng AI/nạp, animation đánh, fade đen ngắn, teleport entrance,
  reset toàn bộ ô/khóa/AI và khôi phục input. Disable hủy replay, không teleport trễ.
  Khi bị bắt, Patrol quay về Player; thân Player và camera xoay mượt từ góc hiện tại
  đến đầu Patrol trong 0.35 giây rồi nhìn theo đầu trong cảnh đánh. Chỉnh `Catch Look Seconds`
  trên `WatchfulHallThreatDirector` (0.1–0.6 giây). Hủy replay khôi phục hướng cũ của Player, camera và Patrol;
  reset thành công dùng hướng của entrance. Hướng nhìn không đổi vị trí bắt.
- Chỉ hỗ trợ Player demo hiện tại; chưa thêm networking hoặc đồng bộ nhiều Player.
- Tiến trình chỉ lưu trong lượt chơi runtime, không ghi save game xuống đĩa.

### Dấu hiệu đang nạp

- Sau 1,5 giây ổn định, mặt ô và bốn đèn viền chuyển sang vàng hổ phách, sáng nhấp nhô
  mềm khoảng một chu kỳ/giây. Màn hình bệ hiện `CHARGING` màu vàng trong khi thanh nạp chạy.
- Chưa đủ thời gian ổn định thì không có hiệu ứng nạp. Di chuyển/rời ô sẽ dừng hiệu ứng;
  nếu đã tích lũy năng lượng thì màn hình hiện `PAUSED`, thanh giữ nguyên tiến trình.
- Đủ 15 giây: ngừng nhấp nhô, ô/khóa chuyển xanh và màn hình hiện `ACTIVATED`.
- `WatchfulHallChargePad.Status Label` gán vào `WH_CS_ChargeLabel` của chính bệ đó.
  Hiệu ứng dùng renderer hiện có và MaterialPropertyBlock, không sửa material dùng chung,
  không thêm particle package hoặc tạo vật thể VFX mỗi frame. Reset/disable trả lại màu gốc.
- Kiểm thử sau khi thêm feedback: EditMode 12/12 và PlayMode 3/3 pass trong scene đã lưu.
  Kiểm tra ngưỡng ổn định, màu emission vàng và chữ CHARGING, dừng/khôi phục màu khi rời ô
  hoặc disable, chữ ACTIVATED khi hoàn tất và regression luồng bốn khóa. Scene sạch sau test.
  Code mới không có policy debt; 13 lỗi policy ở các file ngoài phạm vi vẫn không đổi.

## Kiểm tra

Preview: `Documentation/Engineering/WatchfulHall_Preview.png`.

- Người dùng đã Ctrl+S; scene sạch sau khi thoát Play Mode.
- Play Mode mở được, Player đứng tại arrival; Console query không có error/exception.
  Hai kết quả trên thuộc bản 44 m; bản mở rộng cần kiểm tra Play lại sau Ctrl+S.
- Bản 84 m: spherecast bán kính 0.34 m và boxcast rộng 0.68 m, cao 1.8 m
  đi qua mười đoạn của tuyến gợi ý dài khoảng 115 m đều không gặp vật cản.
  Đây là một tuyến đổi bên qua cover, không phải độ dài đường đi ngắn nhất.
- Boxcast cùng kích thước từ trước cửa vào lòng cả 11 tủ đều thông.
  Tủ số 2 đã dời khỏi vách hốc tường; bốn tủ mới nằm ở các bay bên, tránh
  cản tuyến chạy chính. Kiểm tra sau khi re-enable collider để tránh query
  dùng pose physics cũ trong Edit Mode.
- Lối gợi ý (X,Z): (-5,-44), (-5.7,-40), (-5.7,-32), (5.7,-30), (5.7,-22),
  (-5.7,-20), (-5.7,-12), (5.7,-10), (5.7,-3), (0,0), (0,39).

### Kiểm tra bổ sung khi thay hốc blockout

Ảnh cận cảnh: `Documentation/Engineering/WatchfulHall_RecessPreview.png`.

- Không còn object mang tên `WH_Alcove` trong scene.
- Tủ 3/4 dời tới X=±10.4, tránh cột. Tủ 5 tới (10.4,0,25.5),
  không lấn hốc cuối. Xe đẩy 3/5 tới (10.55,0,22) và (10.55,0,34).
- 33 overlap-box kiểm tra thân và hai cánh cửa của 11 tủ không thấy collider
  ngoài hierarchy tủ giao vào vùng kiểm tra. Đây là kiểm tra giao hình học,
  khác với kiểm tra đường đi vào lòng tủ ở đợt trước.
- 15 kiểm tra từ world renderer bounds của ba mesh trên mỗi xe đẩy không
  giao collider kiến trúc/tủ. Không dựa vào collider xe vì model xe không có collider.
- 27 boxcast kích thước Player đi qua 11 cửa tủ, 6 cửa hốc và 10 đoạn tuyến đi
  đều thông sau khi cập nhật pose collider.
- Dời 10 cụm tủ điện treo tường nguyên bộ, giữ vị trí tương đối của tay nắm/khe gió.
  90 kiểm tra world renderer bounds của các chi tiết không giao collider kiến trúc/tủ;
  tránh để phần trang trí không có collider xuyên vào hốc hoặc tủ trú.
- Preview chụp bằng camera tạm đổi pose rồi trả lại đúng pose ban đầu trong
  cùng batch; không đổi điểm spawn Player. Đã chạy smoke Play Mode sau khi thay hốc:
  Player active và Console query không có error/exception, sau đó thoát Play Mode.
  Scene còn dirty do lần dời tủ điện cuối; người dùng cần Ctrl+S để lưu phần này.
  Các kiểm tra trên không chứng minh AI hide/detection đã có.

Đây là kiểm tra smoke/physics, không phải test AI hoặc đánh giá toàn bộ đường
đi bằng input. Chưa đo FPS trên máy đích và chưa nghiệm thu độ khó minigame.
Mức art là demo dùng asset hiện có, không phải chất lượng render cuối như ảnh
concept; lighting/texture cần tiếp tục art pass khi chốt gameplay.

## Mở rộng chiều ngang — 2026-10-15

- Sàn, trần và tường đầu rộng 60 m; hai tường bên nằm ở X=±30.
  Hai mảng tường cuối mở rộng tới mép sảnh, giữ khoảng cửa thoát giữa rộng 3 m.
- Tủ, hốc trú, xe đẩy và các cụm tủ điện gần tường được dời nguyên cụm;
  giữ kích thước và khoảng cách tương đối giữa các chi tiết.
- Thêm 28 tủ từ `PF_WH_HidingCabinet` và 8 hốc từ `PF_WH_ServiceRecess`.
  Các instance mới nằm trong `WatchfulHallDemo/WH_Cover/WH_WideHallCover`.
  Hàng tủ trong ở X=±13.5; hàng ngoài ở X=±24.5 quay cửa về phía trong sảnh;
  các hốc bổ sung ở X=±17. Tủ ngoài cuối hàng ở Z=35 để tránh chân cột.
- Tủ cũ `WH_HidingCabinet_4` dời tới (-26.4,0,21), tránh điểm tiếp cận
  phía sau hốc `WH_ServiceRecess_Left_2`.
- Kiểm tra hình học Edit Mode: cửa vào cả 39 tủ và 14 hốc đều thông với
  boxcast rộng 0.68 m, cao 1.7 m; vùng bắt đầu các lượt cast không có vật cản.
  Tuyến giữa gồm 10 đoạn và hai tuyến bên gồm tổng 10 đoạn đều thông.
  Tuyến bên đi từ arrival tới Z=-42 trước khi rẽ ra X=±19 để vòng chân cột.
- 111 overlap-box từ bounds collider của thân/cánh tủ và vách hốc mới,
  cùng tủ cũ đã dời, không phát hiện collider ngoài cụm giao vào vùng kiểm tra.
  Bounds được thu nhỏ 0.025 m mỗi cạnh để loại tiếp xúc ở mép.
- Scene validation không phát hiện missing script/prefab; Console query
  không có Error. Không thêm code runtime hoặc chạy Play Mode trong đợt này.
- Camera preview tạm đã được xóa; trần bật lại. Scene còn chưa lưu;
  người dùng cần Ctrl+S trong `Minigame4_WatchfulHall_Demo`.

Ảnh preview: `WatchfulHall_60m_Entrance.png`, `WatchfulHall_60m_Cover.png`
và `WatchfulHall_60m_Layout.png` trong cùng thư mục tài liệu.

## Kiểm thử cơ chế nạp — 2026-10-15

- Người dùng đã lưu scene sau khi gán reference; scene sạch, đã thoát Play Mode sau test.
- `WatchfulHallChargeRulesTests`: 11/11 pass, gồm bảy trường hợp luật nạp và bốn
  kiểm tra policy cho từng file runtime mới. Không tăng baseline hoặc thêm exception.
- `WatchfulHallGameplayTests`: 2/2 pass trong scene đã lưu: đứng đủ thời gian, rời ô giữ
  tiến trình, di chuyển phải ổn định lại, disable/enable không mang theo timer ổn định cũ,
  nạp cả bốn ô, thanh nạp/màu xanh, mở cửa, completion một lần và reset đóng cửa.
  Test tăng time scale để chạy nhanh; chưa phải phép đo thời lượng người chơi thực tế.
- Compile hoàn tất; không có lỗi biên dịch trong lượt test. Validator toàn dự án vẫn báo
  13 lỗi policy ở các file ngoài phạm vi sửa (BackroomMonsterController, Room2LightController,
  FlickeringLightController và các file legacy khác); bốn file Watchful Hall mới không có debt.
- Các test chưa nghiệm thu AI, capture/death, multiplayer, FPS hoặc độ khó toàn màn.

## Tích hợp AI ba quái — 2026-10-15

- Đã gán ba model hiện có, hai tuyến tám điểm và đủ 39 reference tủ. 16 đường đi từ
  điểm spawn tới waypoint đều trả `PathComplete` trên NavMesh đã bake.
- `WatchfulHallThreatGameplayTests`: 7/7 pass trong scene đã lưu, gồm đi tuần/animation
  Humanoid lặp, báo đúng Patrol gần nhất, ngưỡng phát hiện Watcher, ngừng cập nhật sau khi
  Player khuất, chase/search/về tuyến, tủ xa an toàn/tủ gần bị bắt và reset, hủy replay khi
  disable, cùng fail-safe không khóa cứng ô nạp khi thiếu reference AI.
- `WatchfulHallGameplayTests`: 3/3 regression pass; tắt director trong fixture để kiểm tra
  độc lập luật nạp/feedback/bốn khóa, không để AI làm nhiễu các test mục tiêu.
- EditMode: 12 test nạp và 10 test pursuit/policy đã pass. Không tăng baseline.
- Các gallery/vách thật vẫn che ánh nhìn Watcher. Không bỏ qua collider để nhìn xuyên sàn;
  vùng đầu/cuối dưới gallery cần Patrol kiểm soát thay vì giả định Watcher thấy toàn map.
- Scene do người dùng Ctrl+S, không tự ghi đè. Không thêm package hoặc gọi dịch vụ tạo model.
- Chưa test multiplayer, chưa đo FPS máy đích hoặc cân bằng thời lượng/độ khó bằng người chơi.
