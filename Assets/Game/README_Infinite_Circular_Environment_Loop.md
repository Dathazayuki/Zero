# [ARCHIVED] Kế hoạch xây dựng Environment Loop vô hạn — Circular Stairwell Horror Game

> Đây là kế hoạch environment cũ và có thể chứa `SegmentRecycleTrigger`/five-segment swap đã bị loại bỏ. Kiến trúc runtime hiện tại được mô tả trong `Game/README.md` và `Game/Documentation/ROOM_ANOMALY_DIRECTION_DESIGN.md`.

## 1. Mục tiêu

Tài liệu này mô tả kế hoạch xây dựng **môi trường loop vô hạn liền mạch** cho game kinh dị 3D góc nhìn thứ nhất trong Unity.

Phạm vi hiện tại chỉ tập trung vào **environment**:

- phòng / khu vực quan sát;
- landing giữa các tầng;
- thang bộ;
- lõi thang máy hỏng ở chính giữa;
- cơ chế di chuyển lên / xuống;
- tái sử dụng các tầng để tạo cảm giác công trình kéo dài vô hạn;
- không fade;
- không reload scene;
- không teleport Player;
- không để Player nhìn thấy segment bị recycle.

**Chưa triển khai anomaly trong giai đoạn này.**

Tuy nhiên kiến trúc môi trường cần được xây sao cho sau này cửa thang máy, props, ánh sáng và các thành phần khác có thể trở thành anomaly mà không phải viết lại hệ thống loop.

---

# 2. Ý tưởng không gian

Map có cấu trúc dạng vòng quanh một lõi trung tâm.

Lõi trung tâm là một **thang máy bị hỏng và không thể sử dụng**.

Player di chuyển quanh lõi này thông qua:

```text
Room / Observation Area
        ↓
Stair Flight
        ↓
Middle Landing
        ↓
Stair Flight
        ↓
Next Floor / Room
```

Nhìn từ trên xuống, Player liên tục di chuyển quanh lõi trung tâm:

```text
          ┌───────────────┐
          │               │
          │    ROOM       │
          │               │
    ┌─────┘               │
    │                     │
    │   ┌─────────────┐   │
    │   │             │   │
    │   │  ELEVATOR   │   │
    │   │    CORE     │   │
    │   │             │   │
    │   └─────────────┘   │
    │                     │
    │      LANDING        │
    └─────────────────────┘
```

Lõi thang máy và các landing giúp **che line-of-sight giữa các tầng**.

Đây là yếu tố quan trọng nhất cho hệ thống loop.

---

# 3. Nguyên tắc cốt lõi

## 3.1 Player không loop

Không bao giờ thực hiện:

```csharp
player.transform.position = startPosition;
```

Player phải thực sự tiếp tục di chuyển trong world space.

Các hệ thống sau phải giữ nguyên liên tục:

- Player position;
- CharacterController;
- camera rotation;
- velocity;
- gravity;
- mouse look;
- audio listener.

Không có camera snap hoặc black frame.

---

## 3.2 World loop quanh Player

Thay vì đưa Player về đầu map:

```text
PLAYER
  ↓
teleport về start
```

hệ thống sẽ:

```text
Segment cũ nằm xa phía sau
        ↓
Recycle
        ↓
Di chuyển ra phía trước Player
```

Player cảm thấy mình đang tiếp tục di chuyển trong một công trình vô tận.

---

# 4. Đơn vị cơ bản: FloorSegment

Không chia từng bậc thang thành một segment riêng.

Một `FloorSegment` nên đại diện cho **một module hoàn chỉnh giữa hai tầng logic**.

Ví dụ:

