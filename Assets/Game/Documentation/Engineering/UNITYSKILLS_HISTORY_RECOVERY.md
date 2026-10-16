# UnitySkills history recovery — 2026-10-03

## Sự cố

Unity crash khi mở project, trước khi có thể dùng REST. Hộp thoại báo `Size overflow in allocator`, stack tại `JsonUtility.FromJson<WorkflowHistoryData>` → `WorkflowManager.TryLoadHistoryFrom` → `LoadHistory` → `TabController.RefreshHistory`.

Sau đợt authoring demo Room3, file lịch sử UnitySkills là 760.021.945 byte; bản backup là 760.021.567 byte. Plugin đọc và deserialize toàn bộ file trước khi trim history. Không xác nhận file bị sai cú pháp JSON; vấn đề đã quan sát là kích thước quá lớn khiến deserializer native crash. Không dùng `ConvertFrom-Json`/`JsonUtility` để thử đọc lại toàn bộ file này.

## Phục hồi đã thực hiện

Sau khi người dùng xác nhận đã đóng Editor, kiểm tra không còn process Unity, rồi chuyển đúng các mục sau bằng thao tác Move, không xóa:

```text
D:\du an\Zero1\Library\UnitySkills\
├── workflow_history.json
├── workflow_history.json.bak
└── workflow_files\
```

Đích cách ly:

```text
D:\du an\Zero1\Library\UnitySkills\QuarantinedWorkflow_20261003_142543\
```

Giữ cả `workflow_files` để snapshot cũ không bị auto-clean của một history mới, rỗng, thu hồi. Các file khác của UnitySkills được giữ nguyên. Không sửa package, project settings, scene, prefab hoặc code gameplay trong bước phục hồi. Không xóa Library, không gọi Scene Builder.

## Mở lại và kiểm tra

1. Mở project bằng Unity Hub.
2. Kiểm tra Editor đã vào được project và không còn hộp thoại allocator.
3. Không tự chép history/backup 760 MB trở lại đường dẫn cũ; làm vậy có thể gây crash lại. Lịch sử Undo UnitySkills mới sẽ bắt đầu riêng. Các snapshot cũ vẫn được giữ trong thư mục cách ly, nhưng chưa được xác nhận có thể phục hồi từng task an toàn.
4. Kiểm tra Room3 đang có bản geometry nào trước khi làm tiếp. Geometry chưa lưu ở phiên crash có thể đã mất; lịch sử backup không tương đương một scene đã được save.

Đã xác nhận hai file lỗi không còn tại vị trí startup; snapshot vẫn nằm trong thư mục backup. Việc Editor mở lại thành công cần được kiểm chứng sau khi người dùng mở project.

## Trước khi tiếp tục authoring

- Không gửi thêm chuỗi thao tác dựng lớn cho tới khi đã kiểm tra Editor ổn định.
- Dùng nhóm thao tác nhỏ, đợi mỗi nhóm hoàn tất trước khi gửi nhóm tiếp; kiểm tra mesh vertex count trước khi combine.
- Theo dõi kích thước history và thời gian persistence sau mỗi nhóm; dừng nếu tăng bất thường. Không tin rằng ngưỡng auto-clean sau deserialize sẽ bảo vệ được startup.
- Không tiếp tục dùng cách tạo rất nhiều mesh nhỏ rồi snapshot/combine hàng loạt như đợt vừa rồi; ưu tiên module nhẹ hoặc asset có sẵn.
- Chỉ lưu scene khi người dùng chủ động đồng ý/thực hiện; không thêm script tự rebuild hoặc save scene để phục hồi.
