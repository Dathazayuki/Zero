# Kế hoạch 20 anomaly — Infinity Room 4: The Hotel Kitchen

## Mục tiêu và phạm vi

Infinity Room 4 là căn bếp thương mại cũ của một khách sạn. Một tên đồ tể béo, đeo mặt nạ làm từ da heo khâu vá, chế biến món ăn từ những nạn nhân hắn sát hại. Không khí đi từ những bất thường nhỏ trong căn bếp, qua dấu vết có người bị hại, đến các mối đe dọa trực tiếp.

Danh sách gồm **20 anomaly** sau khi gộp IR4-14 vào IR4-12 và thêm IR4-20/21: 4 thay đổi nhỏ khó nhận ra, 6 bất thường rõ ràng, 6 cảnh kinh dị không gây chết người và 4 anomaly có thể giết Player. Chỉ một anomaly xuất hiện trong mỗi lượt; danh sách là catalog lựa chọn, không bắt buộc phát theo thứ tự ID. Giữ luật hiện có: gặp anomaly thì đi ngược hướng mũi tên; nếu phòng bình thường thì đi theo mũi tên.

## Danh sách anomaly

### Nhóm A — Thay đổi nhỏ, cần quan sát kỹ

| ID | Anomaly | Dấu hiệu và tài sản cần có |
|---|---|---|
| IR4-01 | Khối thịt biến mất | Tắt GameObject `Ceramic` thuộc `IR4_Detail_8_ChoppingStation`; các vật dụng khác trên trạm giữ nguyên để tạo khoảng trống bất thường. |
| IR4-02 | Bồn rửa lật ngược | `IR4_Sink` đổi `localScale.x` thành `-1` trong trạng thái anomaly. Xác nhận góc nhìn trong game cho thấy bồn bị lật ngược rõ ràng. |
| IR4-04 | Dao chặt thịt không dính máu | Bình thường đồ tể cầm `BloodKnife` dính máu. Khi có anomaly, tắt `BloodKnife` và bật `NoBloodKnife`, khiến dao sạch bất thường. Giữ nguyên vị trí gắn tay và chuỗi động tác lặp của NPC; F2/đổi anomaly khôi phục `BloodKnife` bật, `NoBloodKnife` tắt. |
| IR4-07 | Đồ tể nhìn theo Player | Đồ tể chỉ theo dõi khi Player nằm trong vùng nhìn 180° phía trước thân (±90°), không nhìn xuyên ra phía sau. Đầu/mắt quay theo Player trong vùng này; thân vẫn giữ chuỗi hành động lặp bình thường. |

### Nhóm B — Bất thường dễ nhận ra

| ID | Anomaly | Dấu hiệu và tài sản cần có |
|---|---|---|
| IR4-03 | Xe đẩy đồ ăn sai vị trí | Di chuyển xe đẩy đồ ăn `IR4_Detail_4_ServiceCart` ra xa vị trí cũ. Lưu transform bình thường và transform anomaly; giữ cấu hình target và độ dịch chuyển đã gán trong Inspector. |
| IR4-05 | Lò bếp tự bật lửa | Bếp tự phát lửa khi Player tiến vào; ngọn lửa là dấu hiệu bất thường và không gây sát thương trong anomaly này. Cần bật/tắt hiệu ứng lửa và ánh sáng bếp. |
| IR4-06 | Răng rơi vào hai bồn rửa | Nhiều instance của `Assets/Game/Prefabs/InfinityRoom4Demo/Teeth.prefab` rơi tự do vào cả hai khoang của `IR4_Sink` bằng Rigidbody và collider. Không thêm lực đẩy, torque hoặc điều khiển quỹ đạo. Phòng bình thường không có răng; F2/đổi anomaly dừng vật lý, ẩn và đặt lại các instance. Không gây sát thương. |
| IR4-08 | Heo còn sống | Bật `Pig_White` và tắt `DeadPig_LP`. Bình thường/F2: `Pig_White` tắt, `DeadPig_LP` bật. Giữ nguyên transform và animation người dùng đã đặt; không gây sát thương. |
| IR4-09 | Máu tràn từ vòi bồn rửa | Vòi mở, chất lỏng đỏ sẫm như máu chảy vào bồn đến khi tràn qua mép rồi loang xuống sàn. Cần hiệu ứng dòng chảy, mức chất lỏng tăng trong bồn và vệt máu trên sàn; đây là anomaly thị giác, không gây sát thương. |
| IR4-20 | Đàn gián tháo chạy khỏi giàn nồi chảo | Nhiều con gián ẩn sau giàn nồi chảo bên trái. Khi Player đến gần, chúng tản chạy lung tung để trốn vào khe trần và các tủ phía dưới. Đây là cảnh kinh dị dễ nhận ra nhưng không gây sát thương; cần đàn gián nhỏ, chuyển động chạy phân tán và điểm trú ở trần/tủ để tạo cảm giác chúng biến mất vào góc khuất. |

### Nhóm C — Kinh dị, không gây chết người