```text
FloorSegment
│
├── Geometry
│   ├── Room / ObservationArea
│   ├── StairFlight_01
│   ├── MiddleLanding
│   ├── StairFlight_02
│   ├── Floor
│   ├── Ceiling
│   └── OuterWalls
│
├── ElevatorSection
│   ├── ElevatorWall
│   ├── ElevatorDoor
│   ├── BrokenPanel
│   ├── ShaftCover
│   └── StructuralDetails
│
├── Props
│
├── Anchors
│   ├── UpAnchor
│   └── DownAnchor
│
└── Triggers
    ├── UpCommitTrigger
    ├── DownCommitTrigger
    ├── UpRecycleTrigger
    └── DownRecycleTrigger
```

Ở giai đoạn environment hiện tại:

```text
Props
```

chỉ chứa môi trường bình thường.

Chưa có `AnomalyRoot`.

---

# 5. Sử dụng 5 FloorSegment vật lý

Khuyến nghị prototype sử dụng:

```text
5 FloorSegment
```

Ví dụ:

```text
          S+2
           │
          S+1
           │
          S0       ← Player
           │
          S-1
           │
          S-2
```

Ý nghĩa:

- `S0`: tầng hiện tại của Player;
- `S+1`, `S+2`: buffer phía trên;
- `S-1`, `S-2`: buffer phía dưới.

Nhờ vậy Player có thể:

```text
UP
UP
DOWN
UP
DOWN
DOWN
...
```

mà hệ thống vẫn luôn có đủ environment ở cả hai phía.

---

# 6. Sliding Window thay vì map vô hạn thật

World logic có thể được hình dung như:

```text
... -5 -4 -3 -2 -1 0 +1 +2 +3 +4 +5 ...
```

Nhưng Unity chỉ duy trì một cửa sổ vật lý:

```text
[-2] [-1] [0] [+1] [+2]
           ↑
         Player
```

Khi Player đi lên:

```text
[-1] [0] [+1] [+2] [+3]
          ↑
        Player
```

Segment thấp nhất được recycle lên trên.

Khi Player đi xuống:

```text
[-3] [-2] [-1] [0] [+1]
           ↑
         Player
```

Segment cao nhất được recycle xuống dưới.

Đây là **bidirectional sliding window**.

---

# 7. Recycle khi Player đi lên

Ban đầu:

```text
S+2
 │
S+1
 │
S0      ← Player
 │
S-1
 │
S-2
```

Player đi lên và chuyển sang `S+1`.

Lúc này `S-2` đã đủ xa và bị che hoàn toàn.

Thực hiện:

```text
S-2
 ↓
Reset
 ↓
Move above S+2
 ↓
Reuse
```

Sau đó:

```text
S-2     ← recycled thành segment mới phía trên
 │
S+2
 │
S+1     ← Player
 │
S0
 │
S-1
```

Player không bị thay đổi transform.

---

# 8. Recycle khi Player đi xuống

Ban đầu:

```text
S+2
 │
S+1
 │
S0      ← Player
 │
S-1
 │
S-2
```

Player đi xuống `S-1`.

`S+2` lúc này ở xa phía trên.

Thực hiện:

```text
S+2
 ↓
Reset
 ↓
Move below S-2
 ↓
Reuse
```

Kết quả:

```text
S+1
 │
S0
 │
S-1     ← Player
 │
S-2
 │
S+2     ← recycled thành segment mới phía dưới
```

---

# 9. Landing là vùng che việc recycle

Map vòng tròn hiện tại có lợi thế rất lớn:

Player không thể nhìn trực tiếp xuyên từ tầng hiện tại sang tầng tiếp theo.

Các yếu tố che khuất:

- landing;
- floor slab;
- ceiling;
- tường;
- lõi thang máy;
- góc quay 90° / 180°;
- stair geometry.

Recycle chỉ được thực hiện sau khi Player đi qua một **occlusion point**.

Ví dụ:

```text
Previous Stair
      │
      ↓

████████████ WALL / FLOOR ████████████

       Player
         ↓
      Landing
         ↓
 [Recycle Trigger]
         ↓
      Next Stair
```

Không recycle ở giữa cầu thang nếu Player vẫn có thể quay camera lại và nhìn thấy segment cũ.

