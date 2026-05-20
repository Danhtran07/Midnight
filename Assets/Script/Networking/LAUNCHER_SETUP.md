# Hướng dẫn setup Launcher + Photon PUN 2

## Nguyên tắc quan trọng

| Đúng | Sai |
|------|-----|
| **1** GameObject `NetworkManager` + `Launcher` **chỉ ở MainMenu** | Launcher ở mỗi scene |
| Play game **từ MainMenu** | Play trực tiếp CreateRoomScene / NightMap |
| `Launcher` dùng Singleton + `DontDestroyOnLoad` | Nhiều object gọi `ConnectUsingSettings()` |

---

## 1. Build Settings

**File → Build Settings → Scenes In Build** (đúng thứ tự):

1. `MainMenu` (index 0)
2. `CreateRoomScene`
3. `RoomListScene` *(tạo scene mới — xem mục 4)*
4. `NightMap`

---

## 2. MainMenu — Launcher (duy nhất)

1. Tạo Empty: **`NetworkManager`**
2. Add Component: **`Launcher`**
3. Inspector:

| Field | Giá trị |
|-------|---------|
| Main Menu Scene | MainMenu |
| Create Room Scene | CreateRoomScene |
| Room List Scene | RoomListScene |
| Game Scene | NightMap |
| Default Max Players | 10 |
| Connect On Start | ✓ |

**Không** thêm Launcher vào scene khác.

### UI MainMenu

| Button | On Click → Launcher |
|--------|---------------------|
| Create Room (mở scene) | `OpenCreateRoomScene()` |
| Join Room (danh sách) | `OpenRoomListScene()` |
| Exit | `ExitGame()` |

---

## 3. CreateRoomScene

### UI

- **TMP Input Field** — tên phòng (tùy chọn, để trống = random `Room_XXXX`)
- **Button Create** → `CreateRoomManager.OnCreateButtonClicked()`
- **Button Back** → `CreateRoomManager.OnBackButtonClicked()`

### Setup

1. Empty **`CreateRoomUI`** → Add **`CreateRoomManager`**
2. Gán **Room Input** = TMP Input Field
3. **Không** thêm `Launcher` / `PhotonNetwork.Connect` ở scene này

---

## 4. RoomListScene (tạo mới)

1. **File → New Scene** → lưu `Assets/Scenes/RoomListScene.unity`
2. Canvas + EventSystem
3. **Scroll View** → Content (Vertical Layout Group)
4. Tạo prefab **`RoomListItem`**:
   - Panel + TMP Text (tên phòng)
   - TMP Text (số người)
   - Button Join → component **`RoomListItemUI`**
5. Empty **`RoomListUI`** → Add **`RoomListUI`**
   - Content Parent = ScrollView/Content
   - Room Item Prefab = RoomListItem
6. Button **Back** → `RoomListUI.OnBackButtonClicked()`
7. Button **Refresh** (tùy chọn) → `RoomListUI.OnRefreshButtonClicked()`
8. Thêm scene vào **Build Settings**

---

## 5. NightMap

- **`GameManager`** + spawn points (không có Launcher)
- Player spawn qua `PhotonNetwork.Instantiate` khi đã trong room

---

## 6. TMP Input Field (Create Room)

1. Canvas → UI → **Input Field - TextMeshPro**
2. Placeholder: `Nhập tên phòng...`
3. Gán vào **CreateRoomManager → Room Input**

---

## 7. Test flow

1. Play từ **MainMenu**
2. Console: `[Launcher] ConnectUsingSettings` → `OnConnectedToMaster` → `OnJoinedLobby`
3. **Create Room** → nhập tên → Create → `OnJoinedRoom` → load **NightMap**
4. Build 2 client / ParrelSync → Join cùng phòng

### Nếu vẫn lỗi "Can only connect while Disconnected"

- Stop Play mode hoàn toàn (tránh DDOL cũ trong Editor)
- Xóa object **NetworkManager** trùng trong scene (chỉ giữ MainMenu)
- Không gọi `ConnectUsingSettings` ở script khác (Voice demo, v.v.)

---

## 8. File script

```
Assets/Script/
├── Launcher.cs          ← Singleton, Photon callbacks
├── CreateRoomManager.cs ← UI tạo phòng
├── RoomListUI.cs        ← Danh sách phòng
├── RoomListItemUI.cs    ← Một dòng trong list
└── GameManager.cs       ← Spawn player NightMap
```