| ID | Anomaly | Dấu hiệu và tài sản cần có |
|---|---|---|
| IR4-10 | Bộ phận người trong nồi sôi | Nồi nước dùng đang sôi chứa nhiều bộ phận người, chuyển động theo nhịp sôi của chất lỏng. Cần bộ phận cơ thể, hiệu ứng nước sôi và hơi nước; anomaly không gây sát thương. |
| IR4-11 | Tủ lạnh bất ngờ mở | Khi Player đến gần, hai cửa tủ lạnh đồng thời bật mở nhanh. Không truy đuổi hoặc gây sát thương. Các bộ phận bên trong do người dùng thêm sau; anomaly chỉ điều khiển hai cửa, không bật/tắt, tạo hoặc di chuyển đồ bên trong. |
| IR4-12 | Đầu heo và thân người thay heo chết | Gộp nội dung IR4-12 và IR4-14: hiện đồng thời `PigHead` và `HumanBody`, ẩn `DeadPig_LP`. Giữ nguyên vị trí đã đặt. Không gây sát thương; reset ẩn hai phần và hiện lại heo chết. |
| IR4-13 | Lò nướng tự bật mở | Player đến gần thì cửa khoang lò bên trái bật mở, sau đó khay trượt ra mang theo một nồi súp đỏ hơi trong đang sôi, có bọt và hơi nước; bên trong chứa `Head` và `Bone`. Không truy đuổi hoặc gây sát thương. Nồi/nội dung chỉ hiện trong anomaly, reset ẩn lại và dừng VFX. |
| IR4-19 | Tiếng người bị nhốt trong tủ lạnh | Khi Player đến gần, tủ lạnh bắt đầu rung lắc; bên trong vang lên tiếng kêu và tiếng đập nghẹt như có người đang bị trói nhốt. Cửa vẫn đóng và anomaly không gây sát thương. Cần chuyển động rung của tủ và lớp âm thanh tiếng kêu/va đập bị bóp nghẹt; dừng cả hai khi Player rời vùng hoặc anomaly được reset. |

| IR4-21 | Khuôn mặt trong khe thông gió | Thuộc nhóm C. Khuôn mặt từ `Assets/TripoModels/scary_daruma_3d_model/scary_daruma_3d_model.fbx` nhìn qua khe `IR4_Detail_2_CeilingVent`. Khi Player nhìn thấy, đầu rút thẳng lên trên vào ống rồi biến mất, không gây sát thương. Vị trí/model/nan khe giữ theo setup của người dùng trong scene. |

**IR4-14 đã gộp vào IR4-12**, không còn asset/lượt chọn riêng và không đánh lại số các ID còn lại.

### Nhóm D — Có thể giết Player

| ID | Anomaly | Dấu hiệu, cách né và tài sản cần có |
|---|---|---|
| IR4-15 | Đàn chuột trong ống thông gió | Khi Player đến gần miệng ống, nắp rung và có tiếng cào báo trước; sau nhịp báo hiệu, đàn chuột tràn xuống, lao theo và tấn công Player. Player phải lập tức chạy khỏi vùng ống và tránh đường lao; bị đàn chuột chạm sẽ chết. Cần miệng ống mở được, đàn chuột nhiều cá thể hoặc swarm effect, đường lao và cửa sổ báo trước rõ ràng. |
| IR4-16 | Chuột trắng bật khỏi mũ đầu bếp | Đầu bếp đổ gục như đã chết, chiếc mũ rơi xuống; sau một nhịp im lặng, chuột trắng chui khỏi mũ rồi bật thẳng vào mặt Player. Player phải né ngang ngay khi chuột lộ diện; bị cú vồ chạm sẽ chết. Cần pose/animation đầu bếp ngã, mũ rơi, chuột trắng và animation bật/vồ có hướng rõ ràng. |
| IR4-17 | Đồ tể trở lại | Khi vào phòng không thấy đồ tể. Đi vào vùng kích hoạt giữa phòng, phía sau đảo bếp, đồ tể xuất hiện ở cửa vào và lập tức truy đuổi bằng NavMesh. Có thể chạy thoát khỏi phòng; không bắt xuyên vật cản. Khi ở đủ gần và không bị che chắn, Player bị khóa điều khiển, camera xoay mượt về mặt đồ tể rồi được nhấc lên. Tay trái giữ vị trí cổ Player, rig Neck nhìn theo camera nâng lên, sau đó màn hình cắt đen đột ngột và hồi sinh. |
| IR4-18 | Cutscene bàn mổ | Đầu bếp đồ tể đứng bất động cạnh chiếc bàn trống. Khi Player đến gần, khóa điều khiển và chuyển camera sang góc nhìn thứ nhất nằm trên bàn; đồ tể xoay cả người từ từ về phía camera trước khi phát Chopping. Màn hình chuyển đen ngay ở nhát dao đầu tiên rồi hồi sinh tại điểm bắt đầu demo. Không có pha né sau khi trigger. |

### Ý tưởng thay thế cho anomaly gây chết liên quan đến đồ tể

Các ý tưởng sau là lựa chọn dự phòng để thay cho một anomaly nhóm D sau khi bạn chọn; chúng chưa được tính vào danh sách chính 20 anomaly.

| Ý tưởng | Diễn biến và cách sống sót |
|---|---|
| Móc thịt quét ngang | Đồ tể giật mạnh đường ray treo móc; tiếng xích và bóng móc quét báo trước hướng nguy hiểm. Player né vào khoảng trống giữa các bàn trước khi móc quét qua lối đi. |
| Dao phay ném dọc hành lang | Đồ tể giơ dao, khóa hướng ném và báo trước bằng ánh phản quang trên lưỡi. Player né sang làn bên cạnh; dao chỉ bay qua một đường cố định, không tự đổi hướng. |
| Xe thịt bị đẩy lao tới | Đồ tể đẩy xe chở thịt lao dọc một lối; bánh xe rung và bóng xe hiện rõ trước khi chạy. Player lách vào hốc tránh cạnh lối đi. |
| Bàn chặt tạo sóng va đập | Đồ tể giáng dao xuống bàn chặt, tạo một đợt chấn động chạy dọc sàn. Vệt bụi hoặc gợn nước báo trước; Player nhảy lên bệ thấp hoặc núp sau đảo bếp để tránh. |
| Móc kéo khỏi sau quầy | Đồ tể thò móc từ sau quầy để kéo Player về phía mình; tay và móc lộ ra trước một nhịp. Player cắt ngang sau vật chắn hoặc rời khỏi hành lang kéo trước khi bị tóm. |

