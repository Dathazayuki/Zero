# Anomaly Demo — Asset Replacement List

Danh sách này dùng để thay các primitive placeholder trong `AnomalyDemo`. Ưu tiên giữ nguyên root GameObject đang được gameplay tham chiếu; thay hoặc thêm mesh ở bên dưới root đó để anomaly không mất reference.

## Quy ước kỹ thuật chung

- Pipeline: Unity URP 17, material dùng `Universal Render Pipeline/Lit`.
- Tỉ lệ: 1 Unity unit = 1 mét.
- Model: FBX hoặc prefab, transform scale `(1,1,1)`, không dùng negative scale.
- Pivot: đặt ở chân vật thể với prop đứng; cửa đặt pivot ở bản lề; kim đồng hồ đặt pivot tại tâm.
- Collider: ưu tiên Box/Capsule Collider đơn giản. Chỉ dùng Mesh Collider cho mesh tĩnh thật sự cần thiết.
- Texture gợi ý: 1K cho prop nhỏ, 2K cho module kiến trúc/hero prop; Base Color, Normal, Mask/Metallic, AO nếu có.
- Không gắn gameplay script trực tiếp vào imported FBX. Giữ script trên root trong scene và đặt model làm child.

## P0 — Asset cần thay trước

| ID | Asset cần tạo/tìm | Placeholder / vị trí scene | Yêu cầu quan trọng | Từ khóa tìm kiếm gợi ý |
|---|---|---|---|---|
| ENV-01 | Modular stacked stairwell kit | `Environment-HandMade` | Floor, landing, stair wall/stringer và railing dạng module; giữ đúng tên anchor trong README; collider sạch | `infinite stacked stairwell`, `brutalist stairs modular URP`, `liminal stairwell environment` |
| PROP-01 | Analog wall clock | `AnomalyDemo/Environment/FloorAnomalyContent/Props/Clock` | Hour hand và minute hand phải là Transform riêng, pivot ở tâm để Wrong Clock hoạt động | `old analog wall clock separate hands`, `vintage station clock game ready` |
| PROP-02 | Framed painting | `AnomalyDemo/Environment/FloorAnomalyContent/Props/Painting` | Toàn bộ tranh nằm dưới một root có thể bật/tắt; artwork không dùng nội dung có bản quyền | `vintage framed painting low poly`, `creepy generic portrait frame` |
| PROP-03 | Wooden chair | `AnomalyDemo/Environment/FloorAnomalyContent/Props/Chair` | Pivot ở chân ghế; silhouette dễ nhớ; collider đơn giản | `old wooden chair game ready`, `abandoned chair low poly` |
| PROP-04 | Recognizable side door | `AnomalyDemo/Environment/FloorAnomalyContent/Props/Door` | Door leaf, frame và handle tách được nếu có; root phải giữ nguyên cho Missing Door | `old apartment door horror`, `worn wooden door modular` |
| PROP-05 | Statue / mannequin | `AnomalyDemo/Environment/FloorAnomalyContent/Props/Statue` | Hướng nhìn chuẩn là local `+Z`; pivot ở chân; có collider ở thân/đầu để camera raycast nhận diện | `creepy statue game ready`, `faceless mannequin low poly`, `stone human statue URP` |
| PROP-06 | Ceiling light fixture | `AnomalyDemo/Environment/FloorAnomalyContent/Lights/CeilingLight_*` | Mesh fixture riêng với component `Light`; không bake emissive quá sáng; dùng chung prefab | `fluorescent ceiling light industrial`, `old hallway lamp game ready` |
| HOR-01 | Shadow silhouette | `Environment/ShadowSilhouette` | Humanoid tối màu, không cần rig; pivot chân; silhouette đọc rõ trong dưới 1 giây | `shadow person silhouette 3D`, `low poly humanoid shadow` |

## P1 — Environment và visual dressing

| ID | Asset cần tạo/tìm | Mục đích | Từ khóa tìm kiếm gợi ý |
|---|---|---|---|
| ENV-02 | Stair transition booth / blind landing | Che teleport ở đầu cầu thang trên và dưới | `stairwell landing modular`, `concrete stair transition`, `brutalist stairwell` |
| ENV-03 | Railing, stair trim, pipe set | Tạo silhouette Penrose và landmark lặp lại dễ ghi nhớ | `stair railing modular`, `industrial pipe modular`, `brutalist handrail` |
| ENV-04 | Crate / storage prop set | Thay cube phụ, tạo bố cục bất đối xứng | `abandoned storage props`, `wood crate game ready` |
| ENV-05 | Grime, leak và crack decals | Phá bề mặt phẳng mà không tăng polygon đáng kể | `URP grime decals`, `wall leak decal`, `plaster crack decal` |
| ENV-06 | Sign/gate cho UP, DOWN, EXIT | Thay khung màu debug; UP tương ứng normal, DOWN tương ứng anomaly | `stair direction sign`, `industrial doorway frame`, `exit portal frame` |
| VIS-01 | Subtle dust particle | Không khí tĩnh, mật độ rất thấp | `dust motes particle Unity URP` |
| VIS-02 | Light flicker response | Emissive material hoặc fixture mesh đổi đồng bộ với Light | `fluorescent light flicker URP` |