---

# 10. Commit Trigger và Recycle Trigger là hai khái niệm khác nhau

Không nên dùng một trigger cho mọi nhiệm vụ.

## Commit Trigger

Xác định Player đã thực sự đi sang tầng kế tiếp theo hướng:

```text
UP
```

hoặc:

```text
DOWN
```

Ví dụ:

```text
UpCommitTrigger
DownCommitTrigger
```

Commit Trigger cập nhật logical current floor.

---

## Recycle Trigger

Đặt sâu hơn sau góc khuất.

Nó xác nhận:

```text
segment xa nhất phía sau đã an toàn để di chuyển
```

Ví dụ:

```text
Player qua CommitTrigger
        ↓
current logical floor thay đổi
        ↓
Player tiếp tục vòng qua landing
        ↓
RecycleTrigger
        ↓
far segment được recycle
```

Việc tách hai trigger giúp giảm lỗi visual popping.

---

# 11. Anchor-based connection

Không hard-code kiểu:

```csharp
segment.position += Vector3.up * 4f;
```

Map có:

- offset X;
- offset Z;
- height;
- rotation;
- hình dạng vòng.

Do đó mỗi `FloorSegment` cần:

```text
UpAnchor
DownAnchor
```

Khi đặt segment mới phía trên:

```text
NewSegment.DownAnchor
        ↓ align với ↓
CurrentTopSegment.UpAnchor
```

Khi đặt segment mới phía dưới:

```text
NewSegment.UpAnchor
        ↓ align với ↓
CurrentBottomSegment.DownAnchor
```

Hệ thống phải align cả:

```text
position
rotation
```

---

# 12. API đề xuất cho FloorSegment

Ví dụ:

```csharp
public class FloorSegment : MonoBehaviour
{
    public Transform UpAnchor;
    public Transform DownAnchor;

    public void PlaceAbove(FloorSegment segment);
    public void PlaceBelow(FloorSegment segment);

    public void ResetSegment();
}
```

`PlaceAbove()` và `PlaceBelow()` tự xử lý alignment.

LoopManager không cần biết segment có hình chữ U, chữ L hay xoắn quanh lõi.

---

# 13. Lõi thang máy trung tâm

Thang máy bị hỏng có hai nhiệm vụ:

## Spatial role

Che line-of-sight giữa:

- tầng trên;
- tầng dưới;
- landing bên kia;
- segment đang recycle.

## Environmental role

Tạo một điểm landmark cố định khiến Player cảm thấy toàn bộ công trình có kiến trúc hợp lý.

---

# 14. Không cho cả thang máy follow Player

Không thực hiện:

```csharp
elevator.transform.position =
    new Vector3(
        elevator.transform.position.x,
        player.transform.position.y,
        elevator.transform.position.z
    );
```

Lý do:

- cửa thang máy sẽ trôi theo Player;
- parallax sai;
- scratches và texture di chuyển;
- light/shadow sai;
- Player không cảm thấy mình thực sự thay đổi độ cao.

---

# 15. Không tạo một cột thang máy dài hàng kilomet

Không dùng:

```text
ElevatorColumn
Scale Y = 100000
```

Vì sẽ gây khó cho:

- UV;
- collider;
- lighting;
- editing;
- precision;
- modularity.

Thang máy cũng phải được xây theo module.

---

# 16. Phương án được chọn: ElevatorSection nằm trong từng FloorSegment

Ở giai đoạn environment hiện tại, giải pháp đơn giản và ổn định nhất là:

```text
FloorSegment
├── Stair Environment
└── ElevatorSection
```

Khi FloorSegment recycle:

```text
stairs
landing
room
elevator section
```

được di chuyển cùng nhau.

Mỗi ElevatorSection cao đúng bằng một floor module.

Các section xếp chồng:

```text
Floor +2
████ ElevatorSection ████

Floor +1
████ ElevatorSection ████

Floor  0
████ ElevatorSection ████

Floor -1
████ ElevatorSection ████

Floor -2
████ ElevatorSection ████
```

Từ góc nhìn Player, chúng tạo thành:

```text
████████████
████████████
████████████
████████████
████████████
```

một lõi thang máy liên tục.

---

# 17. Quy tắc geometry cho ElevatorSection

Mọi ElevatorSection phải có:

- cùng center X/Z;
- cùng footprint;
- cùng material;
- cùng rotation convention;
- cùng chiều cao module;
- top và bottom nối kín;
- không có gap;
- không có z-fighting.

Có thể overlap nhẹ giữa hai module khoảng:

```text
0.1m – 0.3m
```

nếu cần che khe ánh sáng.

Seam nên nằm tại:

```text
floor slab
```

hoặc vùng Player không thể quan sát trực tiếp.

---

# 18. Elevator Door

Mỗi floor có thể có một cửa thang máy hỏng.

Trong environment hiện tại:

```text
ElevatorSection
│
├── ElevatorDoor
├── DoorFrame
├── BrokenControlPanel
├── OutOfOrderSign
└── SurroundingWall
```

Tất cả phải có một **normal environmental state** cố định.

Giai đoạn này cửa:

```text
không mở
không tương tác
không thay đổi
```

Nhưng nên để chúng là GameObject/component riêng thay vì gộp trực tiếp vào mesh toàn bộ shaft.

Lý do:

sau này cửa thang máy có thể được mở rộng gameplay mà không phải sửa lại geometry core.

---

# 19. Tách Physical Segment và Logical Floor

Không dùng:

```text
Segment A = Floor 1
Segment B = Floor 2
```

`FloorSegment A` chỉ là physical module.

Nó có thể lần lượt đại diện:

```text
Logical Floor 1
Logical Floor 6
Logical Floor 11
Logical Floor 16
...
```

Logical state nên nằm trong manager:

```csharp
public class FloorRuntimeState
{
    public int LogicalFloorIndex;
}
```

Ở giai đoạn hiện tại chỉ cần track floor index.

Không cần anomaly state.

---

# 20. Manager architecture

Đề xuất:

```text
InfiniteBuildingManager
│
├── StairLoopManager
│   ├── segment pool
│   ├── current segment
│   ├── top/bottom segment
│   └── recycling
│
├── FloorProgressManager
│   ├── logical floor index
│   └── travel direction
│
└── EnvironmentDebugManager
```

Không cần `AnomalyManager` trong phase hiện tại.

---

# 21. TravelDirection

Dù anomaly chưa được làm, environment loop cần support cả:

```text
UP
DOWN
```

Enum:

```csharp
public enum TravelDirection
{
    Up,
    Down
}
```

Player có thể đổi hướng bất kỳ lúc nào.

Loop system không được giả định chỉ đi lên.

---

# 22. Queue một chiều không đủ

Không dùng duy nhất:

```csharp
Queue<FloorSegment>
```

nếu implementation chỉ hỗ trợ:

```text
A → B → C → D → A
```

vì Player có thể quay xuống.

Cần một cấu trúc có thể:

- lấy segment cao nhất;
- lấy segment thấp nhất;
- chuyển bottom → top;
- chuyển top → bottom.

Có thể sử dụng:

```csharp
LinkedList<FloorSegment>
```

hoặc một ordered list/deque abstraction.

Concept:

```text
Bottom
  ↓
[S-2]
[S-1]
[S0]
[S+1]
[S+2]
  ↑
Top
```

---

# 23. Khi đi lên

Pseudocode:

```text
Player commits UP
        ↓
Current floor shifts upward
        ↓
Wait until old bottom is fully occluded
        ↓
Remove BottomSegment
        ↓
ResetSegment()
        ↓
PlaceAbove(CurrentTopSegment)
        ↓
Add as new TopSegment
```

---

# 24. Khi đi xuống

Pseudocode:

```text
Player commits DOWN
        ↓
Current floor shifts downward
        ↓
Wait until old top is fully occluded
        ↓
Remove TopSegment
        ↓
ResetSegment()
        ↓
PlaceBelow(CurrentBottomSegment)
        ↓
Add as new BottomSegment
```

---

# 25. ResetSegment ở phase environment

Hiện tại `ResetSegment()` chỉ cần reset environment state.

Ví dụ:

```text
Transforms
Doors
Lights
AudioSources
Particles
Animations
Physics state
```

Ngay cả khi các object chưa thay đổi trong prototype, cần chuẩn bị API này từ đầu.

Không cần anomaly logic.

---

# 26. Global objects không được nằm trong recycled segment

Các object sau nên tồn tại ngoài segment pool:

```text
Player
Main Camera
AudioManager
Global Ambience
Game Manager
Lighting Manager nếu global
UI
```

Không để global ambience restart mỗi khi floor recycle.

---

# 27. Local environment audio

FloorSegment có thể chứa local audio:

```text
fluorescent hum
elevator machinery
pipe noise
electrical buzz
```

Nhưng khi recycle:

```text
local AudioSource
```

phải được reset nếu nó có transient state.

Ambient tổng thể vẫn chạy liên tục.

---

# 28. Lighting

Vì FloorSegment được reposition:

không nên thiết kế prototype dựa vào static world assumptions quá mạnh.

Ưu tiên ban đầu:

```text
Realtime lights
```

hoặc setup Mixed phù hợp.

Cẩn thận với:

- baked lightmap;
- static batching;
- baked occlusion culling;
- reflection probe cố định theo world position.

Mục tiêu phase đầu:

```text
seamless geometry loop
```

trước khi tối ưu lighting.

---

# 29. Static flags

Root và các object phải di chuyển cùng FloorSegment không nên được cấu hình theo cách Unity giả định chúng không bao giờ thay đổi transform.

Trong quá trình prototype:

- kiểm tra Static flags;
- tránh batching configuration khiến reposition không hoạt động đúng;
- kiểm tra collider sau khi move;
- chỉ dùng `Physics.SyncTransforms()` khi thực sự cần.

Không gọi nó mỗi frame.

---

# 30. World-space growth

Vì Player không bị teleport, tọa độ Y của Player sẽ tiếp tục tăng hoặc giảm.

Ví dụ:

```text
0m
4m
8m
12m
...
```

Trong demo ngắn điều này không phải vấn đề.

Nếu game cho phép hàng trăm/hàng nghìn loop, sau này cần cân nhắc:

```text
Floating Origin / World Origin Rebasing
```

Nhưng **không cần implement ngay trong phase environment prototype**.

Chỉ ghi chú đây là vấn đề cần kiểm tra trong stress test dài.

---

# 31. Scene hierarchy đề xuất

```text
InfiniteStairwayScene
│
├── Managers
│   ├── InfiniteBuildingManager
│   ├── StairLoopManager
│   ├── FloorProgressManager
│   └── EnvironmentDebugManager
│
├── Player
│
├── SegmentPool
│   ├── FloorSegment_A
│   ├── FloorSegment_B
│   ├── FloorSegment_C
│   ├── FloorSegment_D
│   └── FloorSegment_E
│
├── GlobalEnvironment
│   ├── GlobalAmbience
│   └── GlobalLighting
│
└── UI
    └── DebugPanel
```

---

# 32. Prefab hierarchy đề xuất

```text
PF_FloorSegment
│
├── Geometry
│   ├── ObservationRoom
│   ├── StairFlight_01
│   ├── Landing
│   ├── StairFlight_02
│   ├── Floor
│   ├── Ceiling
│   └── OuterWalls
│
├── ElevatorSection
│   ├── ShaftWalls
│   ├── ElevatorDoor
│   ├── DoorFrame
│   ├── BrokenPanel
│   ├── OutOfOrderSign
│   └── ShaftCover
│
├── EnvironmentProps
│
├── Anchors
│   ├── UpAnchor
│   └── DownAnchor
│
├── Triggers
│   ├── UpCommitTrigger
│   ├── DownCommitTrigger
│   ├── UpRecycleTrigger
│   └── DownRecycleTrigger
│
└── Debug
```