## Kế hoạch tài sản

### Tái sử dụng

Ưu tiên các prefab và vật thể đang có trong scene IR4: bếp, tủ lạnh, bồn rửa, đảo sơ chế, xe đẩy, dụng cụ, dao, lọ gia vị, đĩa và chuông phục vụ. Tách các bộ phận có thể chuyển động như cửa, núm bếp, nắp lọ, đĩa trên cùng và cloche. Các vật lặp dùng chung prefab; gom các bộ phận thuộc cùng một prop dưới một root để hierarchy dễ quản lý.

### Tạo mới

- Một nhân vật đồ tể dùng chung: thân hình to béo, tạp dề bẩn, mặt nạ da heo khâu vá có lỗ thở và dao phay. Giữ chuỗi hành động lặp hiện có; bổ sung điều khiển đầu để nhìn theo Player cho IR4-07, animation truy đuổi cho IR4-17 và animation chặt đồng bộ cutscene cho IR4-18.
- Một bộ bộ phận cơ thể nạn nhân dùng lại cho nồi, tủ lạnh, móc và bàn sơ chế; tạo texture/decal máu cho dao, vệt bẩn và mã vòng tay khách.
- Các prop cục bộ: cụm răng trong bồn rửa, heo sống thay thịt heo, đầu heo trên bàn và đầu người trong lò, nội thất/bộ phận cơ thể trong tủ lạnh, hiệu ứng rung tủ và âm thanh người bị nhốt, hiệu ứng lửa bếp, đàn chuột/ống thông gió, mũ đầu bếp và chuột trắng.
- Ưu tiên decal, material và animation ngắn cho chi tiết nhỏ thay vì tạo model riêng cho từng biến thể.

Đã chọn hướng **gore trực diện** cho phần trình bày kinh dị. Trước khi làm trọn bộ nhân vật và bộ phận cơ thể, nên duyệt một lát cắt hình ảnh gồm đồ tể và IR4-11 (các bộ phận người bên trong tủ lạnh). Không tạo model bằng Meshy/Tripo hoặc tiêu credit nếu chưa có yêu cầu và chấp thuận riêng.

## Tích hợp và nhịp độ triển khai

1. Tạo 20 `AnomalyData` với ID ổn định; IR4-14 được gộp vào IR4-12, không tạo asset riêng. Chỉ IR4-15 đến IR4-18 bật `Can Kill Player`; IR4-20/21 không gây sát thương.
2. Giữ runtime anomaly theo kiến trúc hiện có (`AnomalyBehaviour`, `AnomalyData` và death API dùng chung); tránh tạo hệ thống riêng cho từng anomaly.
3. Demo có F2 = phòng bình thường, F3 = chuyển lần lượt IR4-01 → IR4-02 → IR4-03 → IR4-07 → IR4-05 → IR4-19 → IR4-09 → IR4-11 → IR4-13 → IR4-04 → IR4-06 → IR4-08 → IR4-12. Dùng lại `LivingRoomAnomalyPreview` cùng các implementation của `AnomalyBehaviour`, với reference Player/camera và các vật thể gán trực tiếp. Chưa tích hợp lựa chọn anomaly hoặc chuyển tầng của campaign.
4. Triển khai theo đợt: IR4-01–09; sau khi kiểm tra độ dễ nhận ra, làm IR4-10–13, IR4-19 và IR4-20; cuối cùng mới làm IR4-15–18 và tinh chỉnh va chạm/né tránh.
5. Giữ `Content Ready = false` cho đến khi kiểm tra xong gameplay; đây là scene demo, chưa tự đưa nội dung vào campaign.

## Tiêu chí nghiệm thu

- Bốn anomaly nhỏ có thể nhận ra khi quan sát có chủ đích ở khoảng 0,8–1,2 m trong ánh sáng gameplay; không đòi hỏi soi từng pixel.
- Mỗi anomaly gây chết có dấu hiệu báo trước, vùng nguy hiểm và hành động né có thể hiểu được; không đánh xuyên vật cản hoặc giết người chơi trong fade/blackout.
- Ba anomaly nhóm D có đường sống sót bằng gameplay: chạy khỏi đàn chuột trong ống thông gió, né cú vồ chuột trắng và chạy thoát cuộc truy đuổi. IR4-18 là cutscene tử vong sau khi Player bước vào vùng kích hoạt.
- Khi anomaly kết thúc, reset hoặc đổi trạng thái, toàn bộ actor, collider, âm thanh, material và override camera được dọn dẹp; không để trạng thái dính sang lượt sau.
- Playtest xác nhận hướng mũi tên vẫn áp dụng đúng: anomaly thì đi ngược, phòng bình thường thì đi theo.

## Trạng thái triển khai demo

