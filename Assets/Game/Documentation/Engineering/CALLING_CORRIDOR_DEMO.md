# Minigame 3 — The Calling Corridor: Catch the Rabbit

## Chơi demo

Mở `Assets/Game/Scenes/Demos/Minigame3_CallingCorridor_Demo.unity`, lưu bằng Ctrl+S rồi Play.
WASD di chuyển, mouse-look, Shift chạy; không cần interaction.
`Minigame3.asset` vẫn Content Ready = false; demo chưa được đưa vào campaign chính.

## Luật hiện tại

- Sáu cửa và một hành lang dùng chung. Mỗi lượt có đúng một phòng chứa Rabbit; năm phòng còn lại chứa SpiderHorror.
- Cửa tự mở khi tiếp cận. Đi qua lối hẹp và góc rẽ, vào vùng kiểm tra bên trong phòng mới xác nhận lựa chọn. Chỉ đứng ở ngưỡng cửa chưa tính.
- Vào phòng có Rabbit: tăng tiến độ ngay lúc vào phòng, Rabbit chạy rồi biến mất. Các phòng khác cập nhật cho bước tiếp theo ngay; không cần thoát phòng để tăng bước.
- Phòng vừa chọn giữ cửa mở và không đổi occupant khi Player còn ở bên trong. Quay ra hành lang mới rearm phòng đó; không spawn Spider trước mặt Player vừa tìm được Rabbit.
- Vào phòng có SpiderHorror: khóa input ngắn, camera hướng về Spider, Spider lao vào Player; blackout rồi reset toàn bộ về cửa vào và tiến độ 0.
- Phải tìm đúng Rabbit bảy lần trong sáu phòng bên. Sau 7/7, cửa Exit mở và Rabbit xuất hiện trong phòng Exit. Thỏ giữ hướng cố định của CC_ExitRabbitSpawn, quay ra hành lang và đứng yên khi Player đến; Completed phát đúng một lần. Thỏ tại Exit không tính là lượt tìm thứ tám.
- Cửa hốc trú mở sẵn lúc bắt đầu/reset, đóng sau khi Player đi vào và có thể đẩy mở để thoát ra.
- Bảng hồ sơ cho tiền tố (K7, A4, R3 hoặc B6) và ba ký hiệu ứng với ba đồng hồ theo thứ tự từ đầu đến cuối hành lang. Dấu ● chọn kim giờ, dấu ○ chọn kim phút; hai đầu kim có dấu cùng hình dạng màu đen. Đọc số La Mã mà kim được chọn chỉ tới để ghép mảnh B, rồi tìm biển cửa phù hợp; không vào phòng để xem trước đáp án. Kim phút đặt đúng vạch 5 phút và đọc giá trị số La Mã, ví dụ kim phút chỉ III cho chữ số 3, không phải 15.
- Lượt đầu: cả ba đồng hồ hoạt động, lần lượt chỉ I → III → IV, suy ra K7-134. Sáu lựa chọn là K7-134, K7-143, K7-314, K7-341, K7-413, K7-431. Các lượt sau giữ nguyên: 218 → 142 → 421 → 315 → 531 → 153. Cả bảy lượt đều đọc đủ ba đồng hồ, không có đồng hồ OFF trong lúc giải mã. Đồng hồ đổi ngay khi tìm đúng Rabbit; chết/reset đưa chúng về 134. Sau 7/7, tất cả tắt và bảng ghi ARCHIVE END.
- Không hiển thị nhãn START/MIDDLE/END, REFUGE/WAIT HERE hoặc hướng dẫn đọc đồng hồ. Bảng giữ tên thử thách, tiến độ, tiền tố mã và một hàng ba ký hiệu. Các hàng lần lượt là ●○●, ○●○, ●●○, ○○●, ●○○, ○●●, ●●●. Chúng đổi ngay khi tìm đúng Rabbit; reset trở về ●○● và hoàn tất 7/7 thì ẩn ký hiệu.
- Kim không được chọn biểu diễn chữ số từ một mã cửa sai trong sáu lựa chọn. Mã nhiễu được chọn sao cho cả ba chữ số đều khác đáp án ở cùng vị trí; hai kim trên mỗi đồng hồ luôn chỉ hai số khác nhau.
- Demo giữ thứ tự mã đúng cố định để regression, chưa thêm random. Cả sáu biển ở lượt đầu có tiền tố K7; các lượt sau giữ bộ mã hiện có.

## Đồng hồ dùng chung

