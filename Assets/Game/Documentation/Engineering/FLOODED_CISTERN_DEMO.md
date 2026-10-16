# The Flooded Cistern — demo authoring

## Mở và chơi

Mở `Assets/Game/Scenes/Demos/Minigame5_FloodedCistern_Demo.unity`.
Nhấn Play: WASD di chuyển, chuột nhìn, Shift chạy. Demo tái sử dụng
`FirstPersonController`; không cần mở Title hoặc campaign để xem map.

Đây là demo môi trường riêng, chưa có chase, death, completion hoặc checkpoint.
Không thay đổi readiness của Minigame5, Build Settings hay các scene gameplay.

## Bố cục và chỉnh sửa

- Bể chứa đã mở rộng thành khoảng 42 × 84 m, các hàng trụ vẫn cách nhau 6 m.
- Phần mới kéo dài về phía cửa vào 48 m và rộng thêm 6 m mỗi bên. Giữ nguyên
  cầu thang, khu vực shaft và kích thước từng vòm; không scale toàn bộ root.
- `CisternArrivalAnchor`/Player bắt đầu ở Z=-65; chân cầu thang ở Z≈5.
  Khoảng cách tới chân cầu thang tăng từ khoảng 22 m lên 70 m; tới exit marker
  Z=16 là khoảng 81 m theo phương ngang.
- Với tốc độ demo 3.2 m/s khi đi và 6 m/s khi chạy, đường thẳng tới chân cầu
  thang mất khoảng 22 s/12 s, chưa tính tránh mannequin, quan sát hoặc chase.
  Đây là tính toán khoảng cách, không phải thời lượng minigame đã playtest.
- Vòm thấp, rộng; trần chính cao khoảng 3.5 m, không dùng phong cách trung cổ.
- Gạch men vuông trắng/xám dùng tọa độ world-space, cỡ 0.28 m.
- Nước sâu 0.24 m, sàn thật và collider nằm bên dưới; mặt nước không chặn Player.
- Cầu thang nằm trên trục giữa, 16 bậc cao 0.18 m, dẫn lên vùng sáng ấm.
- Mannequin tái sử dụng mesh có sẵn, không mặc đồ. Chúng chưa có AI.
- `CisternArrivalAnchor` và `CisternExitMarker` chỉ là marker để author gameplay sau.

Các nhóm `Cistern_Architecture`, `Cistern_Staircase`,
`Cistern_Water`, `Cistern_Mannequins`, `Cistern_Detail`, `Cistern_Lighting`
cho phép chỉnh từng phần mà không tác động đến các Infinity Room khác.
Sàn, tường và cầu thang giữ ProBuilder mesh. Trần/vòm nằm trong
`Cistern_Architecture/Cistern_ModularVaults`, gồm 356 instance dùng chung prefab
`Assets/Game/Prefabs/CisternDemo/Cistern_QuarterVault.prefab` và mesh asset
`Cistern_QuarterVault.asset`. Đây là mesh thường, không lưu vertex override riêng
trên từng instance.

Mỗi module là một góc vòm kín 3 × 3 m. Các bản xoay 0°/90°/180°/270° ghép quanh
điểm trụ tạo bốn nhánh vòm cong liên tục. Chân liền trong module, bắt đầu ở
Y=-0.35 dưới nước; không có object cube hoặc bệ riêng bên dưới. Đỉnh vòm Y=3.5,
mặt trên kín Y=3.8. Lưới hiện có 7 × 13 ô; hai ô trên cầu thang được để trống
cho shaft cao sẵn có, còn 89 ô vòm kín.

Đoạn mở rộng bổ sung 24 mannequin trang trí và bốn light không đổ bóng;
mannequin cũ được giữ nguyên. Nước/sàn/tường bao phủ cả footprint mới. Probe
được mở vùng ảnh hưởng lên 44 × 7 × 86 m, không thêm camera phản chiếu realtime.

Chỉnh vật liệu/cấu trúc module trong Prefab Mode để các instance cùng nhận thay đổi.
Đường cong được author trong `Editor/CisternQuarterVaultMesh.cs`. Sau khi chỉnh,
menu `Tools > Zero > Cistern Demo > Create Modular Arch Master` cập nhật đúng mesh
asset dùng chung; chạy lại không phụ thuộc các `Roof_Bay` đã xóa và không ghi mesh
rỗng. Nó chỉ tạo master để kiểm tra, không tự save hoặc dựng lại toàn scene.
Menu `Finalize Modular Vault Replacement` chỉ dọn geometry cũ/master sau khi xác
nhận các prefab quarter tạo thành nhóm bốn, mesh và collider hợp lệ. Menu
`Validate Modular Vault Traversal` kiểm tra trần theo kích thước sàn hiện tại
và đường từ arrival tới chân cầu thang, không còn giới hạn cứng 92 instance
hay tuyến 20 m. Nhấn Ctrl+S để lưu scene; không cần chạy menu tạo master để
sử dụng các vòm đã đặt sẵn.

## Mặt nước URP

Vật liệu: `Assets/Game/Materials/CisternDemo/M_Cistern_Water.mat`.
Shader: `Zero/Cistern Water URP`, hỗ trợ renderer Forward/Forward+ của project.

Trong Inspector material:

- Ripple Scale/Speed/Strength: kích thước, tốc độ và độ gợn.
- Depth Absorption: độ tối theo chiều sâu; không thay đổi độ sâu vật lý.
- Refraction: độ méo ảnh sàn dưới nước.
- Reflection Strength: cường độ phản chiếu môi trường.