---

# 33. Folder structure đề xuất

```text
Assets/
└── Game/
    ├── Scenes/
    │   └── InfiniteStairwayScene.unity
    │
    ├── Prefabs/
    │   └── Environment/
    │       └── PF_FloorSegment.prefab
    │
    ├── Scripts/
    │   ├── EnvironmentLoop/
    │   │   ├── InfiniteBuildingManager.cs
    │   │   ├── StairLoopManager.cs
    │   │   ├── FloorSegment.cs
    │   │   ├── FloorProgressManager.cs
    │   │   ├── TravelDirection.cs
    │   │   ├── SegmentCommitTrigger.cs
    │   │   └── SegmentRecycleTrigger.cs
    │   │
    │   └── Debug/
    │       └── EnvironmentLoopDebug.cs
    │
    ├── Materials/
    ├── Audio/
    └── Models/
```

---

# 34. Development phases

## Phase 1 — Hoàn thiện một FloorSegment

Mục tiêu:

- một tầng hoàn chỉnh;
- room;
- stairs;
- landing;
- central elevator section;
- wall/ceiling/floor;
- collider sạch.

Chưa loop.

Test Player đi lên và xuống một segment bình thường.

---

## Phase 2 — Anchor alignment

Tạo:

```text
UpAnchor
DownAnchor
```

Duplicate FloorSegment thủ công thành 3 tầng.

Kiểm tra:

- không gap;
- không overlap sai;
- stairs nối chính xác;
- elevator core tạo thành cột liên tục.

---

## Phase 3 — 5-segment physical pool

Tạo:

```text
A
B
C
D
E
```

xếp đúng thứ tự.

Chưa recycle.

Test Player có thể đi xuyên cả 5 tầng.

---

## Phase 4 — Upward recycling

Implement:

```text
bottom → top
```

Player đi lên liên tục ít nhất 20 floor.

Không hỗ trợ DOWN trước khi UP ổn định.

---

## Phase 5 — Downward recycling

Implement:

```text
top → bottom
```

Test đi xuống liên tục ít nhất 20 floor.

---

## Phase 6 — Bidirectional stress test

Test:

```text
UP
UP
DOWN
UP
DOWN
DOWN
UP
UP
...
```

Hệ thống phải giữ đúng segment ordering.

---

## Phase 7 — Occlusion / trigger tuning

Di chuyển:

```text
CommitTrigger
RecycleTrigger
```

cho đến khi không thể nhìn thấy recycling từ bất kỳ góc camera hợp lý nào.

Không dùng fade để che lỗi.

---

## Phase 8 — Environment polish

Sau khi loop đã ổn định:

- material;
- lighting;
- elevator visual;
- props;
- audio;
- decals;
- pipes;
- signs;
- room dressing.

Không làm ngược thứ tự này.

---

# 35. Debug tools

Nên có debug overlay bật bằng `F1`.

Hiển thị:

```text
Logical Floor:
42

Travel Direction:
UP

Current Segment:
Segment C

Bottom:
Segment A

Top:
Segment E

Pool:
A → B → C → D → E

Recycle Count:
37
```

Gizmos Scene View nên hiển thị:

```text
UpAnchor
DownAnchor
CommitTrigger
RecycleTrigger
```

---

# 36. Validation tests

## Test A — 50 tầng đi lên

Player đi UP 50 lần.

Expected:

- không teleport;
- không fade;
- không scene reload;
- chỉ có 5 FloorSegment;
- không gap;
- không pop;
- elevator core luôn liền.

---

## Test B — 50 tầng đi xuống

Tương tự cho DOWN.

---