- Đã gán IR4-01, IR4-02, IR4-03, IR4-07, IR4-05, IR4-19, IR4-09, IR4-11, IR4-13, IR4-04, IR4-06, IR4-08, IR4-12, IR4-18 và IR4-17 trong `InfinityRoom4_HotelKitchen_Demo`, dưới root `IR4_Anomalies`.
- Mười chín `AnomalyData` nằm tại `Assets/Game/ScriptableObjects/Anomalies/InfinityRoom4`; IR4-15, IR4-16, IR4-17 và IR4-18 gây chết Player, mười lăm anomaly còn lại không gây chết.
- IR4-21 là mục F3 thứ 19 tại `IR4_Anomalies/IR4_21_VentFace`, component `KitchenVentFaceAnomaly`. Camera nhìn trong góc 18 độ, không bị vật cản và liên tục 0,12 giây sẽ kích hoạt rút một lần; đầu rút thẳng lên theo trục Y thế giới, mặc định 0,8 m trong 0,4 giây rồi ẩn. Giữ trường serialized `Retreat Offset` để bảo toàn scene: độ dài vector là khoảng rút, hướng vector cũ bị bỏ qua; giá trị cũ (0,8; 0; 0) tự có nghĩa rút lên 0,8 m. Không thay setup người dùng đã chỉnh trong scene. Dùng helper `AnomalyGazeVisibility` hiện có. F2/đổi anomaly/disable khôi phục vị trí, ẩn mặt/nan che bổ sung, reset thời gian nhìn. Tool setup/preview tạm đã xóa. Chưa chạy PlayMode; scene lưu thủ công.
- IR4-20 đã gán tại `IR4_Anomalies/IR4_20_CockroachSwarm`, là mục F3 thứ 16. `KitchenCockroachSwarmAnomaly` điều khiển 24 instance dùng chung `Prefabs/InfinityRoom4Demo/PF_IR4_CockroachPlaceholder.prefab`: khối đen dẹt không có collider, kích thước khoảng 0,075 × 0,018 × 0,14 m. Player đến gần giàn nồi chảo bên trái trong 2 m sẽ kích hoạt một lần; 16 con leo lên khe trần, 8 con xuống khe tủ, chạy theo đường gấp khúc trên bề mặt với thời điểm/tốc độ lệch nhau. Chạy hết đường thì ẩn; rời vùng không dừng đàn gián. F2/đổi anomaly/disable ẩn toàn bộ và đặt lại tiến trình. Inspector có `Activation Distance`, `Crawl Speed`, mảng `Insects` và `Routes`; các node `EscapeRoutes/Route_XX/Point_X` cho phép chỉnh đường bò bằng tay. Chưa có model gián thật; chưa chạy PlayMode test, người dùng tự kiểm thử. Tool setup tạm đã được xóa; scene cần lưu thủ công.
- Normal, IR4-07, IR4-18 và các anomaly khác dùng model gốc `IR4_Equipment/plump_chef_3d_model`, gồm hai dao BloodKnife/NoBloodKnife và controller normal hiện có. Chỉ IR4-17 dùng `pig_chef_3d_model`: model heo mặc định tắt; khi kích hoạt IR4-17 thì ẩn model gốc, model heo chỉ xuất hiện khi Player chạm MidRoomTrigger. Reset/F2/hồi sinh tắt model heo và hiện lại model gốc. Model heo có bản sao dao riêng cho truy đuổi, không đổi reference dao của IR4-04. Agent, rig Neck và `KitchenButcherActor` của IR4-17 vẫn gắn trên model heo. Không xóa asset hoặc sửa clip animation nguồn.
- IR4-17 là mục F3 thứ 15, component `KitchenButcherChaseAnomaly` tại `IR4_Anomalies/IR4_17_ButcherChase`. `MidRoomTrigger` đặt (0; 1,1; 1,4), kích thước (5; 2,2; 1,4) bao phủ lối đi giữa phòng phía bắc đảo; `EntranceSpawn` đặt (0; 0; -4,5). `KitchenButcherActor` trên pig chef điều khiển Run/Grab, agent và Neck. Mặc định chạy 3,8 m/s, bắt trong 0,95 m kèm line-of-sight, cập nhật đích 0,2 giây/lần. Camera xoay 0,35 giây rồi nhấc trong 1,2 giây, cao hơn mặt 0,45 m; chỉ mô phỏng nhấc bằng camera góc nhìn thứ nhất, không đẩy capsule Player vào trần. Grab dùng pose Idle và IK tay trái; không có clip nhấc cổ riêng. Neck được chỉnh sau Animator. Rời `RoomVolume` khi truy đuổi thì thoát; đã bị tóm thì không thể né tiếp. Chết dùng freeze 0 để cắt đen ngay, reset/hồi sinh dùng death controller hiện có. F2/đổi anomaly khôi phục normal và dọn agent, camera, input, IK.
- Đã bake NavMesh từ collider phòng vào `Models/InfinityRoom4Demo/Navigation/IR4_ChaseNavMesh.asset`; Player được đánh dấu không tham gia bake. Kiểm tra đường đi từ cửa tới hai phía đảo đều `PathComplete`. Không thay pipeline/project settings và không tự lưu scene. Chưa chạy PlayMode test.
- IR4-18 dùng `KitchenTableCutsceneAnomaly` tại `IR4_Anomalies/IR4_18_TableCutscene`, là mục F3 thứ 14. Khi kích hoạt, ẩn đồ trên bàn và thay chuỗi hành động thường bằng Idle (`IDLE2`) đang có, phát lặp ở tốc độ 1 bằng controller riêng `Animation/IR4_18_TableCutscene.controller`; không sửa `Chev.controller` hoặc clip `Chopping.anim` của người dùng. Player đến trong 1,6 m sẽ bị khóa điều khiển, màn hình đen lập tức và camera chuyển ngay sang `TableCameraAnchor` dưới lớp đen, sau đó fade sáng trong 1 giây (`Capture Fade Seconds`). Khi sáng hoàn toàn, đồ tể quay từ từ trong 1,2 giây rồi phát Chopping. Đen ngay tại thời gian clip 16/24 giây (khung 16 ở 24 FPS, có thể chỉnh `Impact Clip Seconds`), dùng `PlayerDeathController.TryBeginFreezeDeath` với freeze bằng 0. `CaptureFade` là overlay riêng, không tranh quyền với overlay của death controller. F2/đổi anomaly/disable dọn fade và khôi phục camera, input, đồ trên bàn, vị trí và Animator thường; khi đã bàn giao cho death controller thì không tranh quyền camera/input. Chưa chạy PlayMode test; người dùng tự kiểm thử.
- IR4-12 dùng `LivingRoomArrangementAnomaly` tại `IR4_12_PigHeadAndHumanBody`: `PigHead` và `HumanBody` cùng bật, `DeadPig_LP` tắt. Phòng bình thường/F2/đổi anomaly khôi phục ngược lại. IR4-14 không có lượt F3 riêng; vị trí, animation và nội dung model do người dùng đặt được giữ nguyên.
- IR4-08 dùng `LivingRoomArrangementAnomaly` tại `IR4_08_LivingPig`, bật `Pig_White` và tắt `DeadPig_LP` khi kích hoạt; F2/đổi anomaly khôi phục heo chết. Không thêm controller, thay animation hoặc di chuyển hai object.
- IR4-06 dùng `KitchenFallingTeethAnomaly` tại `IR4_06_FallingTeeth`: 48 instance liên kết cùng `Teeth.prefab`, chia `LeftBasin`/`RightBasin`, 24 mỗi bồn. Tận dụng Rigidbody/CapsuleCollider có sẵn, khối lượng 0,01 kg, CCD Continuous Speculative và interpolation; nguồn prefab không bị ghi đè. Các pose thả ban đầu khác nhau được đặt sẵn bằng seed 406, không phải torque hay chuyển động được script điều khiển. Runtime chỉ bật gravity và thả từ vận tốc bằng 0. Dùng MeshCollider sẵn có của bồn, không thêm mặt sàn giả che lòng bồn. Mẫu Teeth trong scene được gom vào nhóm anomaly để không còn răng lẻ ở trạng thái normal.
- IR4-04 dùng `LivingRoomArrangementAnomaly` tại `IR4_04_CleanKnife`, đổi trạng thái hai object `NoBloodKnife`/`BloodKnife` đã gắn tay phải đồ tể. Không thay model, material, transform hoặc animation. Bình thường dùng dao dính máu, anomaly dùng dao không dính máu.
- IR4-02 đặt scale X của `IR4_Sink` thành `-1` và khôi phục khi reset.
- IR4-03 dời xe đẩy đồ ăn `IR4_Detail_4_ServiceCart`; giữ reference và vị trí anomaly do người dùng chỉnh trong Inspector.
- IR4-07 điều khiển bone `mixamorig:Head` sau Animator, chỉ khi Player nằm trong vùng 180° phía trước đồ tể; hướng xét theo thân đồ tể, không theo object anomaly. Chuỗi hành động cơ thể vẫn do Animator hiện có điều khiển. Reference Player/camera được serialize để giữ sau reload.
- IR4-05: đến trong khoảng 3 m quanh `IR4_Range` sẽ bật sáu họng lửa bằng Particle System và sáu đèn hắt không đổ bóng; lửa tiếp tục cháy đến khi F2/đổi anomaly. Không có collider gây sát thương.
- IR4-19: đến trong khoảng 2,5 m quanh `IR4_Fridge` sẽ rung nhẹ tủ, phát tiếng thở đau đớn lọc nghẹt và tiếng đập bên trong. Rời vùng sẽ dừng âm thanh, khôi phục pose; cửa được đóng và chặn tương tác trong anomaly, mở lại quyền tương tác khi reset.
- IR4-09: đến gần bồn trong 2,8 m sẽ bắt đầu dòng máu từ miệng vòi vào khoang bồn bên phải, dâng trong 10 giây, tràn qua mép trước rồi loang trên sàn trong 8 giây tiếp theo. Dòng vòi ngắn dần theo mực máu, có hạt bắn ở điểm chạm và mép tràn cong. Máu tiếp tục chảy khi Player rời vùng; F2/đổi anomaly dọn sạch toàn bộ. Không gây sát thương, không phủ kín cả phòng.
- Các anomaly còn lại trong danh sách vẫn ở mức thiết kế. Scene được người dùng lưu thủ công.
- IR4-11: `KitchenFridgeOpeningAnomaly` dùng trực tiếp `FridgeDoorLeft` và `FridgeDoorRight`, kích hoạt một lần trong khoảng 1,8 m và mở trong 0,16 giây. Giữ cửa đóng trước trigger và mở sau trigger đến khi reset. F2 khôi phục trạng thái cửa và quyền tương tác bình thường; không điều khiển nội dung tủ. Chỉnh khoảng cách/tốc độ ở `IR4_11_FridgeOpens` trong Inspector.
- IR4-13: `KitchenOvenOpeningAnomaly` điều khiển `OvenDoorLeft` và `LeftOvenInterior/Rack_1`, không đổi lò phải. Trong khoảng 1,8 m, cửa mở trong 0,18 giây rồi khay trượt theo local Z âm 0,52 m trong 0,7 giây. `IR4_13_SoupPot` là con của Rack_1 (không đặt dưới Tray có scale dẹt), chứa nồi rỗng, hai quai, mặt súp đỏ và `Contents/Head`, `Contents/Bone`. Đây là object `Head` có sẵn trong scene, tương ứng tên “Had” trong yêu cầu. Hai phần được căn và thu nhỏ theo kích thước nồi; giữ material gốc.
- Súp có opacity 0,58 và gợn động; bọt sôi và hơi nước bắt đầu sau khi cửa mở. F2/đổi anomaly đưa khay và cửa về bình thường, ẩn nồi/nội dung và dừng, xóa particles, kể cả khi đang chuyển động. Các object khác do người dùng thêm vào Tray không bị bật/tắt. Chỉnh khoảng cách, thời gian mở/trượt và `Tray Local Offset` tại `IR4_13_OvenOpens`; độ trong chỉnh ở material `Art/InfinityRoom4/IR4_13_RedSoup.mat` → Opacity.

