# Reveal Album — Tóm tắt thay đổi

## Luồng mới
Host bấm **Tiếp theo** để lần lượt mở từng "mảnh" của album hiện tại.  
Tất cả client đều thấy trạng thái giống host (qua Fusion networked state).  
Chỉ host có nút bấm; client khác thấy label "Chờ host mở...".

### Thứ tự reveal mỗi album
1. Prompt gốc (`link 0`) — hiện ngay khi album mở
2. Bức tranh đầu tiên (`link 1`) — host bấm Next lần 1
3. Bức tranh tiếp theo... (`link 2, 3, ...`) — mỗi lần bấm mở thêm 1
4. Final guess (`link cuối`) — host bấm Next lần cuối trong album
5. Label nút đổi thành **Album tiếp theo ▶** → host chuyển sang album kế
6. Sau album cuối: **Kết thúc** → gọi `AdvancePhase()` để thoát Reveal

---

## Files đã thay đổi

### `Assets/_Project/Scripts/Core/Gameplay/Phases/PhaseManager.cs`
| Thay đổi | Chi tiết |
|---|---|
| Thêm property | `[Networked] public byte RevealLinkIndex` — index link đang được reveal (0=prompt) |
| Thêm method | `SetRevealLinkIndex(byte)` — host set trực tiếp |
| Thêm method | `RevealNext()` — host UI gọi: advance link, hoặc chuyển album, hoặc AdvancePhase() |
| Update | `StartGame()` và `ResetForLobby()` đều reset `RevealLinkIndex = 0` |

### `Assets/_Project/Scripts/Core/Gameplay/Phases/Strategies/RevealPhase.cs`
| Thay đổi | Chi tiết |
|---|---|
| `OnEnter` | Gọi thêm `SetRevealLinkIndex(0)` |
| `Tick` | **Xóa** timer auto-advance. Reveal giờ do host điều khiển thủ công |

### `Assets/_Project/Scenes/TestScenes/Hoai/UI/RevealAlbumPanel.cs`
**Viết lại hoàn toàn.** Fields cũ (`LinkSlot[] drawingSlots`, `countdownLabel`) được thay bằng cấu trúc mới:

| Field (Inspector) | Mục đích |
|---|---|
| `titleLabel` | "Album của [tên người]" |
| `promptContainer` | GameObject bật/tắt (hiện ngay khi album mở) |
| `promptLabel` / `promptAuthorLabel` | Nội dung prompt + tên người viết |
| `drawingSlots[]` | Array `DrawingLinkSlot` (root + drawingImage + authorLabel), mỗi slot = 1 bức tranh |
| `guessContainer` | GameObject bật/tắt cho final guess |
| `finalGuessLabel` / `guessAuthorLabel` | Nội dung guess + tên người đoán |
| `hostControlsRoot` | Ẩn/hiện toàn bộ nút bấm của host |
| `nextButton` | Host bấm để reveal link tiếp theo |
| `nextButtonLabel` | "Tiếp theo ▶" / "Album tiếp theo ▶" / "Kết thúc" |
| `statusLabel` | Non-host thấy "Chờ host mở..." |

> **Lưu ý cần làm trong Unity Editor:** Scene cần được rebuild UI theo cấu trúc mới này và kéo các refs vào Inspector của RevealAlbumPanel.

---

## Files mới tạo

### `Assets/_Project/Scenes/TestScenes/Hoai/Mock/MockAlbumData.cs`
ScriptableObject. Tạo asset qua **Assets > Create > InkEcho > Mock Album Data**.

Cấu trúc mỗi `MockAlbum`:
- `ownerName` — tên chủ album
- `originalPrompt` — prompt gốc (link 0)
- `drawings[]` — mảng `MockDrawingEntry` (drawerName + Texture2D)
- `finalGuess` + `guesserName` — link cuối

### `Assets/_Project/Scenes/TestScenes/Hoai/Mock/MockRevealDriver.cs`
MonoBehaviour test độc lập — **không cần Fusion / server**.  
Kéo vào scene test, assign `MockAlbumData` và tất cả UI refs, bấm Play rồi click nút Next.

Fields giống `RevealAlbumPanel` (title, promptContainer, drawingSlots[], guessContainer, nextButton...).  
Khác biệt: đọc từ `MockAlbumData` thay vì `ServiceLocator<AlbumStore>`.

---

## Cách test offline (MockRevealDriver)
1. Tạo `MockAlbumData` asset, điền dữ liệu và kéo Texture2D cho drawings
2. Tạo một scene test mới (hoặc dùng scene có sẵn trong Hoai/)
3. Thiết kế UI giống layout của `RevealAlbumPanel`
4. Gắn `MockRevealDriver` vào GameObject, kéo tất cả refs + MockAlbumData
5. Play → bấm nút "Tiếp theo ▶" để test toàn bộ luồng reveal
