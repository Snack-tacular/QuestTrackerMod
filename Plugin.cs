using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QuestTrackerMod
{
    [BepInPlugin("com.github.antigravity.questtrackermod", "Quest Tracker Mod", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource? Log;
        private Harmony? _harmony;

        // Config Entries - General & Full Window
        public static ConfigEntry<KeyCode>? CfgToggleKey;
        public static ConfigEntry<float>?   CfgPositionX;
        public static ConfigEntry<float>?   CfgPositionY;
        public static ConfigEntry<float>?   CfgWindowWidth;
        public static ConfigEntry<float>?   CfgWindowHeight;
        public static ConfigEntry<float>?   CfgAlmostDoneThreshold;
        public static ConfigEntry<string>?  CfgPinnedQuestIds;

        // Config Entries - Compact Small HUD Window
        public static ConfigEntry<float>?   CfgHUDPositionX;
        public static ConfigEntry<float>?   CfgHUDPositionY;
        public static ConfigEntry<float>?   CfgHUDWidth;
        public static ConfigEntry<float>?   CfgHUDHeight;
        public static ConfigEntry<int>?     CfgCompactHUDMaxItems;
        public static ConfigEntry<bool>?    CfgHideCompletedInAlmostDone;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Quest Tracker Mod initializing...");

            // General Configs
            CfgToggleKey = Config.Bind("General", "ToggleKey", KeyCode.Q, "Key to cycle quest tracker views (Small Window -> Big Window -> Hidden).");
            CfgPinnedQuestIds = Config.Bind("General", "PinnedQuestIds", "", "Comma-separated list of pinned quest IDs.");

            // Full Window Config
            CfgPositionX = Config.Bind("Window", "PositionX", 120f, "X position of the main quest window.");
            CfgPositionY = Config.Bind("Window", "PositionY", 90f, "Y position of the main quest window.");
            CfgWindowWidth = Config.Bind("Window", "Width", 620f, "Width of the main quest window.");
            CfgWindowHeight = Config.Bind("Window", "Height", 580f, "Height of the main quest window.");

            // Compact HUD Config
            CfgHUDPositionX = Config.Bind("CompactHUD", "PositionX", 1200f, "X position of the draggable small HUD window.");
            CfgHUDPositionY = Config.Bind("CompactHUD", "PositionY", 50f, "Y position of the draggable small HUD window.");
            CfgHUDWidth = Config.Bind("CompactHUD", "Width", 330f, "Width of the draggable small HUD window.");
            CfgHUDHeight = Config.Bind("CompactHUD", "Height", 280f, "Height of the draggable small HUD window.");
            CfgCompactHUDMaxItems = Config.Bind("CompactHUD", "MaxItems", 5, "Maximum number of quests to show in the small HUD.");

            // Filtering Config
            CfgAlmostDoneThreshold = Config.Bind("Filtering", "AlmostDoneThreshold", 0.50f, "Threshold percentage (0.0 to 1.0) to consider a quest 'Almost Done'.");
            CfgHideCompletedInAlmostDone = Config.Bind("Filtering", "HideCompletedInAlmostDone", true, "Hide 100% completed quests in the 'Almost Done' tab.");

            try
            {
                var go = new GameObject("QuestTrackerController");
                DontDestroyOnLoad(go);
                go.AddComponent<QuestTrackerUI>();

                _harmony = new Harmony("com.github.antigravity.questtrackermod");
                _harmony.PatchAll();

                Log.LogInfo("Quest Tracker Mod loaded successfully!");
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to initialize Quest Tracker Mod: " + ex);
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