## Material và texture cần chuẩn bị

Các material placeholder hiện nằm tại `Assets/Game/Materials/Prototype/` và được share bởi object trong scene.

| Material hiện tại | Material/texture thay thế | Ghi chú |
|---|---|---|
| `Wall` | Painted plaster / concrete wall | Nên có variation sạch và bẩn để dùng anomaly môi trường sau này |
| `Floor` | Worn tile, concrete hoặc linoleum | Roughness cao, không phản chiếu quá mạnh |
| `Ceiling` | Pale concrete/plaster stair tread | Màu sáng để từng bậc thang dễ đọc trong ánh sáng thấp |
| `Old Wood` | Worn varnished wood | Dùng cho chair, door và painting frame; có thể tách thành 2–3 material sau |
| `Almost Black` | Statue/shadow dark material | Statue vẫn cần normal detail; shadow gần như unlit nhưng không hoàn toàn phẳng |
| `Brass` | Aged brass/metal | Handle và chi tiết clock |
| `Painting` | Original generic artwork | Tự tạo artwork hoặc dùng nội dung có license rõ ràng |
| `Normal Blue`, `Anomaly Red`, `Exit Green` | Signage/emissive language | Chỉ là debug language; final art nên truyền đạt bằng hình dạng/ánh sáng thay vì chữ lớn |

## Audio cần tạo hoặc tìm

| ID | Audio | Yêu cầu | Từ khóa tìm kiếm gợi ý |
|---|---|---|---|
| AUD-01 | Footsteps behind player | 4–8 bước, mono, dry, không nhạc; WAV 44.1/48 kHz | `indoor concrete footsteps mono`, `hallway footsteps behind` |
| AUD-02 | Fluorescent hum loop | Loop kín 20–60 giây, không click ở điểm nối | `fluorescent light hum loop`, `electrical room tone` |
| AUD-03 | Light flicker/buzz | 2–4 variation ngắn | `fluorescent flicker buzz`, `electric light sputter` |
| AUD-04 | Infinite stairwell room tone | Stereo ambience rất nhẹ, có reverb dài nhưng không chứa jumpscare | `empty stairwell room tone`, `concrete stairwell ambience`, `liminal space ambience` |
| AUD-05 | Distant building sounds | Pipe knock, metal creak, distant door; variation rời | `distant pipe knock`, `building settling horror ambience` |
| AUD-06 | Decision/loop transition cue | Một transient rất kín đáo khi teleport/reset | `subtle horror transition whoosh`, `low tension sting` |
| AUD-07 | Escape ambience/cue | Cảm giác relief nhưng vẫn bất an | `dark ambient resolution sting` |

`FootstepsBehindEvent` vẫn chạy an toàn khi chưa có `AUD-01`: game tự sinh procedural fallback và không phát sinh NullReferenceException.

## UI và typography

| ID | Asset | Ghi chú |
|---|---|---|
| UI-01 | Minimal crosshair | 16–32 px, trắng/xám, nền trong suốt |
| UI-02 | Readable horror font | Chỉ dùng cho instruction/ending; debug panel có thể giữ font mặc định |
| UI-03 | Interaction prompt icon | Keyboard `E`, sau này có thể mở rộng gamepad |
| UI-04 | Subtle screen fade | Dùng Canvas/Image hoặc Volume để che teleport nếu cần |

## Cách thay asset mà không làm hỏng gameplay

1. Mở `AnomalyDemo` và tìm root tương ứng trong `AnomalyDemo/Environment/FloorAnomalyContent/Props` hoặc `Lights`.
2. Giữ nguyên root, `NormalStateSnapshot` và anomaly references.
3. Disable hoặc xóa các primitive child, sau đó đặt prefab/model mới làm child của root.
4. Clock chỉ là prop bình thường, không còn anomaly thay đổi kim đồng hồ.
5. Với Painting, trạng thái normal phải ẩn visual; anomaly `Strange Painting` mới làm visual xuất hiện.
6. Với Statue, bảo đảm mặt nhìn theo local `+Z`; cập nhật collider để line-of-sight raycast có thể chạm tượng.
7. Với Light, giữ component `Light` hoặc cập nhật mảng `lights` trong `LightFlickerEvent`.
8. Chạy `Tools > Anomaly Demo > Run Smoke Validation`, sau đó test F2/F3 trong Play Mode.

`Create or Rebuild Editable Scene` giữ nguyên root `Environment-HandMade`, nhưng tạo lại root gameplay `AnomalyDemo`. Hãy backup/commit mọi art-pass nằm trong `AnomalyDemo` trước khi rebuild.