### Triển khai IR4-15 — Đàn chuột từ IR4_Hood

- Pha Warning 0,8 giây nay rung trực tiếp `IR4_Hood` để báo hiệu trước khi chuột xuất hiện. `Warning Hood` được gán trong scene; Inspector có `Warning Shake Distance` (mặc định 0,015 m) và `Warning Shake Degrees` (1,2 độ). Hết Warning trả về pose gốc, sau đó đàn chuột mới chạy. F2/reset/disable giữa lúc rung cũng khôi phục pose, không cộng dồn transform. Tool bind tạm đã xóa, scene lưu thủ công; chưa chạy PlayMode.

- Điều chỉnh mật độ: giữ 40 con từ Rat người dùng, giãn bề rộng tuyến thành 0,84 m (trước khoảng 0,45 m), `Spawn Interval` = 0,035 giây để đàn tuồn ra trong khoảng 1,4 giây. Các route miệng ống/mặt bếp/sàn được giãn đồng bộ. Giữ pool dựng sẵn, không Instantiate/Destroy trong gameplay. Scene instance Animator dùng `CullCompletely`, SkinnedMeshRenderer tắt `updateWhenOffscreen` và tắt cast shadow riêng đàn chuột; model nguồn và các anomaly khác giữ nguyên. Đếm mesh cho 40 con là 1.254.320 tam giác nếu toàn đàn được render. Chưa có profiler PlayMode/build nên không khẳng định FPS hoặc mức cải thiện; chưa tăng số lượng để tránh nhân thêm chi phí skinning/render. Tool authoring tạm đã xóa; scene lưu thủ công. Các giá trị này thay thế interval/lane cũ bên dưới.

