<!-- README styled with HTML + inline CSS (GitHub supports inline styles on most elements) -->

<div align="center">

# 🌙 MidnightChat

<p>
  <strong>Trò chuyện & khám phá đêm</strong> · Unity · Photon PUN · Voice & Chat · Multiplayer mobile
</p>

<p>
  <img src="https://img.shields.io/badge/Unity-2022.3.62f2-000000?style=for-the-badge&logo=unity&logoColor=white" alt="Unity"/>
  <img src="https://img.shields.io/badge/Platform-Android%20%7C%20iOS-3DDC84?style=for-the-badge&logo=android&logoColor=white" alt="Platform"/>
  <img src="https://img.shields.io/badge/Networking-Photon-00B8D4?style=for-the-badge" alt="Photon"/>
  <img src="https://img.shields.io/badge/Voice-Photon%20Voice-7C3AED?style=for-the-badge" alt="Photon Voice"/>
  <img src="https://img.shields.io/badge/Project-MidnightChat-6366F1?style=for-the-badge" alt="MidnightChat"/>
</p>

</div>

---

## 📖 Giới thiệu

**MidnightChat** là game 3D trên mobile — gặp gỡ, trò chuyện và khám phá map đêm **NightMap** cùng bạn bè. Đặt tên, tạo hoặc tham gia phòng multiplayer qua Photon, dùng **voice chat**, **chat phòng** và điều khiển joystick trên màn hình cảm ứng.

<table>
<tr>
<td width="50%" valign="top" style="padding: 16px; background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); border-radius: 12px;">

### 🎮 Gameplay
- Di chuyển 3D với joystick
- Camera theo nhân vật
- Âm thanh bước chân (footstep)
- Map đêm với môi trường stylized

</td>
<td width="50%" valign="top" style="padding: 16px; background: linear-gradient(135deg, #1a1a2e 0%, #0f3460 100%); border-radius: 12px;">

### 🌐 Multiplayer
- Kết nối Photon tự động
- Tạo / tham gia phòng (tối đa 10 người)
- Danh sách phòng realtime
- Đồng bộ scene giữa các client

</td>
</tr>
<tr>
<td width="50%" valign="top" style="padding: 16px; background: linear-gradient(135deg, #16213e 0%, #1a1a2e 100%); border-radius: 12px;">

### 🎙️ Voice & Chat
- Voice chat qua Photon Voice
- Cài đặt âm lượng / mic trong game
- Chat text trong phòng

</td>
<td width="50%" valign="top" style="padding: 16px; background: linear-gradient(135deg, #0f3460 0%, #533483 100%); border-radius: 12px;">

### 📱 Mobile
- Adaptive Performance (Samsung / Google)
- UI tối ưu cảm ứng
- Hướng màn hình landscape

</td>
</tr>
</table>

---

## 📸 Screenshots

| 🏠 Main Menu | 🚪 Room List |
|:---:|:---:|
| ![Main Menu](docs/images/screenshot-main-menu.png) | ![Room List](docs/images/screenshot-room-list.png) |

| 🌲 Night Map (Gameplay) | 🎙️ Voice & Chat |
|:---:|:---:|
| ![Gameplay NightMap](docs/images/screenshot-gameplay.png) | ![Voice and Chat](docs/images/screenshot-voice-chat.png) |

### 🖼️ Banner

<p align="center">
  <img src="docs/images/screenshot-banner.png" alt="MidnightChat Banner" width="900"/>
</p>

---

## 🗺️ Luồng scene

```mermaid
flowchart LR
    A[MainMenu] --> B[CreateRoomScene]
    A --> C[RoomListScene]
    B --> D[NightMap]
    C --> D
```

| Scene | Mô tả |
|-------|--------|
| `MainMenu` | Menu chính, kết nối Photon, đặt tên người chơi |
| `CreateRoomScene` | Tạo phòng mới |
| `RoomListScene` | Xem và tham gia phòng có sẵn |
| `NightMap` | Map chơi chính — spawn player, voice, chat |

---

## 🛠️ Công nghệ

| Thành phần | Chi tiết |
|------------|----------|
| **Engine** | Unity `2022.3.62f2` |
| **Template** | Mobile 3D + Adaptive Performance |
| **Multiplayer** | Photon PUN 2 |
| **Voice** | Photon Voice |
| **UI** | TextMesh Pro, UGUI |
| **Điều khiển** | Joystick Pack, First Person Controller (modular) |

---

## 📁 Cấu trúc thư mục chính

```
MidnightChat/
├── Assets/
│   ├── Scenes/              # MainMenu, CreateRoom, RoomList, NightMap
│   ├── Script/
│   │   ├── Networking/      # Launcher, GameManager, Room list, Chat
│   │   ├── Player/          # Setup, name tag, footstep
│   │   ├── Voice/           # VoiceManager, PlayerVoice, settings UI
│   │   └── Camera/
│   └── Photon/              # PUN + Voice SDK
├── docs/
│   └── images/              # ← Đặt screenshot tại đây
├── ProjectSettings/
└── README.md
```

---

## 🚀 Cài đặt & chạy

### Yêu cầu

- [Unity Hub](https://unity.com/download) với editor **2022.3.62f2** (hoặc tương thích 2022.3 LTS)
- Tài khoản [Photon](https://www.photonengine.com/) — App ID PUN (và Voice nếu dùng voice)
- Android SDK / Xcode (khi build mobile)

### Các bước

1. **Clone** repository:
   ```bash
   git clone https://github.com/YOUR_USERNAME/MidnightChat.git
   cd MidnightChat
   ```
2. Mở project bằng **Unity Hub** → Add → chọn thư mục `MidnightChat`.
3. Cấu hình **Photon App ID** trong Photon dashboard và gán vào `PhotonServerSettings` (Assets/Photon/...).
4. Mở scene `Assets/Scenes/MainMenu.unity` và nhấn **Play**, hoặc build **File → Build Settings** cho Android/iOS.

### Build mobile

1. `File → Build Settings` → chọn **Android** hoặc **iOS**.
2. Đảm bảo 4 scene trong **Scenes In Build** (đã cấu hình sẵn).
3. **Player Settings** → Company: `dankchan`, Product: `MidnightChat`.

---

---

## 📜 Scripts chính

| Script | Vai trò |
|--------|---------|
| `Launcher.cs` | Singleton Photon — connect, lobby, tạo/join phòng, sync scene |
| `GameManager.cs` | Spawn player local khi vào `NightMap` |
| `CreateRoomManager.cs` | UI/logic tạo phòng |
| `RoomListUI.cs` | Hiển thị danh sách phòng |
| `RoomChatManager.cs` | Chat trong phòng |
| `VoiceManager.cs` | Quản lý voice toàn cục |
| `PlayerVoice.cs` | Voice từng người chơi |

---

## 🤝 Đóng góp

Pull request và issue luôn được chào đón. Vui lòng mô tả rõ bug hoặc tính năng kèm scene / bước tái hiện.

---


---

<div align="center">

<p style="color: #64748b; font-size: 14px;">
  Made with Unity · Photon · ❤️ <strong>MidnightChat</strong>
</p>

<p>
  <sub>Repository: <code>MidnightChat</code> · Unity 2022.3 LTS</sub>
</p>

</div>
