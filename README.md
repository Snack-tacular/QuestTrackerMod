# 📜 QuestTrackerMod for Sineus Arena

A modern, lightweight, high-performance **Quest & Achievement Tracker Mod** for **Sineus Arena**, built using BepInEx 5.

---

## ℹ️ About

**QuestTrackerMod** brings real-time in-game quest and achievement tracking directly onto your screen while playing *Sineus Arena*. 

No more opening full-screen menus mid-combat to check how close you are to completing an objective! QuestTrackerMod sorts your active quests by **"Almost Done First"**, translates raw localization/Russian keys into clean natural English, lets you pin your favorite quests, and features a sleek, non-intrusive dark obsidian glass interface.

### ✨ Key Features:
- **🔄 Single-Key View Cycling (`Q`)**: Seamlessly cycle between **Small HUD Window** ➔ **Big Quest Window** ➔ **Hidden**.
- **🔥 "Almost Done First" Sorting**: Automatically ranks active uncompleted quests by progress percentage so you always know which quests are nearest completion.
- **📌 Quest Pinning System**: Pin your favorite quests so they stay permanently anchored at the top of both the HUD and main window across game sessions.
- **📐 Fully Resizable & Draggable**: Drag windows anywhere on screen and resize them using the bottom-right corner grip (**`◢`**).
- **🌐 Pure English Formatting**: Cleans technical raw keys (`common herolifesteal chance` ➔ *Lifesteal Chance*), translates Cyrillic terms, and formats scroll stacks & map objectives cleanly.
- **⚡ Zero Stutter Performance**: Optimized definition caching and zero-allocation progress reading for smooth 60+ FPS gameplay.

---

## 🎮 Controls

| Hotkey | Description |
| :--- | :--- |
| **`Q`** | **Cycle Views**: Small HUD Window ➔ Big Quest Window ➔ Hidden |
| **Drag Header** | Click and drag the top banner of either window to move it around your screen. |
| **Drag Corner (`◢`)** | Click and drag the bottom-right corner grip to resize either window. |
| **`[PIN]` / `[PINNED]`** | Click on any quest card in the Big Window to pin/unpin it to the top. |

---

## 🗂️ Categories & Tabs

### Tabs (`Q` Big Window)
- **ALMOST DONE**: Shows uncompleted quests above your configured completion threshold (default 50%+).
- **ALL QUESTS**: Displays all available quests in the game.
- **IN PROGRESS**: Filters for active, uncompleted quests.
- **COMPLETED**: Shows finished and rewarded quests.

### Filter Pills
`[ All ]` `[ Heroes ]` `[ Weapons ]` `[ Scrolls ]` `[ Artifacts ]` `[ Map ]` `[ General ]`

---

## ⚙️ Configuration

The mod automatically generates a configuration file at `BepInEx/config/com.github.antigravity.questtrackermod.cfg` on first launch.

You can customize:
- **`ToggleKey`**: Default `Q`
- **`AlmostDoneThreshold`**: Progress ratio threshold (e.g. `0.50` for 50%)
- **`PinnedQuestIds`**: Persisted comma-separated list of pinned quest IDs
- **`Window position & dimensions`**: Saved automatically when dragging/resizing

---

## 🚀 Installation

1. Ensure **BepInEx 5** is installed in your Sineus Arena game directory (or managed via r2modman / Thunderstore).
2. Download the latest `QuestTrackerMod.dll` from the [Releases](https://github.com/Snack-tacular/QuestTrackerMod/releases) page.
3. Place `QuestTrackerMod.dll` into your `BepInEx/plugins/` directory:
   ```
   BepInEx/plugins/QuestTrackerMod/QuestTrackerMod.dll
   ```
4. Launch the game and press **`Q`**!

---

## 🛠️ Building from Source

Requirements: **.NET Core SDK 6.0+**

```bash
git clone https://github.com/Snack-tacular/QuestTrackerMod.git
cd QuestTrackerMod
dotnet build -c Release
```

---

## 📄 License
MIT License. Free to use, modify, and distribute.