- Hướng thân chuột khi rơi/nhảy bám theo tiếp tuyến chuyển động 3D: đầu chúi xuống khi lao từ miệng ống, thay vì chỉ xoay ngang theo mặt sàn. Khi xuất hiện trong ống đã đặt hướng đầu xuống; khi tiếp đất chuyển dần về hướng chạy ngang, chỉnh tốc độ quay bằng `Turn Speed` (mặc định 720 độ/giây). Regression test bổ sung kiểm tra hướng đầu khi rơi thẳng và lao chéo; chưa chạy PlayMode.

- `IR4_Anomalies/IR4_15_HoodRatSwarm` là mục F3 thứ 18. Có 40 instance chung `Prefabs/InfinityRoom4Demo/PF_IR4_SwarmRat_FromScene.prefab`, tạo từ đúng object `Rat` do người dùng đặt trong scene, giữ model/material và scale 0,6 của mẫu. Object `Rat` gốc được giữ làm mẫu và ẩn trong scene. Dùng bản sao clip `Mammals_run_A1` phát lặp, root motion tắt; không đổi SpecialRat của IR4-16.
- Tuyến đã sửa: từ trong miệng hút `IR4_Hood` (Y khoảng 2,43 m), lộ ra dưới miệng (Y 2,28 m), rơi lên mặt bếp (Y 1,415 m), chạy tới mép bếp rồi nhảy xuống sàn (Y 0,015 m), chạy chụm qua lối đông đảo về cửa. Chặng rơi dùng tiến độ gia tốc trọng trường và chặng nhảy có cung nhô lên; đây là quỹ đạo trình bày có kiểm soát. Các con lệch hẹp quanh tuyến, tốc độ chỉ chênh nhỏ; `Spawn Interval` = 0,025 giây, 40 con tuồn ra trong khoảng 1 giây để tạo dòng lũ dày đặc. F2/reset đặt lại cả thời gian rơi/nhảy và vị trí.
- Chạm `HoodApproachTrigger` ở khu bếp phải sẽ chờ 0,8 giây, rồi đàn chuột lần lượt tràn từ mặt dưới `IR4_Hood`, xuống lối đi phía đông đảo, vòng phía nam và chạy ra cửa vào. Từng con lệch tuyến, tốc độ và thời điểm xuất hiện. `Rats`/`EscapeRoutes` được nhóm dưới root anomaly; có thể kéo các `Point_X` để chỉnh tuyến. `Run Speed` mặc định 2,8 m/s, `Contact Radius` 0,52 m gồm bề rộng Player và chuột. Kiểm tra va chạm theo đoạn di chuyển, chỉ bắt ở gần mặt sàn, không bắt khi chuột còn ở trên cao. Né hết đàn chuột thì sống; đàn chạy hết sẽ ẩn.
- Khi chạm chuột: khóa điều khiển, camera đổ xuống cao 0,16 m trên sàn trong 0,65 giây, nghiêng 72 độ. Tám con trong pool được dùng cho cảnh chạy ngang trước camera trong 1,5 giây với rung mạnh, sau đó cắt đen ngay bằng `TryBeginFreezeDeath(..., 0f)` và hồi sinh. F2/đổi anomaly/disable dọn đàn chuột, reset tuyến, khôi phục camera/cinematic lock. Code Policy PASS; chưa chạy PlayMode, người dùng tự kiểm thử; tool setup đã xóa và scene cần lưu thủ công.