Ba instance dưới `CallingCorridorDemo/CC_ClockPuzzle` dùng duy nhất
`Prefabs/CallingCorridorDemo/PF_CC_PuzzleClock.prefab`.
`CallingCorridorClockPuzzle` nhận Session và mảng Clocks theo thứ tự Start, Middle, End.
Mỗi `CallingCorridorClock` cần Hour Pivot, Minute Pivot, Dial và Offline Cover. Kim giờ ngắn hơn, có đĩa tròn đen đặc; kim phút dài hơn, có vành tròn đen rỗng thật, đặt riêng để thân kim không lấp lỗ. Cả hai quay theo nấc 30 độ tương ứng vị trí số La Mã, đứng yên trong mỗi lượt.
`Tools > Zero > Calling Corridor > Upgrade Shared Clock Hands` cập nhật duy nhất prefab đồng hồ và hai mesh dấu tròn dùng chung khi được chủ động chạy; không ghi đè scene. `CallingCorridorRules.ClockHand`, `ClockHandDigit` và `ClockSymbols` là nguồn dữ liệu cho hai kim và hàng ký hiệu. `ClockDigit` vẫn trả chữ số đáp án để giữ các truy vấn cũ.
Chỉnh vị trí từng instance trong scene; chỉnh hình dáng trong prefab chung. Không Apply reference Session hoặc nhãn thứ tự riêng lên prefab.
- Quái vật hành lang bắt đầu ngay khi tìm đúng Rabbit lần thứ ba, tức lúc chuyển sang lượt 4, dù Player còn trong phòng. Mỗi lần chuyển sang lượt 5, 6, 7 sẽ reset và bắt đầu lại encounter ngay; ba lượt đầu vẫn an toàn. Cảnh báo ánh sáng 1.8 giây diễn ra đồng thời với di chuyển và animation, không còn làm quái đứng chờ. Không tạo thêm instance quái vật.
- `CallingCorridorThreat.Minimum Active Seconds` mặc định 20 giây. Trú trong hốc ít nhất 0.6 giây khiến quái đi qua; nếu tới đầu tuyến quá sớm, quái tiếp tục đi qua lại giữa spawn và exit cho đến hết thời gian tối thiểu. Ra khỏi hốc sẽ bị truy đuổi lại. Khi sang lượt mới, bị bắt, chọn Spider, reset/disable hoặc completion, encounter cũ được cleanup. Thời gian tối thiểu không trì hoãn những thao tác cleanup này. Hốc trú chỉ chống quái hành lang, không bảo vệ khỏi Spider trong phòng sai.

## Một prefab phòng duy nhất

Tất cả sáu phòng L1/R1/L3/R3/L5/R5 dưới `CallingCorridorDemo/CC_RabbitRooms` dùng
`Assets/Game/Prefabs/CallingCorridorDemo/PF_CC_StorageRoom_L1.prefab`.
Không tạo prefab hoặc variant theo từng cửa.

Nhóm con giữ nguyên Architecture, Dressing, Lighting, GameplayMarkers.
Chỉ chỉnh prefab chung khi muốn thay trang trí tất cả phòng; không Apply mã/pose riêng của từng cửa lên prefab.
Các CC_Pocket* cũ vẫn disabled để không chắn phòng kho.

Component `CallingCorridorRoomEncounter` trên prefab phòng:

- Rabbit Prefab: `Prefabs/CallingCorridorDemo/Character/Rabbit.prefab`.
- Spider Prefab: `Prefabs/CallingCorridorDemo/Character/SpiderHorror.prefab`.
- Rabbit Spawn / Spider Spawn / Rabbit Escape: marker trong GameplayMarkers.
- BoxCollider Is Trigger, phủ vùng vào phòng sau góc rẽ; không phủ hành lang chung hoặc cửa.
- Actor được tạo một lần khi session Prepare, dùng lại giữa các lượt; model collider disabled để không chắn Player. Reset/disable dừng animation và coroutine, ẩn actor.

`CC_Gameplay/CC_SharedDoors/CallingCorridorGate` có hai mảng cùng thứ tự:
Doors và Rooms = L1, R1, L3, R3, L5, R5. Reference không được thiếu hoặc trùng.
Exit Room trỏ tới `CC_ExitVestibule/CC_ExitRabbitEncounter`, có marker `CC_ExitRabbitSpawn`
và `CC_ExitRabbitEscape`. Component dùng lại animation Rabbit hiện có; reset/disable ẩn thỏ tại Exit.
Session giữ owner duy nhất của tiến độ, reset, blackout và input lock.
Gate giữ phòng đang được chọn để chống callback lặp và rearm sau khi quay ra.

## Animation

Rabbit dùng state Move trong Rabbit.controller với clip model có sẵn, không dùng Move.anim rỗng.
SpiderHorror dùng Legacy Animation Idle khi chờ và Walk khi lao; chuyển động nhảy là trajectory do encounter điều khiển.
Model hiện chưa có clip skeletal Attack/Jump chuyên dụng. Khi có clip mới, thay presentation trong encounter;
không thêm controller AI/chase hoặc package chỉ để thực hiện cú lao ngắn.

## Kiểm thử

- EditMode CallingCorridorRulesTests: đúng/sai, immediate entry, duplicate callback, reset toàn bộ.
- PlayMode CallingCorridorGameplayTests: đi bộ qua sáu cửa và góc rẽ, Rabbit chạy/ẩn, Spider lao/reset,
  cleanup khi reset/disable giữa animation, đúng bảy bước và completion một lần, missing reference, text bounds.
- Test Runner yêu cầu scene được người dùng lưu. Không tự save scene.
- Sau thay geometry, chọn CallingCorridorDemo và chủ động chạy
  Tools > Zero > Calling Corridor > Bake Selected Navigation, rồi Ctrl+S.
- Chưa đo thời lượng bằng người chơi thật, FPS máy đích hoặc multiplayer.

Không Scene Builder, RSP, editor auto-setup hoặc auto-save. Scene authoring qua UnitySkills có dry-run và Undo.
