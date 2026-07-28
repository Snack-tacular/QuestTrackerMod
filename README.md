# 📜 Quest Tracker Mod for Sineus Arena

An in-game Quest Tracker mod for **Sineus Arena** built for **BepInEx**. It allows you to track all your active, in-progress, and completed quests live during gameplay, automatically sorted by **which quests are almost done first**!

---

## 🔥 Key Features

- **🔥 Almost Done First**: Automatically sorts active quests by completion percentage (`current / required` descending, e.g. 95%, 90%, 80%...).
- **📜 Full In-Game Quest Window**: Toggle a draggable, styled Quest Window anywhere in game using **`Q`**.
- **📌 Compact HUD Overlay**: Displays a sleek top-right overlay with your top 4 almost-done quests live on screen during gameplay (Toggle with **`K`**).
- **🏷️ Category Filtering**: Filter by `Heroes`, `Weapons`, `Boosts`, `Artifacts`, `Locations`, and `General`.
- **🔍 Live Search**: Easily search any quest by title, description, or ID.
- **⚡ Live Event Updates**: Automatically updates quest progress in real-time as you kill mobs, open chests, or complete objectives.

---

## 🎮 Controls

| Hotkey | Action |
|--------|--------|
| **`Q`** | Toggle Full Quest Window |
| **`K`** | Toggle Compact HUD Overlay |
| **Drag Header** | Reposition window anywhere on screen |

---

## ⚙️ Configuration

Settings are saved in `BepInEx/config/com.github.antigravity.questtrackermod.cfg`:
- `ToggleKey`: Custom hotkey for full window (default `Q`).
- `CompactHUDKey`: Custom hotkey for compact HUD (default `K`).
- `AlmostDoneThreshold`: Threshold percentage for "Almost Done" (default `0.50` = 50%).
- `CompactHUDMaxItems`: Number of top almost-done quests in HUD (default `4`).

---

## 🚀 Deployed Location

Deployed directly to your r2modman profile:
`C:\Users\luah8\AppData\Roaming\r2modmanPlus-local\SineusArenaSurvivors\profiles\Default\BepInEx\plugins\QuestTrackerMod\QuestTrackerMod.dll`
