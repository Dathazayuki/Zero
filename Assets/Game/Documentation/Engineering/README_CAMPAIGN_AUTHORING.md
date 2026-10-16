# README — Thiết lập Infinity Room, Minigame và Anomaly trong Unity

Hướng dẫn authoring nội dung cho campaign Zero hiện tại. Đọc theo thứ tự khi thêm
một chặng mới; không cần chạy Scene Builder hoặc tạo thêm hệ thống quản lý tiến trình.

> `Content Ready` chỉ cho phép runtime thử sử dụng nội dung. Nó không tự tạo geometry,
> anomaly, NavMesh, marker, trigger hoặc thang máy còn thiếu. Chỉ bật sau khi cấu hình
> đã được kiểm tra. Framework có sẵn không đồng nghĩa cả năm chặng đã chơi hoàn chỉnh.

## Mục lục

- [1. Sơ đồ kết nối và nơi tìm cấu hình](#1-sơ-đồ-kết-nối-và-nơi-tìm-cấu-hình)
- [2. Chuẩn bị campaign một lần](#2-chuẩn-bị-campaign-một-lần)
- [3. Tạo Infinity Room mới](#3-tạo-infinity-room-mới)
- [4. Thêm anomaly vào một Room](#4-thêm-anomaly-vào-một-room)
- [5. Tạo minigame mới](#5-tạo-minigame-mới)
- [6. Cấu hình thang máy và chuyển chặng](#6-cấu-hình-thang-máy-và-chuyển-chặng)
- [7. Checkpoint, death và Continue](#7-checkpoint-death-và-continue)
- [8. Ví dụ nối hoàn chỉnh một chặng](#8-ví-dụ-nối-hoàn-chỉnh-một-chặng)
- [9. Checklist nghiệm thu](#9-checklist-nghiệm-thu)
- [10. Xử lý lỗi thường gặp](#10-xử-lý-lỗi-thường-gặp)

## 1. Sơ đồ kết nối và nơi tìm cấu hình

Project tier: small-game phát triển dài hạn. Phân biệt ba lớp: dữ liệu cấu hình,
scene/prefab thực thi gameplay và runtime sở hữu tiến trình.

```text
TitleScene / GameFlow
└── CampaignRuntime.definition → Campaign_Main.asset
    └── Stages[n]
        ├── Infinity Room → InfinityRoomDefinition
        │   ├── Segment Prefab → FloorSegment + RoomAnomalyController
        │   │   └── Anomaly Slots → component AnomalyBehaviour → Data
        │   └── Anomaly Roster → các asset AnomalyData được phép chọn
        └── Minigame → MinigameDefinition.SceneName
            └── Scene đích / CampaignLevelBootstrap
                ├── Minigame Session → MonoBehaviour triển khai IMinigameSession
                ├── Entrance Checkpoint → marker an toàn
                └── Exit Elevator → EndFloorElevatorController
```

| Cần thay đổi | Nơi thực hiện trong Unity |
|---|---|
| Thứ tự chặng, cặp Room/minigame | `Assets/Game/ScriptableObjects/Campaign/Campaign_Main.asset → Stages` |
| Prefab và danh sách anomaly của Room | `InfinityRoom1…5.asset` trong cùng thư mục |
| Scene đích của minigame | `Minigame1…5.asset` trong cùng thư mục |
| Model, collider và mechanic anomaly | Prefab tầng/phòng, component trong `Anomaly Slots` |
| Điểm căn cabin, checkpoint và session | `CampaignLevelBootstrap` của scene đích |
| Luật thắng/thua của minigame | Controller gameplay và adapter `IMinigameSession` |

Runtime hiện yêu cầu **đúng năm chặng**. Dùng các definition placeholder đã có để
hoàn thiện Room2–5/Minigame3–5; không tăng `Stages` lên sáu nếu chưa thay đổi domain.
ID campaign, stage, Room, minigame và anomaly phải ổn định; không đổi ID đã có save
nếu chưa có migration. Tên hiển thị/chủ đề không thay thế ID.

## 2. Chuẩn bị campaign một lần

1. Nếu chưa có asset, chạy `Tools > Zero > Campaign > Create Definitions`.
   Menu giữ các asset đã tồn tại; không dùng để reset cấu hình đang làm.
2. Mở `TitleScene`, chọn object chứa `GameFlowController`.
3. Bảo đảm **cùng object** có `CampaignRuntime`, trường `Definition` trỏ tới
   `Campaign_Main.asset`. Có thể dùng `Tools > Zero > Campaign > Bind Active Scene Flow`.
4. Kiểm tra scene name trong `GameFlowController`, đặc biệt `Loop Scene Name` trỏ tới
   `InfiniteStairwayScene`. Không gán một campaign khác cho GameFlow bản sao trong scene khác.
5. Trong Build Settings, kiểm tra Title là scene khởi đầu; Office, loop và các scene
   minigame đã authoring đều nằm trong danh sách được bật.
6. Lưu scene bằng `Ctrl+S` do bạn chủ động thực hiện.

Test tích hợp từ Title/New Game. Chạy riêng một minigame scene không bảo đảm có
persistent flow, campaign phase và Player thực tế như lúc chuyển từ thang máy.

## 3. Tạo Infinity Room mới

### 3.1. Author prefab tầng

Tạo prefab riêng, ví dụ `PF_InfinityRoom2`. Có thể duplicate prefab tầng đang hoạt động
để giữ contract, rồi sửa nội dung trên bản mới; không sửa Room1 để giả thành Room2.

Hierarchy dưới đây là gợi ý tổ chức, không phải toàn bộ tên bắt buộc:

```text
PF_InfinityRoom2 [FloorSegment]
├── Geometry
├── Anchors
│   ├── UpAnchor
│   └── DownAnchor
├── TriggerRoot
│   └── FloorCommitTrigger [BoxCollider, SegmentCommitTrigger]
├── Room [RoomAnomalyController, CorrectArrowController]
│   └── AnomalyContent
├── Floor_Number
│   ├── Number0
│   └── … Number8
└── LoopResumePoint [LoopResumePoint]
```

- Gán reference geometry, Room, stairs và anchors trong `FloorSegment` theo prefab hiện có.
- `UpAnchor/DownAnchor` phải nối đúng vị trí và rotation với tầng bên cạnh; kiểm tra cả
  đi lên và xuống, không chỉ kích thước căn phòng.
- Mỗi physical segment thường có đúng **một** `SegmentCommitTrigger`; collider là trigger.
  Có thể để manager/owner trống vì loop bind lúc initialization.
- Giữ tên `Floor_Number`; `Number0…Number8` là con trực tiếp của nó. Runtime tìm và bật
  số bằng contract này. Tắt sẵn mọi Number trong prefab.
- Gán `Room Root`, `Correct Arrow Controller` và `Anomaly Slots` trên `RoomAnomalyController`.
- Thêm `LoopResumePoint` trong prefab, đặt ở sàn an toàn, nhìn đúng hướng, không overlap
  commit/kill/entry trigger. `Spawn Transform` để trống dùng chính marker hoặc gán marker khác.
- Giữ collider sàn/tường, Player đi được qua cửa và cầu thang. Khi cần navigation,
  cấu hình NavMesh riêng cho geometry mới, không coi bake của Room1 là hợp lệ cho Room2.

### 3.2. Gán Room definition

Chọn `InfinityRoom2.asset`, hoặc tạo qua `Create > Zero > Campaign > Infinity Room`:

| Field | Cách gán |
|---|---|
| `Room Id` | ID riêng, ổn định; giữ ID của placeholder khi đang hoàn thiện nó |
| `Design Theme` | Mô tả chủ đề cho designer |
| `Segment Prefab` | Prefab asset `PF_InfinityRoom2`, không phải instance trong scene |
| `Anomaly Roster` | Các asset Data có behaviour tương ứng trong prefab |
| `Maximum Difficulty` | Giới hạn 1–3 cho Room |
| `Use Authored Scene Pool` | **OFF** với Room mới; ON chỉ khi cố ý dùng pool đã đặt trong scene |
| `Content Ready` | Bật sau khi prefab/roster/reference đã được kiểm tra |

Room1 hiện dùng authored scene pool. Room mới dùng prefab definition để runtime tạo
pool năm segment, trong đó current và một neighbor mỗi phía active.

### 3.3. Chuẩn bị scene loop dùng chung

Runtime hiện tải `GameFlowController.LoopSceneName` cho các Infinity Room kế tiếp.
`InfinityRoomDefinition` không có field scene riêng; sự khác nhau giữa các Room nằm ở
prefab/roster được inject trước initialization.

Trong `InfiniteStairwayScene`, cần **đúng một** `CampaignLevelBootstrap` cho lượt trở về:

| Field | Reference |
|---|---|
| `Environment Root` | Parent của geometry, loop managers và anchors; **không chứa Player** |
| `Building` | `InfiniteBuildingManager` của scene |
| `Arrival Anchor` | Marker con của Environment Root, tại ngưỡng cabin đến |
| `Entrance Checkpoint` | Marker an toàn ở đầu Room để bind context respawn |
| `Destination Facade` | Facade che mép cabin nếu có; tùy chọn |
| `Minigame Session` | Để trống |
| `Exit Elevator` | Không phải đường thoát Room thường; Room dùng tuyến End floor hiện có |

Arrival anchor forward hướng **vào cabin**, ngược với forward của cabin anchor.
Giữ scale các root/anchor thống nhất, khuyến nghị `(1,1,1)`; không để lệch cao độ sàn.
Loop manager và parent chứa pool runtime phải thuộc Environment Root để cùng được căn chỉnh.
Đặt bootstrap trên object active ngoài root geometry có thể bị ẩn.

`Entrance Checkpoint` không thay thế `LoopResumePoint`: Continue Stairway hiện tìm
`LoopResumePoint` trong current segment. Với các prefab khác nhau, kiểm tra cả hai marker
đều an toàn và không còn dùng vị trí spawn chỉ phù hợp Room1.

### 3.4. Đăng ký vào campaign và đường thoát Room

Trong `Campaign_Main → Stages → stage-2`, gán definition vừa hoàn thiện vào `Infinity Room`.
Giữ `Minigame` trỏ tới definition của minigame sau Room2.

Kiểm tra `StairLoopManager → Ending Segment Prefab` vẫn trỏ tới End floor hợp lệ,
có entry/sequence/elevator đã cấu hình. Đạt Number8 chưa tự thắng: cần quyết định đúng
cuối để vào tuyến End floor, rồi chọn elevator đi tới minigame của chặng hiện tại.

## 4. Thêm anomaly vào một Room

### 4.1. Tạo dữ liệu

Tạo asset bằng `Create > Zero > Anomaly Prototype > Anomaly Data`, đặt trong thư mục
`Assets/Game/ScriptableObjects/Anomalies` hoặc thư mục con theo Room.

| Field | Ý nghĩa |
|---|---|
| `Id` | ID anomaly duy nhất; không dùng tên GameObject làm ID runtime |
| `Display Name` | Tên dễ đọc cho authoring/debug |
| `Type` | Loại nội dung; không phải pacing tier |
| `Pacing Tier` | `UneasyNormal`, `MinorAnomaly` hoặc `MajorAnomaly` |
| `Difficulty` | 1–3; không vượt giới hạn Room nếu muốn nó có thể được chọn |
| `Spawn Weight` | Trọng số tương đối, không phải phần trăm xuất hiện trực tiếp |
| `Can Kill Player` | Quyền gây death, không tự tạo mechanic/collider gây chết |
| `Death Style` | Presentation death theo hệ thống hiện có |

`UneasyNormal` **vẫn tính là anomaly gameplay**, không phải pure Normal.
Pacing còn giới hạn tier theo tiến độ, hồi phục sau Major và chống lặp ID gần nhất;
do đó gán roster không có nghĩa mọi anomaly xuất hiện ngay tại tầng0.

### 4.2. Thêm implementation và nối hai danh sách

1. Mở prefab Room/tầng trong Prefab Mode.
2. Thêm behaviour có sẵn hoặc component mới kế thừa `AnomalyBehaviour` dưới AnomalyContent.
3. Gán asset vừa tạo vào trường `Data` của component.
4. Gán đủ reference model, marker, renderer, audio, animator và trigger cần thiết.
5. Thêm **component behaviour** vào `RoomAnomalyController → Anomaly Slots`.
6. Thêm **cùng asset AnomalyData** vào `InfinityRoomDefinition → Anomaly Roster`.
7. Lưu prefab thủ công và kiểm tra scene instance không giữ override dữ liệu cũ ngoài ý muốn.

Không để slot null, Data thiếu hoặc ID trùng. Roster không tự sinh behaviour trong prefab;
behaviour ngoài roster không được campaign chọn. Room1 có menu
`Tools > Zero > Campaign > Sync Room1 Roster From Prefab`; menu này chỉ đồng bộ Room1,
không phải công cụ tự đồng bộ mọi Room.

### 4.3. Khi viết mechanic mới

- `OnActivate()` chuẩn bị/khởi động đúng trạng thái; không giả định reserve đã có Player.
- `CaptureNormalState()` lưu baseline nếu cần; không lưu state lượt chơi trong Data asset.
- `OnResetAnomaly()` trả mọi thay đổi về baseline, gọi hai lần an toàn.
- `OnDisable()` cleanup coroutine, audio, particle, agent, event và camera override đang sở hữu.
- Trigger consume/gate callback; mechanic chỉ bắt đầu khi đúng điều kiện và đúng current floor.
- Nếu lethal, kết nối death API/kill trigger hiện có; chỉ bật `Can Kill Player` là chưa đủ.

**Giới hạn binding hiện tại:** `RoomAnomalyController.InitializeRoom()` bind Player/camera
cho các concrete behaviour đã biết; `FloorSegment.ConfigureNavigation()` bind floor context
cho một số anomaly. Mechanic mới cần context không tự được inject chỉ vì kế thừa base class.
Phải thực hiện task tích hợp binding rõ ràng trước khi đánh dấu ready. Không tự tìm Player
placeholder, thêm singleton hoặc nối thêm chuỗi `if type` dài để né chuẩn kiến trúc;
dùng capability contract/composition phù hợp khi mở rộng điểm binding.

## 5. Tạo minigame mới

### 5.1. Scene contract

Ví dụ scene `Minigame3_CallingCorridor`:

```text
Minigame3_CallingCorridor
├── CampaignSystems [CampaignLevelBootstrap, adapter IMinigameSession]
└── EnvironmentRoot
    ├── ArrivalAnchor
    ├── EntranceCheckpoint
    ├── Geometry / Lighting / Audio / Navigation
    ├── GameplayController / StartTrigger / CompleteTrigger
    └── ExitElevator [EndFloorElevatorController + các trigger]
```

- Cần đúng **một** `CampaignLevelBootstrap`; thiếu hoặc trùng đều làm loader từ chối.
- Bootstrap/session phải accessible và lifecycle hoạt động khi scene được chuẩn bị.
- Scene không có Player/Main Camera/AudioListener/GameFlow active thứ hai. Nếu giữ
  placeholder để authoring, tắt nó và không gán nó làm Player gameplay.
- Geometry, navigation và gameplay objects phải đi cùng Environment Root khi căn scene.
- Marker entrance đặt trước start trigger, trên sàn thật, không nằm trong catch/kill volume.
- Prepare phải bảo đảm navigation dùng được sau alignment trước khi cho session ready.

### 5.2. Adapter bắt buộc

Minigame mới cần một `MonoBehaviour` triển khai contract ở
`Scripts/Flow/Campaign/IMinigameSession.cs`:

```csharp
bool IsReady { get; }
IEnumerator Prepare();
void Begin(MinigameContext context);
void ResetAttempt();
event Action Completed;
```

Đây là danh sách member, không phải script hoàn chỉnh để dán vào project.

| Member | Trách nhiệm |
|---|---|
| `Prepare` | Cache/bind dependency và chuẩn bị NavMesh/resources; chưa tự thắng |
| `IsReady` | Chỉ true khi gameplay và marker bắt buộc hợp lệ |
| `Begin` | Nhận `context.Player`, `context.Fade`, `context.Entrance`; chuẩn bị lượt chơi |
| `ResetAttempt` | Reset mechanic/monster/trigger, cleanup và đưa lượt chơi về trạng thái sạch |
| `Completed` | Phát đúng một lần khi Player thực sự đạt điều kiện thắng |

Campaign subscribe completion sau lúc destination được chuẩn bị: **không phát Completed
ngay trong Prepare/Begin** để giả lập nội dung hoặc bỏ qua stage.

Gameplay controller xử lý luật cục bộ; adapter chuyển event sang campaign, subscribe/unsubscribe
đối xứng. Không gọi trực tiếp tăng stage, tải Room kế tiếp hoặc ending từ mechanic minigame.
Fail/death minigame dùng reset local; không gọi reset Stairway để đưa Player về Room1.

Hai adapter tham khảo đang có:

- `BackroomMinigameSession → BackroomGameplayManager`: chase, catch/reset, escape.
- `RoomsMinigameSession → TrueEndingGameplayController`: Room1/Room2; fail reset cả cụm,
  complete không chạy Blue Sky trong minigame mode.

### 5.3. Gán bootstrap và definition

| `CampaignLevelBootstrap` field | Reference trong scene minigame |
|---|---|
| `Environment Root` | Root map và gameplay objects |
| `Arrival Anchor` | Marker tại cửa cabin, là con của root; forward hướng vào cabin |
| `Entrance Checkpoint` | Marker dùng khi Continue/retry, nhìn vào đường chơi |
| `Destination Facade` | Facade tại cabin nếu có, tùy chọn |
| `Minigame Session` | **Component adapter**, không phải script asset hoặc controller không triển khai interface |
| `Building` | Để trống |
| `Exit Elevator` | Controller cabin đi tiếp sau thắng, cần cho Minigame1–4 |

Chọn `Minigame3.asset` hoặc tạo qua `Create > Zero > Campaign > Minigame`:

1. Giữ/đặt `Minigame Id` riêng, ổn định.
2. Gán `Scene Name = Minigame3_CallingCorridor` đúng scene đã lưu, tránh scene trùng tên.
3. Thêm scene vào Build Settings và bật entry.
4. Kiểm tra session/bootstrap, lưu scene thủ công, rồi bật `Content Ready`.
5. Gán definition vào `Campaign_Main → stage-3 → Minigame`.

`Publish Saved Minigames` hiện dành cho hai draft Minigame1/2 đã biết; không tự publish
scene mới bất kỳ. Với Minigame3–5, gán definition và Build Settings thủ công.

### Minigame 4 — The Watchful Hall

Minigame 4 thay chủ đề `The Long Hall` bằng màn hide-and-dash trong khu service/industrial
rộng, có một quái vật khổng lồ quan sát nhiều khu vực từ vị trí cao. Có thể đặt scene
`Minigame4_WatchfulHall`. Player di chuyển giữa các điểm nấp để kích hoạt bốn ô nạp
năng lượng trên cùng map, mở khóa cửa đích rồi đi qua cửa.

- Cửa đích ban đầu khóa, hiển thị bốn biểu tượng khóa đỏ, mỗi biểu tượng gắn với một ô.
- Bốn ô ban đầu màu đỏ. Đứng trên mỗi ô 15 giây để nạp đầy thanh năng lượng.
  Nạp đầy thì ô và biểu tượng tương ứng trên cửa chuyển xanh; ô đã hoàn tất giữ trạng thái
  trong lượt chơi đó. Có thể kích hoạt bốn ô theo thứ tự bất kỳ.
- Đủ bốn ô xanh mới mở khóa cửa. Chỉ đi qua cửa đã mở mới phát `Completed` đúng một lần.
  Chạm cửa khi thiếu ô hoặc chỉ nạp xong ô cuối không hoàn thành minigame.
- Bị bắt xóa cả tiến độ nạp dở và ô đã kích hoạt, khóa lại cửa, trả bốn biểu tượng về đỏ.
  Rời ô hoặc di chuyển trên ô chỉ dừng nạp, giữ toàn bộ tiến trình dở. Phải đứng yên
  1,5 giây trước mỗi lần bắt đầu/nạp tiếp; thời gian này không tính vào 15 giây nạp.
  Xoay camera nhìn quanh không làm gián đoạn.

- Một Watcher trên cao xoay vùng nhìn và spotlight; thấy liên tục 1,5 giây thì báo vị trí
  cho Patrol gần nhất theo đường đi NavMesh. Khi mất LOS, ngừng cập nhật vị trí.
- Hai Patrol chạy `Patrolling → Chasing → Investigating → Searching → Patrolling`.
  Thấy Player thì đuổi; mất dấu thì tới vị trí cuối, tìm quanh tám giây rồi về tuyến.
  Collider/cover đặc chặn LOS; không teleport quái vật vào người chơi.
- 39 tủ che Player từ xa, nhưng Patrol trong 3 m phía cửa vào có thể kiểm tra và bắt
  trong 1,4 m tại cửa. Tường chặn LOS tới cửa vẫn có tác dụng. Alcove dựa vào geometry
  che khuất, không dùng cờ an toàn để miễn nhiễm khi quái đã áp sát.
- Mở đầu bằng một chu kỳ quan sát an toàn để dạy luật. Sau đó tăng độ khó bằng khoảng cách
  và lựa chọn cover, không rút look-away window đến mức không thể chạy tới nơi nấp kế tiếp.
- Bị bắt reset attempt tại `EntranceCheckpoint`, watcher và bốn ô/cửa đích.
  Thanh nạp 15 giây là thời gian kích hoạt từng ô; không cần đồng hồ đếm ngược toàn màn.

Greybox bốn vị trí nạp, đường nối giữa chúng, cửa đích và các cover zones. Phân bố ô ở
các vùng khác nhau của sảnh, có đường tới điểm nấp; mỗi ô cần cửa sổ nạp hữu ích sau
1,5 giây ổn định, bằng cover hoặc look-away window phù hợp, tính cả thời gian tới/rời ô.
Adapter minigame sở hữu reset attempt và điều phối
AI; không nhét state machine này vào `CampaignRuntime`. Kiểm thử cover thật sự chặn LOS,
Player thoát chase bằng cover, nạp đủ 15 giây, chỉ đổi đúng một biểu tượng tương ứng,
cửa khóa khi còn thiếu ô, reset cả bốn ô khi chết, callback trùng và completion khi AI
đang ở từng state; cleanup khi session reset/disable.

Demo đã gán bốn ô, thanh nạp và khóa cửa qua `WatchfulHallSession` trên `WH_Objectives`.
Chỉnh `Charge Seconds` (15), `Settle Seconds` (1,5) tại component này. `ResetAttempt()`
xóa toàn bộ tiến trình và đóng cửa. AI/capture đã nối qua `WatchfulHallThreatDirector`:
một Watcher báo cho Patrol gần nhất, hai Patrol tuần tra/chase/search; tủ chỉ an toàn từ xa.
Chưa có networking, chưa bật `Content Ready`.
Xem setup chi tiết trong `WATCHFUL_HALL_DEMO.md`.

### Minigame 5 — The Flooded Cistern

Minigame 5 là chase trong một bể chứa ngầm khổng lồ, không còn dùng chủ đề `The Final
Rehearsal` hay cống ngầm. Không gian có nước đọng, các hàng cột bê tông lớn đỡ vòm cong
lặp sâu vào bóng tối. Có thể đặt scene `Minigame5_FloodedCistern`. Đường thoát là cầu thang
sáng ở trung tâm, dễ nhận ra từ các nút giao chính.

- Chỉ mannequin có activation zone trên tuyến chơi mới là threat; mannequin trang trí khác
  phải inert. Mỗi mannequin được kích hoạt riêng khi Player vào vùng của nó.
- Dùng nhịp cột và các khoang vòm để tạo đoạn khuất tầm nhìn xen kẽ đường băng qua lộ thiên;
  phản chiếu mặt nước tăng chiều sâu nhưng không được che mép sàn, activation zone hoặc lối
  đi an toàn. Nước không tự làm chậm Player trong bản đầu.
- Sau một visual tell ngắn, mannequin đuổi Player bằng navigation đã chuẩn bị trong
  `Prepare()`. Nó chỉ đứng yên khi nằm trong camera view, có line of sight rõ tới Player;
  khuất khỏi tầm nhìn thì tiếp tục di chuyển. Một mannequin đang bị nhìn không đóng băng
  các mannequin threat khác.
- Session giữ quyền điều phối bắt Player: catch reset Player về `EntranceCheckpoint`, đặt
  lại mọi threat và activation zone, rồi cho phép thử lại. Chạm exit staircase phát
  `Completed` đúng một lần và dừng toàn bộ chase.
- Dạy luật bằng một mannequin đơn lẻ ở tầm nhìn thoáng trước; chỉ sau đó mới đặt các vùng
  kích hoạt quanh góc cua và phối hợp hai threat. Đừng dùng nước/ánh sáng để che khuất
  collider, lối đi hoặc hướng về cầu thang. Âm thanh chỉ là ambience/pursuit feedback,
  không phải điều kiện để hiểu luật.

Đây mới là design contract, chưa có scene/controller sẵn. Khi triển khai, tạo adapter
`IMinigameSession` riêng; không gắn logic mannequin vào `CampaignRuntime` hoặc dùng adapter
Backroom nếu hành vi reset/navigation khác. Kiểm thử tầm nhìn qua góc, nhiều threat cùng
active, catch/reset, exit khi đang chase và cleanup sau `ResetAttempt()`.

## 6. Cấu hình thang máy và chuyển chặng

Trên cabin, dùng `EndFloorElevatorController` và cấu hình:

- `Left Door` / `Right Door`: Transform cửa active, scale X đóng khác 0; runtime cache scale gốc.
- `Doorway Blocker`: collider vật lý chặn cửa khi đóng; không phải trigger.
- `Cabin Backroom Anchor`: marker ở ngưỡng cửa, forward từ cabin ra map đích.
- `Departure Only Objects`: phần nguồn cần ẩn; không đưa Player/cabin hoặc hệ thống sống
  xuyên cảnh vào danh sách này.
- Audio motor/door/arrival, cabin light và floor indicator nếu dùng; cấu hình theo audio system.
- Door duration khoảng 0.8 giây; minimum ride ít nhất 5 giây. Giá trị serialized có thể dài hơn.

Thêm `ElevatorEndingTrigger`, gán `Elevator` về cabin và `Kind` tương ứng:

| Kind | Vị trí và tác dụng |
|---|---|
| `Approach` | Trước cửa; mở cabin khi Available |
| `Entry` | Đủ sâu trong cabin để Player đã vào hẳn; đóng cửa và bắt đầu load |
| `Exit` | Ở lối ra cabin; xác nhận Player ra đích và commit checkpoint handoff |
| `Unload` | Callback tương thích luồng cũ; không dùng nó thay cho readiness trong campaign |

Trigger dùng `BoxCollider Is Trigger`; kiểm tra layer collision với Player, không để
approach/entry/exit volume chồng lên nhau gây consume sai thời điểm.

Minigame1–4: gán cabin vào `CampaignLevelBootstrap.Exit Elevator`. Khi session Completed,
campaign gọi MakeAvailable. Chặng kế tiếp chưa ready thì không cho bắt đầu ride.
Không tự MakeAvailable trong Start của cabin để mở đường trước khi thắng.

Luồng campaign hiện load additive, căn root, Prepare và nhận destination trước khi mở cửa.
Cabin/Player được chuyển sang scene đích; source scene được unload trong luồng này khi cửa
còn đóng, **không chờ riêng Unload trigger kiểu legacy**. Thiết kế cabin kín và Departure Only
Objects đúng để không lộ map nguồn hoặc map đích đang căn chỉnh.

Minigame5: Completed mở quyền chọn ending, không tự mở thang máy Room6.
Cần authoring khu lựa chọn ending và `CampaignEndingChoiceTrigger` với presentation
tương ứng; framework không tạo geometry hub hoặc tự gọi ending thay nội dung còn thiếu.

## 7. Checkpoint, death và Continue

- Infinity Room Continue: khôi phục đúng stage nhưng bắt đầu floor0, pure Normal;
  dùng `LoopResumePoint` trong prefab current segment, random lại Arrow/anomaly.
- Minigame Continue: tải scene theo definition và đặt Player tại `Entrance Checkpoint`;
  dùng Player thật, không giả lập elevator ride.
- Lịch sử anomaly encounter giữ qua sai/death/Continue trong Room hiện tại; Room mới có
  history mới. Reserve roll không được ghi là Player đã gặp.
- Chuyển đích thất bại không tăng stage hoặc ghi đè checkpoint cũ.
- Không thêm lệnh save tầng/pose tùy ý vào anomaly hoặc minigame. Save do flow/checkpoint owner quản lý.
- Không đổi ID hoặc sửa JSON bằng tay để vượt các placeholder chưa ready.

## 8. Ví dụ nối hoàn chỉnh một chặng

Muốn hoàn thiện Room2 và minigame sau nó:

1. Author `PF_InfinityRoom2`, thêm resume marker và behaviour anomaly.
2. Trong `InfinityRoom2.asset`, gán prefab/roster, tắt authored pool, kiểm tra rồi bật ready.
3. Hoàn thiện bootstrap/arrival geometry trong loop scene dùng chung.
4. Review `Minigame2_Rooms`, gán session/entrance và author Exit Elevator của nó.
5. Trong `Minigame2.asset`, gán scene name, đăng ký Build Settings và kiểm tra ready.
6. Trong `Campaign_Main.stage-2`, gán đúng cặp Room2/Minigame2.
7. Kiểm tra Minigame1 có exit elevator để tới Room2. Kiểm tra Room3 definition ready
   nếu muốn rời Minigame2 tới Room3; nếu chưa có Room3, bị chặn là đúng thiết kế.
8. Save scene/prefab thủ công; chơi từ Title để kiểm tra toàn tuyến.

Không đánh dấu các Room khác ready để làm cho validation nhìn đẹp; nội dung chưa có phải
ở trạng thái unready và chặn progression an toàn.

## 9. Checklist nghiệm thu

### Cấu hình và prefab

- [ ] Campaign_Main có đúng 5 stage, ID riêng và reference definition hợp lệ.
- [ ] Title GameFlow có CampaignRuntime gán đúng definition.
- [ ] Prefab Room mới có anchors, một commit trigger, Number lifecycle và resume marker.
- [ ] Mỗi roster Data có behaviour/slot tương ứng; không có null hoặc ID trùng.
- [ ] Scene đích có đúng một bootstrap, session đúng interface và marker an toàn.
- [ ] Scene name đúng và có trong Build Settings; không có Player/camera active thứ hai.
- [ ] Exit elevator được gán; tầng8 có End route/cabin hoạt động.
- [ ] Scene/prefab được lưu thủ công; không chạy Scene Builder/Apply All.

### Play Mode

- [ ] Initial/Continue Room luôn bắt đầu floor0 Normal, đúng prefab và đúng spawn.
- [ ] Đi lên/xuống không hở sàn, lệch anchor hoặc duplicate commit.
- [ ] Anomaly chỉ kích hoạt theo luật; recycle/death không còn audio/coroutine/agent cũ.
- [ ] Đúng/sai ở số8 vẫn đúng luật; reaching8 không tự ending.
- [ ] Cabin chỉ mở đích sau load/alignment/readiness, Player không bị teleport giữa ride.
- [ ] Thắng minigame chỉ phát Completed một lần, chỉ mở đường đi tiếp một lần.
- [ ] Retry và Continue đặt đúng entrance, trigger rearm, không nhầm Player placeholder.
- [ ] Destination chưa ready/load thất bại không nhốt Player hoặc mất checkpoint cũ.
- [ ] Ba ending bị chặn trước khi Minigame5 hoàn thành.

Chạy `Tools > Zero > Campaign > Validate Definitions`. Validator kiểm tra cấu trúc ID/roster
và báo readiness; **không bảo đảm collider, geometry, navigation hoặc mọi reference scene đúng**.
Khi có thay đổi C#, chạy Code Policy và EditMode/PlayMode test liên quan. Không nâng baseline
để che regression. Checklist này là việc cần chạy, không phải khẳng định mọi scene đã pass.

## 10. Xử lý lỗi thường gặp

| Hiện tượng/log | Kiểm tra trước |
|---|---|
| Campaign không hoạt động | CampaignRuntime cùng object GameFlow, Definition và5 stage hợp lệ; vào từ Title |
| `CAMPAIGN_CONTENT_NOT_READY` | Definition đích ready, scene có trong Build Settings, đúng phase/chặng |
| `CAMPAIGN_ROOM_CONFIGURATION_INVALID` | Room prefab/roster, building/loop reference, bootstrap trước init, Player thật |
| `CAMPAIGN_LOAD_FAILED` | Scene name, đúng một bootstrap, arrival anchor là con root, session Prepare/IsReady |
| `CAMPAIGN_EXIT_ELEVATOR_MISSING` | Gán Exit Elevator trên bootstrap của minigame vừa thắng |
| Elevator không mở/không ride | Completed chưa phát, state chưa Available, door scale/collider/anchor, destination chưa ready |
| Map đến lệch/xuyên sàn | Hai anchor cùng ngưỡng và cao độ, forward đối nhau, root/parent pool đúng, collider không overlap |
| Anomaly mới không được chọn | Data trên component, Anomaly Slots, roster cùng asset, ID, difficulty/tier/cooldown/history |
| Anomaly có trong roster nhưng không chạy | Thiếu behaviour/reference hoặc chưa tích hợp Player/floor context binding |
| Continue Room spawn sai | LoopResumePoint thiếu/sai trong prefab; runtime có thể fallback LoopEntrySpawnPoint |
| Continue minigame spawn sai | Entrance Checkpoint scene sai vị trí/rotation hoặc nằm trong start/catch volume |
| Monster không đi sau alignment | NavMesh preparation, agent cùng root, surface/agent type và vị trí agent trên mesh |
| Minigame thắng nhưng không sang Room | Completed gate, exit elevator, Room definition kế tiếp ready; không tự load từ gameplay |

## Tài liệu và source liên quan

- [Campaign framework và trạng thái bàn giao](CAMPAIGN_FRAMEWORK_UNITY_SETUP.md)
- [Title/checkpoint setup](TITLE_CHECKPOINT_UNITY_SETUP.md)
- [Chuẩn code](CODING_STANDARDS.md) và [dependency/ownership](ARCHITECTURE.md)
- `Scripts/Flow/Campaign/`: definitions, domain progress, bootstraps, session contract và loader.
- `Scripts/EnvironmentLoop/RoomAnomalyController.cs`: anomaly slots và Player binding hiện tại.
- `Scripts/Anomalies/AnomalyBehaviour.cs`: lifecycle extension cho anomaly.
- `Scripts/EnvironmentLoop/EndFloorElevatorController.cs`: cabin/door/ride presentation.

Tài liệu này mô tả contract mã nguồn hiện tại. Room/minigame mới chỉ được xem là chạy được
sau khi wiring, authoring và test tuyến chuyển cảnh thực tế hoàn tất; không phải chỉ vì đã
tạo definition hoặc bật Content Ready.
