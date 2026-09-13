# Room CorrectArrow và Anomaly

## Luật cốt lõi

Mỗi `FloorSegment` sở hữu một round độc lập. Trạng thái được chuẩn bị khi segment trở thành active và bị khóa cho đến lúc Player rời segment.

```text
Normal  -> đi theo CorrectArrow
Anomaly -> đi ngược CorrectArrow
```

| Room | CorrectArrow | Hướng đúng |
|---|---|---|
| Normal | Up | Up |
| Normal | Down | Down |
| Anomaly | Up | Down |
| Anomaly | Down | Up |

`CurrentTravelDirection` không còn thuộc luật gameplay.

## Vòng đời một round

```text
Segment được lấy từ pool
        -> Reset Room về baseline
        -> Random CorrectArrow 50/50
        -> Random Normal/Anomaly 45/55
        -> Khóa Arrow, anomaly và ExpectedDirection
        -> Player quan sát và chọn UP hoặc DOWN
        -> SegmentCommitTrigger chụp quyết định của Current cũ
        -> Dynamic pool thay cả hai segment liền kề
        -> Đúng: Number + 1 (tối đa 8)
        -> Sai: Number = 0
```

Target liền kề giữ nguyên round đã chuẩn bị khi trở thành Current; không được reroll ngay trước mặt Player.

## Dynamic pool

- Pool có ít nhất 9 `FloorSegment`.
- Luôn đúng năm segment active: `Below2`, `Below1`, `Current`, `Above1`, `Above2`.
- Segment còn lại inactive và ở Normal baseline.
- Không `Instantiate`, `Destroy`, reload scene hoặc teleport Player khi gameplay chạy.
- Chỉ `SegmentCommitTrigger` thực hiện transaction; hệ `SegmentRecycleTrigger` cũ đã bị loại bỏ.
- Mỗi commit chỉ giữ target liền kề; bốn segment active cũ còn lại được thay bằng bốn reserve đã lấy trước khi release.

## Floor Number

`Floor_Number` hiển thị tiến độ đúng liên tiếp từ 0 đến 8. Cả ba segment active luôn hiển thị cùng giá trị. Đúng tại 8 tiếp tục giữ 8; bất kỳ quyết định sai nào cũng làm tầng tiếp theo hiển thị 0.

## Anomaly content

Các anomaly nằm trong `Room/AnomalyContent`. Khi segment được trả về pool, Room reset anomaly, CorrectArrow, animator, audio, particle, rigidbody và props về baseline trước khi inactive.