Camera demo đã bật Depth Texture và Opaque Texture riêng. Không cần cài package
nước hoặc bật các tính năng này trên toàn bộ project. Không có camera phản chiếu
phụ chạy mỗi frame; phản chiếu dùng cubemap baked, nên không phản chiếu chính xác
Player/mannequin đang di chuyển như planar reflection hoặc SSR.

## Bake lại phản chiếu khi đổi geometry/đèn

Quality hiện tại tắt realtime reflection probes. Demo dùng baked probe để không
phải thay Quality hoặc thêm chi phí render sáu mặt mỗi frame.
Probe giữ chế độ Custom và reference cubemap explicit sau bake, để vẫn hoạt động
khi reload/Play Mode mà không cần một LightingDataAsset từ global light bake.

1. Dừng Play Mode.
2. Chọn `Cistern_Lighting/Cistern_ReflectionProbe`.
3. Chạy `Tools > Zero > Cistern Demo > Bake Selected Reflection Probe`.
4. Nhấn Ctrl+S để lưu scene sau khi tự chỉnh sửa.

Menu chỉ bake probe đã chọn trong scene demo, ghi
`Assets/Game/Materials/CisternDemo/Cistern_Reflection.exr`. Nó không chạy sau
compile/domain reload, không mở hoặc save scene và không bake các scene khác.

Kết cấu cố định dùng Batching Static và Reflection Probe Static. Mannequin hiện chỉ
có Reflection Probe Static để xuất hiện trong bản bake; khi thêm AI di chuyển,
xóa flag này trên mannequin. Bản bake chỉ giữ pose lúc bake, không theo đuổi chuyển động.

Look của demo nằm trong `VP_CisternDemo.asset`: ánh sáng trắng/xám yếu, lối thoát
ấm, không dùng ánh sáng xanh tím. Đây là profile riêng, không sửa OldFilm hoặc
profile của gameplay.

## Vòng sóng bước chân

`Cistern_Water/Cistern_WaterRipples` có `WaterFootstepRippleController`:

- Player: CharacterController của Player thực tế.
- Water Surface: MeshRenderer của `Cistern_WaterSurface`.
- Ripple Material: `M_Cistern_FootstepRipple.mat`, shader `Zero/Water Footstep Ripple URP`.
- Step Distance: mặc định 0.65 m. Chạy nhanh tạo sóng dày hơn theo thời gian;
  đứng yên hoặc xoay camera không phát vòng sóng.
- Lifetime: 1.7 s; Max Diameter: 2.3 m; Pool Size: 12, tối đa 24.
- Surface Offset: 0.008 m để tránh z-fighting với mặt nước.

Component kiểm tra chân Player grounded, nằm trong footprint nước và ngập
0.02–1 m; lên cầu thang/ra nước không phát sóng. Không cần thêm trigger hoặc
bật collider của mặt nước. Teleport quá 3 m/frame không phát sóng giả.
`ClearRipples()` dùng khi reset/respawn; disable tự clear, destroy giải phóng
mesh và pool. Khi dùng trong scene additive, bootstrap gán Player thực tế qua
`Configure(player, surface, material)` thay vì tìm Player theo tên.

Pool chỉ tạo lúc khởi tạo; dùng chung mesh/material, MaterialPropertyBlock,
không tạo collider, material instance, camera phản chiếu hoặc object mỗi frame.
Đây là vòng sóng presentation, không phải mô phỏng chất lỏng và không đẩy vật thể.
Không sửa shader nước cũ, bố cục map, tốc độ Player hay hệ thống âm thanh.

## Kiểm tra thực hiện

- Ripple sampler: 4/4 EditMode test pass (đứng yên, chuyển động ngang, teleport, reset).
- Ripple controller: 2/2 PlayMode test pass (di chuyển tạo sóng, disable dọn pool,
  thiếu reference fail-safe). Shader `Zero/Water Footstep Ripple URP` không có compile message.
- Chưa xác nhận chất lượng vòng sóng bằng preview trong map thực tế: ảnh kiểm tra
  nhìn xuống mặt nước quá tối; cần chạy Player đi trong nước để đánh giá độ tương phản.
- `CisternQuarterVaultMeshTests`: chạy lại sau mở rộng, 2/2 EditMode test pass
  (mesh kín, winding/normal đúng, tạo lại không phụ thuộc geometry cũ trong scene).
- Bản trước khi mở rộng đã xác nhận 92 prefab instance, 23 ô trần và scene
  khoảng 1.35 MB. Bản mở rộng có 356 prefab instance; sau khi người dùng lưu,
  scene là 2,380,669 byte (khoảng 2.38 MB), không nhúng mesh riêng từng vòm.
- Bản mở rộng: validator đã pass 89 ô có trần kín và spherecast tuyến giữa
  68.5 m không bị chặn. Sàn/nước trải tới cửa vào mới; cầu thang giữ vị trí cũ.
- Reflection probe đã bake lại sau mở rộng; các light mới không đổ bóng.
- Hai shader compile không có error/message trên URP hiện tại.
- Sau save và vào lại Play Mode, Custom cubemap reference vẫn tồn tại; Console
  không ghi error trong lần kiểm tra demo cuối.
- Player grounded ở điểm đầu; raycast xác nhận sàn Y=0 và chiếu nghỉ Y=2.88.
- Spherecast dọc trục giữa từ cửa vào đến trước cầu thang không gặp vật cản.
- Code Policy đã chạy nhưng project còn 13 lỗi legacy ở các script khác; không
  nâng baseline hoặc sửa các module ngoài phạm vi để che kết quả này.
- Chưa nghiệm thu chase, NavMesh, checkpoint hoặc FPS trên thiết bị mục tiêu.