## Test C — đổi hướng liên tục

Pattern:

```text
UP
DOWN
UP
DOWN
UP
DOWN
```

sau đó:

```text
UP
UP
DOWN
DOWN
UP
DOWN
DOWN
UP
```

Không được:

- duplicate segment;
- đảo sai thứ tự;
- mất floor;
- segment overlap.

---

## Test D — Camera abuse

Tại mỗi landing:

- quay camera 180°;
- nhìn lên;
- nhìn xuống;
- đứng sát tường;
- đi lùi;
- chạy nhanh qua trigger.

Không được nhìn thấy segment bị reposition.

---

## Test E — Elevator continuity

Quan sát thang máy trong nhiều tầng.

Không được thấy:

- shaft biến mất;
- cửa teleport trước mặt;
- seam rõ;
- material flicker;
- gap giữa module;
- z-fighting.

---

# 37. Acceptance criteria cho Environment Loop

Environment phase chỉ hoàn thành khi:

- map vòng tròn hoạt động;
- Player đi UP vô hạn;
- Player đi DOWN vô hạn;
- Player đổi hướng tự do;
- không teleport Player;
- không fade;
- không reload scene;
- không instantiate/destroy floor liên tục;
- sử dụng pool 5 FloorSegment;
- segment recycle hai chiều;
- Player không nhìn thấy recycle;
- landing/lõi thang máy che tốt;
- central elevator core luôn liên tục;
- elevator section recycle cùng floor;
- anchor alignment chính xác;
- collider không tạo gap;
- global audio không restart;
- scene không spam error;
- chạy stress test nhiều vòng ổn định.

---

# 38. Những thứ CHƯA làm ở phase này

Không triển khai:

```text
anomaly selection
anomaly spawning
horror events
monster
jumpscare
chase
progress puzzle
correct/incorrect anomaly decision
ending
HorrorDirector
```

Environment phải hoàn thiện trước.

---

# 39. Chuẩn bị cho phase anomaly tương lai

Dù chưa triển khai anomaly, tránh gộp toàn bộ environment thành một mesh không thể kiểm soát.

Đặc biệt nên giữ riêng:

```text
ElevatorDoor
ElevatorPanel
Lights
Props
Signs
Room objects
```

để sau này có thể thay đổi chúng độc lập.

Tuy nhiên **không viết anomaly code trong phase hiện tại**.

Mục tiêu chỉ là tránh phải remodel/refactor environment sau này.

---

# 40. Architecture summary

Hệ thống cuối cùng của phase environment:

```text
                     PLAYER
                       │
                       ▼
              Floor Commit Trigger
                       │
                       ▼
              FloorProgressManager
                       │
              UP / DOWN movement
                       │
                       ▼
               StairLoopManager
                       │
          ┌────────────┴────────────┐
          │                         │
        UP move                  DOWN move
          │                         │
          ▼                         ▼
 Bottom → Top                 Top → Bottom
          │                         │
          └────────────┬────────────┘
                       ▼
                FloorSegment Pool
                       │
      ┌────────────────┼────────────────┐
      │                │                │
   Stairs           Landing       ElevatorSection
                                         │
                                         ▼
                              continuous central core
```

---

# 41. Design principle

Player phải có cảm giác:

```text
Tôi tiếp tục đi lên.
       ↓
Tầng tiếp theo.
       ↓
Tầng tiếp theo.
       ↓
Tầng tiếp theo.
       ↓
Tôi quay xuống.
       ↓
Tầng dưới tiếp tục xuất hiện.
       ↓
Không có dấu hiệu map reset.
```

Trong khi Unity thực tế chỉ có:

```text
5 reusable FloorSegment
```

và âm thầm thực hiện:

```text
Reset
↓
Reposition
↓
Reuse
```

ở phía Player không thể quan sát.

**Nguyên tắc quan trọng nhất của toàn bộ hệ thống:**

> Player không loop. Thế giới loop quanh Player.