### Triển khai IR4-16 — Chuột trắng

- Pose bám mặt cập nhật: thân dựng dọc theo camera, bụng quay vào mặt Player; bốn chân duỗi theo bốn góc chữ X trên mặt phẳng camera, không quặp ra sau hoặc chĩa theo chiều sâu vào camera. Nhóm bone cẳng chân được căn thẳng, bàn chân/ngón đi theo đầu mút; đầu–mũi xoay riêng để nhìn camera. Blend pose trong cú nhảy, khôi phục khi reset. Đã xem preview model thật tại `IR4_16_ClingPosePreview.png`; regression test kiểm tra hướng bụng, bốn hướng chân và cleanup. Chưa chạy PlayMode.

- Khi SpecialRat nhảy bám mặt, `KitchenRatClingPose` cache rig Mammals một lần, tạm dừng Animator và dang bốn nhóm chân (Fore/Leg) theo bốn góc của mặt phẳng camera. Pose blend theo tiến trình nhảy; đầu `Head_Head.001` xoay để trục đầu–mũi hướng vào vị trí camera, giữ trong lúc rung lắc. F2/đổi anomaly/reset khôi phục pose rig và trạng thái enabled của Animator rồi mới rebind, không sửa clip Idle/Walk nguồn. Độ cao điểm bám người dùng đã chỉnh (-0,15 m so với camera) được giữ nguyên. Có regression test pose/cleanup; chưa chạy PlayMode.

- Model normal `plump_chef_3d_model` được tách trong scene thành `PlumpChef_Hat` và `PlumpChef_Body`, asset mesh tại `Models/InfinityRoom4Demo/PlumpChef_Hat.asset` và `PlumpChef_Body.asset`. Mũ có 721 tam giác, thân có 22.920 tam giác, tổng khớp 23.641 tam giác của nguồn. Giữ UV/material, bindpose và bone weights; renderer nguồn `tripo_node_be7cb2ee` tắt để có thể khôi phục. Mesh gốc là bề mặt liền: phần tách theo mép mũ hiện có, chưa tạo thêm đỉnh đầu hoặc mặt bịt bên trong mũ. Tool tách/preview tạm đã xóa; không sửa FBX/prefab nguồn và không tự lưu scene.

- Model `dirty_chef_3d_model_1` trong scene dùng `IR4_16_MergedBody`: gộp 12 SkinnedMeshRenderer `tripo_part_0`–`tripo_part_12`, loại trừ `tripo_part_7` (mũ). Mesh lưu tại `Models/InfinityRoom4Demo/IR4_16_MergedChefBody.asset`, 14.219 vertex, 65 bone; giữ 12 submesh/material gốc. Renderer của các phần thân nguồn tắt, object/bone vẫn giữ để bảo toàn rig và có thể khôi phục. Mũ và cơ chế lăn độc lập giữ riêng. Kiểm tra bake ở pose Idle/Death có sai lệch vertex nhỏ hơn 0,000001 m so với các phần gốc; tool gộp/kiểm tra tạm đã xóa.

- Sửa xung đột Root Motion của IR4-16: bỏ `GroundFall` vốn ghi đè vị trí mỗi LateUpdate. Idle tắt Root Motion để đứng tại chỗ; Death bật Root Motion để Animator điều khiển vị trí và hướng cú ngã, hết clip giữ pose bằng speed = 0 trước bước lăn mũ. F2/reset khôi phục pose, vị trí/hướng root, tốc độ và thiết lập Root Motion ban đầu. Inspector hiện gán chef là `plump_chef_3d_model (1)`, mũ là `PlumpChef_Hat`; giữ nguyên reference người dùng đã đổi. Migration: trường float `groundedDeathOffset` cũ được bỏ, giá trị serialized -0,8 cũ không còn được đọc và sẽ được Unity loại khi người dùng lưu scene; không chuyển giá trị này sang Root Motion. Không sửa clip nguồn. `Floor Height` chỉ dùng cho mũ/chuột.
- Mũ là SkinnedMeshRenderer: chụp mesh tại lúc ngã xong, ẩn mũ gốc và hiện `IR4_16_RollingHat` độc lập để lăn; F2 khôi phục mũ gốc và ẩn bản sao. Hướng chuột đi ra lấy từ hông tới đầu ở pose nằm. Có regression test cho hướng thoát và lifecycle Root Motion/reset; chưa chạy PlayMode.

- `IR4_Anomalies/IR4_16_WhiteRat` là mục F3 thứ 17, dùng `dirty_chef_3d_model_1`; normal vẫn dùng `plump_chef_3d_model`. `ApproachTrigger` có thể di chuyển/đổi kích thước trong scene. Khi kích hoạt anomaly, đồ tể dirty đứng yên với `Chev_Idle`; chạm vùng trigger phát `Chev_Death`. Chờ state Death kết thúc mới lăn `tripo_part_7` trong 1 giây, rồi hiện `SpecialRat` từ bone `mixamorig:Head`.
- Chuột bò ra 0,6 m trong 2 giây với `Mammals_walk_A1`, sau đó phát lặp `Mammals_idle_A3`. Khi đang Idle, nếu Player đến trong `Attack Distance` (mặc định 2,4 m), chuột nhảy vào camera trong 0,45 giây, rung camera mạnh 1,2 giây rồi dùng death API freeze bằng 0 để cắt đen/hồi sinh. Đứng ngoài khoảng cách này chưa kích hoạt cú nhảy; khi cú nhảy đã bắt đầu thì không né được.
- Controller/clip sao chép riêng tại `Animation/IR4_16_*`; không sửa clip nguồn. F2/đổi anomaly/disable khôi phục model, mũ, chuột, camera và cinematic lock. Tool setup tạm đã xóa, scene lưu thủ công. Chưa chạy PlayMode, người dùng tự kiểm thử.

