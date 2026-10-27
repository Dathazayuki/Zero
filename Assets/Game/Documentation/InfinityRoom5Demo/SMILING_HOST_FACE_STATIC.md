# The Smiling Host — Face Static

Lớp nhiễu cho model `TheSmilingHost` trong scene Cabaret. Object con `FaceStatic`
là SkinnedMeshRenderer dùng lại bộ xương của model, nên lớp che đi theo chuyển động
đầu. Vùng che là một dải ngang qua mắt và sống mũi với các vệt nhiễu đen/xám chạy
ngang như ảnh tham chiếu. Tóc, miệng cười và cằm nằm ngoài dải che.

Object `FaceNoiseRibbon` nằm dưới bone `mixamorig:Head`, là mesh ngang cong riêng
nhô ra hai bên thái dương. Dải này dùng MeshRenderer và đi theo bone, không cần
script chạy mỗi frame. Nó được thu chiều ngang về 60% mesh gốc khi đặt trong scene.

## Asset

- Shader: `Assets/Game/Shaders/SmilingHostFaceStaticURP.shader`.
- Material: `Assets/Game/Art/InfinityRoom5/M_SmilingHostFaceStatic.mat`.
- Mesh: `Assets/Game/Art/InfinityRoom5/TheSmilingHost_FaceStatic.asset`.
- Mesh dải ngang: `Assets/Game/Art/InfinityRoom5/TheSmilingHost_NoiseRibbon.asset` (1.344 tam giác).
- Material dải ngang: `Assets/Game/Art/InfinityRoom5/M_SmilingHostNoiseRibbon.mat`.
- Nguồn mesh: `Assets/TripoModels/tuxedo_suit_3d_model/tuxedo_suit_3d_model.fbx`.
- Ảnh cận mặt: [TheSmilingHost_StaticPreview.png](TheSmilingHost_StaticPreview.png).

Shader URP tự chạy bằng thời gian GPU, không cần script Update hoặc texture mới.
UV channel 1 của mesh overlay lưu vị trí bind-pose trong không gian gốc của Host;
không dùng shader này trực tiếp trên mesh nhân vật chưa được chuẩn bị dữ liệu đó.
Model và texture nguồn không bị thay thế.

## Chỉnh trong Inspector

Chọn material `M_SmilingHostFaceStatic`:

- **Horizontal band center XY / half size XY**: X/Y là tâm, Z/W là nửa chiều rộng/cao
  của dải ngang, đơn vị theo model gốc. Mặc định `(0, 2.01, 0.16, 0.05)`.
  Hai trường mask cũ được giữ ẩn để bảo toàn material đã lưu và không còn điều khiển vùng che.
- **Front surface minimum Z**: giới hạn vùng phía trước đầu, tránh phủ ra phía sau.
- **Static changes per second**: tốc độ đổi nhiễu, hiện đặt 36 lần/giây.
- **Grain density**: mật độ hạt, hiện đặt 1100.
- **Dark grain brightness**: độ sáng phần nhiễu xám, hiện đặt 0.13.
- **Horizontal tearing**: độ xé ngang của các hàng nhiễu, hiện đặt 0.075.
- **Separate ribbon mesh**: 1 cho material dải ngang, 0 cho material phủ mặt;
  chỉ dải ngang dùng biến dạng vertex.
- Muốn dải nhô ra rộng/hẹp hơn: chỉnh **Local Scale X** của `FaceNoiseRibbon`.
  Material riêng của dải có **Horizontal band** nửa chiều rộng 0.24.
- **Coverage**: đặt 0 để ẩn material đó; đặt 1 để hiện. Muốn tắt toàn bộ hiệu ứng,
  tắt cả `FaceStatic` và `FaceNoiseRibbon` hoặc tắt root nhân vật.

Khi tái sử dụng nhân vật, sao chép cả root Host để giữ reference xương của overlay.
Không chỉ sao chép object `FaceStatic` sang một bộ xương khác.
Shader kiểm tra depth bình thường, nên bị tường/vật che khuất như nhân vật.
Ảnh preview được render bằng Unity ở Edit Mode; chưa kiểm tra animation trong Play Mode.
Scene cần được người dùng lưu thủ công sau khi gắn lớp che.