### Model và animation IR4-20

- Thay 24 khối đen tạm bằng instance liên kết `Prefabs/InfinityRoom4Demo/roach.prefab`, giữ các root `Cockroach_XX` và đường bò hiện có. Model được căn chân sát bề mặt, chiều dài khoảng 0,18 m; collider bị vô hiệu để không cản Player.
- Controller riêng `Animation/IR4_20_Roach.controller`: `Idle` giữ tư thế nghỉ của prefab (clip tạo từ pose rig hiện tại), `Run` dùng bản sao clip `ModelV2/3dpea.com_cuban_cockroach/cockroach/source/Roach/Run.anim`, phát lặp. Mỗi con chuyển sang Run khi thực sự bắt đầu bò sau độ trễ riêng; root motion tắt vì anomaly điều khiển đường bò. F2/đổi anomaly ẩn và reset; kích hoạt lại bắt đầu ở Idle. Prefab nguồn và clip Run nguồn được giữ nguyên.

### Asset nồi súp IR4-13

- Nồi rỗng và quai được dựng bằng mesh xoay tròn, khoảng 0,48 m đường kính × 0,17 m chiều cao, vừa khoang lò dưới. Mesh tại `Models/InfinityRoom4Demo/SoupPot`; material thép, súp, bọt và hơi tại `Art/InfinityRoom4/IR4_13_*`.
- Mask hơi nước 64 × 64 và mesh nồi được tạo nội bộ ngày 2026-10-22; không dùng Meshy/Tripo hay tải asset ngoài. `Head` và `Bone` tái sử dụng từ scene, giữ provenance/quyền sử dụng nguồn của dự án.
- Shader `KitchenFlowingBloodURP` được mở rộng Opacity/Blend/Depth Write với mặc định opaque như cũ, để các material IR4-09 không thay đổi hành vi. Chỉ súp IR4-13 dùng alpha blending và không ghi depth.

### Tài sản VFX/âm thanh của IR4-05 và IR4-19

- `Assets/Game/Art/InfinityRoom4/IR4_FlameMask.png`: mask alpha 64 × 128, dựng bằng công thức hình giọt lửa tại Editor (2026-10-22), không dùng ảnh/model bên ngoài. Material `IR4_BurnerFlame.mat` dùng shader `Universal Render Pipeline/Particles/Unlit`, hòa trộn additive, không ghi depth.
- `Assets/Game/Art/InfinityRoom4/IR4_FridgeKnocks.wav`: tiếng đập tổng hợp xác định bằng seed 419, mono 22.050 Hz, vòng 4 giây, tạo riêng cho dự án; không tải mẫu bên ngoài.
- Tiếng người bên trong tái sử dụng clip `Assets/Game/Audio/sspsurvival-frequent-breathing-painful-breathing-the-sound-of-breathing-a-person-is-breathing-frequently-15255.mp3` đã có trong dự án, giảm pitch và lọc low-pass 650 Hz. Đây là tiếng thở/đau nghẹt, không phải bản thu lời kêu cứu mới; giữ hồ sơ quyền sử dụng gốc của clip khi phát hành.

### VFX dòng máu IR4-09

- Dựng riêng bằng mesh, Particle System và `Shaders/KitchenFlowingBloodURP.shader` trên URP 17 đang có; không cài package hoặc đổi pipeline/project settings. Đây là VFX dòng chảy định hướng, **không phải mô phỏng chất lỏng thể tích** và không tự tìm đường quanh vật cản mới.
- Dùng PBR của URP cho ánh sáng, specular và phản chiếu; shader tạo chuyển động gợn/chi tiết dòng chảy theo thời gian, không cấp phát mesh mỗi frame. [Tài liệu Unity: chiếu sáng trong shader URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/use-built-in-shader-methods-lighting.html).
- Mesh bồn, dòng vòi, mép tràn và vũng có viền không đều được tạo nội bộ tại `Models/InfinityRoom4Demo/BloodFlow`; material và phản chiếu cục bộ nằm trong `Art/InfinityRoom4/IR4_09_*`. Không tải texture nước chưa rõ bản quyền; asset hình học và shader được tạo riêng cho dự án ngày 2026-10-22.
- Inspector trên `IR4_09_BloodOverflow` cho phép chỉnh `Activation Distance`, `Fill Seconds`, `Spread Seconds` và `Fill Height`. Các Transform con là mốc chỉnh vị trí vòi, mặt bồn, mép tràn và vũng; di chuyển cả node cùng bồn nếu bố cục đổi.

## Giả định

- Giữ footprint phòng hiện tại khoảng 8 × 10 m và cửa phía nam; các thời gian/khoảng cách nêu trên là giá trị khởi đầu để playtest, không phải thông số khóa cứng.
- Bộ 20 là danh mục lựa chọn sau khi gộp IR4-14 vào IR4-12 và thêm IR4-20/21; thứ tự nhóm thể hiện mức độ kinh dị tăng dần chứ không ép thứ tự xuất hiện.
- Tài liệu ghi cả kế hoạch thiết kế và phạm vi demo đã triển khai ở mục trên; chưa đưa nội dung vào campaign.
