using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace QuestTrackerMod
{
    public enum QuestTrackerDisplayMode
    {
        SmallWindow = 0,
        BigWindow   = 1,
        Hidden      = 2
    }

    public enum QuestTab
    {
        AlmostDone,
        All,
        InProgress,
        Completed
    }

    public class QuestDataModel
    {
        public QuestDefinition Definition = null!;
        public string QuestId = "";
        public string Title = "";
        public string DescPattern = ""; // Pre-parsed description template
        public string CachedDescription = "";
        public QuestCategory Category;
        public int CurrentProgress = -1;
        public int RequiredProgress = 1;
        public float Percent = 0.0f;
        public bool IsCompleted = false;
        public bool IsRewarded = false;
        public int Remaining = 0;
        public bool IsPinned = false;
        public bool IsSessionQuest = false;

        public bool IsAlmostDone(float threshold)
        {
            return !IsCompleted && !IsRewarded && Percent >= threshold;
        }

        public bool UpdateProgress(HashSet<string> pinnedIds)
        {
            if (Definition == null) return false;

            int cur = 0;
            bool completed = false;
            bool rewarded = false;

            try
            {
                cur = QuestProgressRepository.GetProgress(Definition);
                completed = QuestProgressRepository.IsCompleted(Definition);
                rewarded = QuestProgressRepository.IsRewarded(Definition);
            }
            catch { }

            if (cur >= RequiredProgress)
            {
                completed = true;
            }

            bool pinned = pinnedIds != null && pinnedIds.Contains(QuestId);

            if (cur == CurrentProgress && completed == IsCompleted && rewarded == IsRewarded && pinned == IsPinned)
            {
                return false;
            }

            CurrentProgress = cur;
            IsCompleted = completed;
            IsRewarded = rewarded;
            IsPinned = pinned;
            Percent = Mathf.Clamp01((float)cur / RequiredProgress);
            Remaining = Mathf.Max(0, RequiredProgress - cur);

            FormatDescriptionFast();
            return true;
        }

        private void FormatDescriptionFast()
        {
            if (string.IsNullOrEmpty(DescPattern))
            {
                CachedDescription = $"Progress: {CurrentProgress} / {RequiredProgress}";
                return;
            }

            CachedDescription = $"{DescPattern} ({CurrentProgress} / {RequiredProgress})";
        }

        // Static Pre-Parsing (Runs ONCE when quest definitions are cached!)
        public static QuestDataModel CreateStaticModel(QuestDefinition q, HashSet<string> pinnedIds)
        {
            string qId = q.questId ?? q.name ?? "Quest";
            int req = 1;
            if (q.condition != null && q.condition.RequiredProgress > 0)
            {
                req = q.condition.RequiredProgress;
            }

            string title = FormatTitleStatic(q);
            string descPattern = BuildDescPatternStatic(q, req);

            var model = new QuestDataModel
            {
                Definition = q,
                QuestId = qId,
                Title = title,
                DescPattern = descPattern,
                Category = q.category,
                RequiredProgress = req,
                IsSessionQuest = !q.persistProgress
            };

            model.UpdateProgress(pinnedIds);
            return model;
        }

        private static string FormatTitleStatic(QuestDefinition q)
        {
            if (q == null) return "Unknown Quest";

            string result = "Objective";

            if (q.condition != null)
            {
                string condType = q.condition.GetType().Name;
                string targetName = ExtractTargetNameFromConditionStatic(q.condition);

                switch (condType)
                {
                    case "HeroLevelQuestCondition":
                        result = !string.IsNullOrEmpty(targetName) ? $"Hero Mastery: {targetName}" : "Hero Mastery";
                        break;
                    case "KillWithHeroTotalQuestCondition":
                        result = !string.IsNullOrEmpty(targetName) ? $"Hero Slayer: {targetName}" : "Hero Slayer";
                        break;
                    case "KillWithWeaponQuestCondition":
                    case "WeaponLevelQuestCondition":
                        result = !string.IsNullOrEmpty(targetName) ? $"Weapon Master: {targetName}" : "Weapon Master";
                        break;
                    case "KillBossTotalQuestCondition":
                    case "BossKillTimedQuestCondition":
                        result = !string.IsNullOrEmpty(targetName) ? FormatBossTitleStatic(targetName) : "Boss Hunter";
                        break;
                    case "KillMobsTotalQuestCondition":
                        result = "Monster Slayer";
                        break;
                    case "KillSpecificMobsTotalQuestCondition":
                        result = !string.IsNullOrEmpty(targetName) ? $"Huntsman: {targetName}" : "Targeted Hunt";
                        break;
                    case "OpenChestsTotalQuestCondition":
                        result = "Treasure Hunter";
                        break;
                    case "ClearLairsTotalQuestCondition":
                        result = "Lair Purger";
                        break;
                    case "ShrineActivateQuestCondition":
                        result = "Beacon Pilgrim";
                        break;
                    case "SurviveTimeQuestCondition":
                        result = "Survivalist";
                        break;
                    case "UseBellsTotalQuestCondition":
                        result = "Bells";
                        break;
                    case "UseFoxesTotalQuestCondition":
                        result = "Fox Shrines";
                        break;
                    case "ArtifactCollectQuestCondition":
                        result = !string.IsNullOrEmpty(targetName) ? $"Relic Collector: {targetName}" : "Relic Collector";
                        break;
                    case "BoostLevelQuestCondition":
                        result = !string.IsNullOrEmpty(targetName) ? $"Scroll: {targetName}" : "Scroll Stacks";
                        break;
                    case "BagCollectQuestCondition":
                        result = "Loot Bags";
                        break;
                    case "MapClearQuestCondition":
                        result = "Stage Conqueror";
                        break;
                    case "CompleteQuestsTotalQuestCondition":
                        result = "Master Adventurer";
                        break;
                    default:
                        string rawName = q.name;
                        if (string.IsNullOrEmpty(rawName)) rawName = q.questId;
                        result = CleanTextStatic(rawName);
                        break;
                }
            }
            else
            {
                string rawName = q.name;
                if (string.IsNullOrEmpty(rawName)) rawName = q.questId;
                result = CleanTextStatic(rawName);
            }

            return ApplySpecificRenamesStatic(result);
        }

        private static string FormatBossTitleStatic(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return "Boss Hunter";
            targetName = ApplySpecificRenamesStatic(targetName);
            if (targetName.StartsWith("Boss", StringComparison.OrdinalIgnoreCase))
            {
                return $"Defeat {targetName}";
            }
            return $"Boss Hunter: {targetName}";
        }

        private static string BuildDescPatternStatic(QuestDefinition q, int req)
        {
            if (q == null) return "Progress:";

            string pattern = "Progress:";

            if (q.condition != null)
            {
                string condType = q.condition.GetType().Name;
                string targetName = ExtractTargetNameFromConditionStatic(q.condition);

                switch (condType)
                {
                    case "KillMobsTotalQuestCondition":
                        pattern = "Slay monsters";
                        break;
                    case "KillSpecificMobsTotalQuestCondition":
                        pattern = !string.IsNullOrEmpty(targetName) ? $"Slay {targetName}" : "Slay target monsters";
                        break;
                    case "KillBossTotalQuestCondition":
                        if (!string.IsNullOrEmpty(targetName))
                        {
                            string bossLabel = targetName.StartsWith("Boss", StringComparison.OrdinalIgnoreCase) ? targetName : $"Boss {targetName}";
                            pattern = $"Defeat {bossLabel}";
                        }
                        else
                        {
                            pattern = "Defeat Boss";
                        }
                        break;
                    case "BossKillTimedQuestCondition":
                        float timeLimit = GetSingleFieldStatic(q.condition, "timeLimitSeconds", 180f);
                        if (!string.IsNullOrEmpty(targetName))
                        {
                            string bossLabel = targetName.StartsWith("Boss", StringComparison.OrdinalIgnoreCase) ? targetName : $"Boss {targetName}";
                            pattern = $"Defeat {bossLabel} under {timeLimit:F0}s";
                        }
                        else
                        {
                            pattern = $"Defeat Boss under {timeLimit:F0}s";
                        }
                        break;
                    case "HeroLevelQuestCondition":
                        int reqLvl = GetIntFieldStatic(q.condition, "requiredLevel", req);
                        pattern = !string.IsNullOrEmpty(targetName) ? $"Reach Level {reqLvl} with {targetName}" : $"Reach Hero Level {reqLvl}";
                        break;
                    case "KillWithHeroTotalQuestCondition":
                        pattern = !string.IsNullOrEmpty(targetName) ? $"Slay monsters with {targetName}" : "Slay monsters with hero";
                        break;
                    case "KillWithWeaponQuestCondition":
                        pattern = !string.IsNullOrEmpty(targetName) ? $"Slay monsters with {targetName}" : "Slay monsters with weapon";
                        break;
                    case "WeaponLevelQuestCondition":
                        int wReqLvl = GetIntFieldStatic(q.condition, "requiredLevel", req);
                        pattern = !string.IsNullOrEmpty(targetName) ? $"Upgrade {targetName} to Level {wReqLvl}" : $"Upgrade weapon to Level {wReqLvl}";
                        break;
                    case "OpenChestsTotalQuestCondition":
                        pattern = "Open treasure chests";
                        break;
                    case "ClearLairsTotalQuestCondition":
                        pattern = "Clear lairs";
                        break;
                    case "ShrineActivateQuestCondition":
                        pattern = "Activate beacon shrines";
                        break;
                    case "SurviveTimeQuestCondition":
                        float timeS = GetSingleFieldStatic(q.condition, "survivalSeconds", req);
                        int reqMin = (int)(timeS / 60);
                        int reqSec = (int)(timeS % 60);
                        pattern = $"Survive for {reqMin}m {reqSec:D2}s";
                        break;
                    case "UseBellsTotalQuestCondition":
                        pattern = "Ring Bells";
                        break;
                    case "UseFoxesTotalQuestCondition":
                        pattern = "Use Fox Shrines";
                        break;
                    case "ArtifactCollectQuestCondition":
                        pattern = !string.IsNullOrEmpty(targetName) ? $"Collect artifact: {targetName}" : "Collect artifacts";
                        break;
                    case "BoostLevelQuestCondition":
                        int bReqLvl = GetIntFieldStatic(q.condition, "requiredLevel", req);
                        pattern = !string.IsNullOrEmpty(targetName) ? $"Reach {bReqLvl} stacks of {targetName}" : $"Reach {bReqLvl} stacks";
                        break;
                    case "BagCollectQuestCondition":
                        pattern = "Collect Bags";
                        break;
                    case "MapClearQuestCondition":
                        pattern = "Clear stages";
                        break;
                    case "CompleteQuestsTotalQuestCondition":
                        pattern = "Complete quests";
                        break;
                }
            }

            return ApplySpecificRenamesStatic(pattern);
        }

        private static bool ContainsCyrillicStatic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
            {
                if (c >= 0x0400 && c <= 0x04FF) return true;
            }
            return false;
        }

        private static string ExtractTargetNameFromConditionStatic(object condition)
        {
            if (condition == null) return "";
            try
            {
                Type type = condition.GetType();

                var fHero = type.GetField("heroDefinition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fHero != null)
                {
                    object heroObj = fHero.GetValue(condition);
                    if (heroObj != null)
                    {
                        string name = GetCleanNameFromObjectStatic(heroObj);
                        if (!string.IsNullOrEmpty(name)) return ApplySpecificRenamesStatic(name);
                    }
                }

                var fArt = type.GetField("artifactPreset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fArt != null)
                {
                    object artObj = fArt.GetValue(condition);
                    if (artObj != null)
                    {
                        string name = GetCleanNameFromObjectStatic(artObj);
                        if (!string.IsNullOrEmpty(name)) return ApplySpecificRenamesStatic(name);
                    }
                }

                var fBoost = type.GetField("boostPreset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fBoost != null)
                {
                    object bObj = fBoost.GetValue(condition);
                    if (bObj != null)
                    {
                        string name = GetCleanNameFromObjectStatic(bObj);
                        if (!string.IsNullOrEmpty(name)) return ApplySpecificRenamesStatic(name);
                    }
                }

                var fBoss = type.GetField("bossName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fBoss != null)
                {
                    string bossStr = fBoss.GetValue(condition) as string ?? "";
                    if (!string.IsNullOrEmpty(bossStr)) return ApplySpecificRenamesStatic(CleanTextStatic(bossStr));
                }

                var fMobs = type.GetField("mobNames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fMobs != null)
                {
                    string[] mobArr = fMobs.GetValue(condition) as string[];
                    if (mobArr != null && mobArr.Length > 0 && !string.IsNullOrEmpty(mobArr[0]))
                    {
                        return ApplySpecificRenamesStatic(CleanTextStatic(mobArr[0]));
                    }
                }

                var fWep = type.GetField("weaponType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fWep != null)
                {
                    object wepVal = fWep.GetValue(condition);
                    if (wepVal != null)
                    {
                        string wepStr = wepVal.ToString() ?? "";
                        if (!string.IsNullOrEmpty(wepStr)) return ApplySpecificRenamesStatic(CleanTextStatic(wepStr));
                    }
                }
            }
            catch { }
            return "";
        }

        private static string GetCleanNameFromObjectStatic(object obj)
        {
            if (obj == null) return "";
            try
            {
                Type type = obj.GetType();

                var pHero = type.GetProperty("HeroName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pHero != null)
                {
                    string val = pHero.GetValue(obj) as string ?? "";
                    if (!string.IsNullOrEmpty(val)) return CleanTextStatic(val);
                }

                var pDisp = type.GetField("displayName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pDisp != null)
                {
                    string val = pDisp.GetValue(obj) as string ?? "";
                    if (!string.IsNullOrEmpty(val)) return CleanTextStatic(val);
                }

                var pName = type.GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pName != null)
                {
                    string val = pName.GetValue(obj) as string ?? "";
                    if (!string.IsNullOrEmpty(val)) return CleanTextStatic(val);
                }

                var fName = type.GetField("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fName != null)
                {
                    string val = fName.GetValue(obj) as string ?? "";
                    if (!string.IsNullOrEmpty(val)) return CleanTextStatic(val);
                }
            }
            catch { }
            return CleanTextStatic(obj.ToString() ?? "");
        }

        private static string CleanTextStatic(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            if (ContainsCyrillicStatic(text))
            {
                text = TranslateRussianTermStatic(text);
            }

            text = text.Replace("Quest_", "").Replace("quest_", "")
                       .Replace("Achievement_", "").Replace("ach_", "")
                       .Replace("QUEST_", "").Replace("LOC_", "")
                       .Replace("(Clone)", "").Trim();

            text = CleanBoostOrStatNameStatic(text);

            text = ApplySpecificRenamesStatic(text);

            if (text.Length > 0 && !ContainsCyrillicStatic(text))
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
            }
            return "Objective";
        }

        private static string CleanBoostOrStatNameStatic(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            string cleaned = text;

            cleaned = Regex.Replace(cleaned, @"\b(common|uncommon|rare|epic|legendary|preset|buff|boost|stat|hero|unit|team|player)\b", "", RegexOptions.IgnoreCase);

            cleaned = Regex.Replace(cleaned, @"([a-z])([A-Z])", "$1 $2");
            cleaned = cleaned.Replace("_", " ").Trim();

            cleaned = Regex.Replace(cleaned, @"herolifesteal|lifesteal", "Lifesteal", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"movementspeed|movespeed", "Movement Speed", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"attackspeed", "Attack Speed", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"critchance", "Critical Chance", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"critdamage", "Critical Damage", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"healthregen", "Health Regen", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"maxhp", "Max HP", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"pickuprange", "Pickup Range", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"cooldown", "Cooldown Reduction", RegexOptions.IgnoreCase);

            cleaned = Regex.Replace(cleaned, @"\bhero\b", "", RegexOptions.IgnoreCase);

            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(cleaned)) return "Scroll";

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned);
        }

        private static string ApplySpecificRenamesStatic(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = Regex.Replace(text, @"\b(Storm\s*Anchor)\b", "Cursed Anchor", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(Black\s*Hole)\b", "Ancient Cube", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(Soup)\b", "Ogre's Stew", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(Hammer\s*Chaos)\b", "Hammer of Chaos", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(Ice\s*Zone)\b", "Ice Walk", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(Ritual\s*Niddle|Ritual\s*Needle|Niddle)\b", "Needle", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(Staff)\b", "Follower of the Will", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(Toxic\s*Bomb)\b", "Acid Bomb", RegexOptions.IgnoreCase);

            return text;
        }

        private static string TranslateRussianTermStatic(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains("Светоч") || text.Contains("светоч")) return "Beacon Shrine";
            if (text.Contains("Логово") || text.Contains("логово")) return "Lairs";
            if (text.Contains("Босс") || text.Contains("босс")) return "Boss";
            if (text.Contains("Убить") || text.Contains("убить")) return "Slay Monsters";
            if (text.Contains("Сундук") || text.Contains("сундук")) return "Chests";
            if (text.Contains("Выжить") || text.Contains("выжить")) return "Survive";
            if (text.Contains("Герой") || text.Contains("герой")) return "Hero";
            if (text.Contains("Оружие") || text.Contains("оружие")) return "Weapon";
            return "";
        }

        private static int GetIntFieldStatic(object obj, string fieldName, int fallback)
        {
            try
            {
                var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) return (int)f.GetValue(obj);
            }
            catch { }
            return fallback;
        }

        private static float GetSingleFieldStatic(object obj, string fieldName, float fallback)
        {
            try
            {
                var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) return (float)f.GetValue(obj);
            }
            catch { }
            return fallback;
        }
    }

    public class QuestTrackerUI : MonoBehaviour
    {
        private const int MAIN_WINDOW_ID = 94821;
        private const int HUD_WINDOW_ID  = 94822;

        // Display Mode: 0 = SmallWindow, 1 = BigWindow, 2 = Hidden
        private QuestTrackerDisplayMode _displayMode = QuestTrackerDisplayMode.SmallWindow;

        // Pinned Quest IDs
        private HashSet<string> _pinnedQuestIds = new HashSet<string>();

        // Pre-parsed Quest Data Models
        private List<QuestDataModel> _models = new List<QuestDataModel>();

        // Sorted Display List
        private List<QuestDataModel> _sortedModels = new List<QuestDataModel>();

        // UI State - Main Window
        private Rect _windowRect = new Rect(120, 90, 620, 580);
        private Vector2 _scrollPos = Vector2.zero;
        private string _searchQuery = "";
        private QuestTab _currentTab = QuestTab.AlmostDone;
        private int _selectedCategoryIndex = 0; // 0 = All

        // UI State - Small HUD Window
        private Rect _hudWindowRect = new Rect(1200, 50, 330, 280);
        private Vector2 _hudScrollPos = Vector2.zero;

        // Window Resizing State
        private bool _isResizingMain = false;
        private bool _isResizingHUD = false;
        private Vector2 _dragStartPos;
        private Vector2 _windowStartSize;

        private float _lastFetchTime = -10f;
        private const float REFRESH_INTERVAL = 0.5f;

        // Custom Visual Styling & Persistent Assets
        private bool _stylesInitialized = false;

        private GUIStyle? _windowStyle;
        private GUIStyle? _headerTitleStyle;
        private GUIStyle? _headerSubStyle;
        private GUIStyle? _tabStyle;
        private GUIStyle? _tabActiveStyle;
        private GUIStyle? _pillStyle;
        private GUIStyle? _pillActiveStyle;
        private GUIStyle? _cardBoxStyle;
        private GUIStyle? _questTitleStyle;
        private GUIStyle? _questDescStyle;
        private GUIStyle? _questRatioStyle;
        private GUIStyle? _badgeAlmostDoneStyle;
        private GUIStyle? _badgeActiveStyle;
        private GUIStyle? _badgeCompletedStyle;
        private GUIStyle? _badgePinnedStyle;
        private GUIStyle? _pinButtonStyle;
        private GUIStyle? _pinButtonActiveStyle;
        private GUIStyle? _searchStyle;
        private GUIStyle? _footerStyle;

        private GUIStyle? _hudWindowStyle;
        private GUIStyle? _hudTitleStyle;
        private GUIStyle? _hudItemStyle;
        private GUIStyle? _hudItemTitleStyle;
        private GUIStyle? _resizeGripStyle;

        private Texture2D? _bgTexture;
        private Texture2D? _hudBgTexture;
        private Texture2D? _cardBgTexture;
        private Texture2D? _barBgTexture;
        private Texture2D? _barFillAlmostDoneTexture;
        private Texture2D? _barFillActiveTexture;
        private Texture2D? _barFillCompletedTexture;
        private Texture2D? _tabActiveTex;
        private Texture2D? _tabInactiveTex;
        private Texture2D? _tabHoverTex;
        private Texture2D? _pillActiveTex;
        private Texture2D? _pillInactiveTex;
        private Texture2D? _pillHoverTex;
        private Texture2D? _pinActiveTex;
        private Texture2D? _pinInactiveTex;
        private Texture2D? _pinHoverTex;

        private void Start()
        {
            LoadPinnedQuestIds();

            float posX = Plugin.CfgPositionX?.Value ?? 120f;
            float posY = Plugin.CfgPositionY?.Value ?? 90f;
            float w = Plugin.CfgWindowWidth?.Value ?? 620f;
            float h = Plugin.CfgWindowHeight?.Value ?? 580f;
            _windowRect = new Rect(posX, posY, w, h);

            float hudX = Plugin.CfgHUDPositionX?.Value ?? (Screen.width - 340f);
            float hudY = Plugin.CfgHUDPositionY?.Value ?? 50f;
            float hudW = Plugin.CfgHUDWidth?.Value ?? 330f;
            float hudH = Plugin.CfgHUDHeight?.Value ?? 280f;
            _hudWindowRect = new Rect(hudX, hudY, hudW, hudH);

            try
            {
                if (QuestService.I != null)
                {
                    QuestService.I.OnProgressChanged += OnQuestProgressUpdated;
                    QuestService.I.OnQuestCompleted += OnQuestProgressUpdated;
                }
            }
            catch { }

            InitializeModelsOnce();
        }

        private void LoadPinnedQuestIds()
        {
            _pinnedQuestIds.Clear();
            string raw = Plugin.CfgPinnedQuestIds?.Value ?? "";
            if (!string.IsNullOrEmpty(raw))
            {
                string[] parts = raw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)
                {
                    string id = p.Trim();
                    if (!string.IsNullOrEmpty(id)) _pinnedQuestIds.Add(id);
                }
            }
        }

        private void SavePinnedQuestIds()
        {
            string joined = string.Join(",", _pinnedQuestIds);
            if (Plugin.CfgPinnedQuestIds != null)
            {
                Plugin.CfgPinnedQuestIds.Value = joined;
            }
        }

        private void TogglePinQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return;
            if (_pinnedQuestIds.Contains(questId))
            {
                _pinnedQuestIds.Remove(questId);
            }
            else
            {
                _pinnedQuestIds.Add(questId);
            }
            SavePinnedQuestIds();
            UpdateAllProgress(true);
        }

        private void OnQuestProgressUpdated(QuestDefinition quest)
        {
            _lastFetchTime = -10f;
        }

        private void Update()
        {
            KeyCode toggleKey = Plugin.CfgToggleKey?.Value ?? KeyCode.Q;
            if (Input.GetKeyDown(toggleKey))
            {
                _displayMode = (QuestTrackerDisplayMode)(((int)_displayMode + 1) % 3);
                
                if (_displayMode == QuestTrackerDisplayMode.BigWindow || _displayMode == QuestTrackerDisplayMode.SmallWindow)
                {
                    UpdateAllProgress(true);
                }
            }

            if (Time.time - _lastFetchTime >= REFRESH_INTERVAL)
            {
                UpdateAllProgress(false);
            }
        }

        private void InitializeModelsOnce()
        {
            if (_models.Count > 0) return;

            var rawDefs = FetchAllQuestDefinitions();
            var list = new List<QuestDataModel>();

            foreach (var q in rawDefs)
            {
                if (q != null)
                {
                    list.Add(QuestDataModel.CreateStaticModel(q, _pinnedQuestIds));
                }
            }

            _models = list;
            SortModels();
        }

        private void UpdateAllProgress(bool forceSort)
        {
            _lastFetchTime = Time.time;

            if (_models.Count == 0)
            {
                InitializeModelsOnce();
                return;
            }

            bool anyChanged = false;
            foreach (var model in _models)
            {
                if (model.UpdateProgress(_pinnedQuestIds))
                {
                    anyChanged = true;
                }
            }

            if (anyChanged || forceSort || _sortedModels.Count != _models.Count)
            {
                SortModels();
            }
        }

        private void SortModels()
        {
            _sortedModels = _models.OrderByDescending(q => q.IsPinned)
                                   .ThenBy(q => q.IsCompleted || q.IsRewarded ? 1 : 0)
                                   .ThenByDescending(q => q.Percent)
                                   .ThenBy(q => q.Remaining)
                                   .ThenBy(q => q.Title)
                                   .ToList();
        }

        private List<QuestDefinition> FetchAllQuestDefinitions()
        {
            var list = new List<QuestDefinition>();

            try
            {
                if (QuestService.I != null)
                {
                    var db = GetDatabaseFromObject(QuestService.I);
                    if (db != null && db.Quests != null)
                    {
                        foreach (var q in db.Quests)
                        {
                            if (q != null && !list.Contains(q)) list.Add(q);
                        }
                    }
                }
            }
            catch { }

            if (list.Count == 0)
            {
                try
                {
                    if (AchievementService.I != null)
                    {
                        var db = GetDatabaseFromObject(AchievementService.I);
                        if (db != null && db.Quests != null)
                        {
                            foreach (var q in db.Quests)
                            {
                                if (q != null && !list.Contains(q)) list.Add(q);
                            }
                        }
                    }
                }
                catch { }
            }

            if (list.Count == 0)
            {
                try
                {
                    var dbs = Resources.FindObjectsOfTypeAll<QuestDatabase>();
                    if (dbs != null)
                    {
                        foreach (var db in dbs)
                        {
                            if (db != null && db.Quests != null)
                            {
                                foreach (var q in db.Quests)
                                {
                                    if (q != null && !list.Contains(q)) list.Add(q);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        private static QuestDatabase? GetDatabaseFromObject(object obj)
        {
            if (obj == null) return null;
            try
            {
                var type = obj.GetType();
                var f = type.GetField("_database", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     ?? type.GetField("database", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null)
                {
                    return f.GetValue(obj) as QuestDatabase;
                }
                var p = type.GetProperty("_database", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     ?? type.GetProperty("database", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (p != null)
                {
                    return p.GetValue(obj) as QuestDatabase;
                }
            }
            catch { }
            return null;
        }

        private void OnGUI()
        {
            InitStylesIfNeeded();

            HandleGlobalResizeDrag();

            switch (_displayMode)
            {
                case QuestTrackerDisplayMode.SmallWindow:
                    DrawDraggableSmallWindow();
                    break;
                case QuestTrackerDisplayMode.BigWindow:
                    DrawFullMainWindow();
                    break;
                case QuestTrackerDisplayMode.Hidden:
                    break;
            }
        }

        private void HandleGlobalResizeDrag()
        {
            Event e = Event.current;
            if (e == null) return;

            if (_isResizingMain)
            {
                if (e.type == EventType.MouseDrag)
                {
                    Vector2 mousePos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    Vector2 delta = mousePos - _dragStartPos;
                    _windowRect.width = Mathf.Max(420f, _windowStartSize.x + delta.x);
                    _windowRect.height = Mathf.Max(320f, _windowStartSize.y + delta.y);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    _isResizingMain = false;
                    if (Plugin.CfgWindowWidth != null) Plugin.CfgWindowWidth.Value = _windowRect.width;
                    if (Plugin.CfgWindowHeight != null) Plugin.CfgWindowHeight.Value = _windowRect.height;
                    e.Use();
                }
            }
            else if (_isResizingHUD)
            {
                if (e.type == EventType.MouseDrag)
                {
                    Vector2 mousePos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    Vector2 delta = mousePos - _dragStartPos;
                    _hudWindowRect.width = Mathf.Max(260f, _windowStartSize.x + delta.x);
                    _hudWindowRect.height = Mathf.Max(150f, _windowStartSize.y + delta.y);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    _isResizingHUD = false;
                    if (Plugin.CfgHUDWidth != null) Plugin.CfgHUDWidth.Value = _hudWindowRect.width;
                    if (Plugin.CfgHUDHeight != null) Plugin.CfgHUDHeight.Value = _hudWindowRect.height;
                    e.Use();
                }
            }
        }

        // ── 1. Resizable Small Window ──────────────────────────────────────────────
        private void DrawDraggableSmallWindow()
        {
            _hudWindowRect = GUI.Window(HUD_WINDOW_ID, _hudWindowRect, DrawCompactHUDContent, "", _hudWindowStyle);
            _hudWindowRect.x = Mathf.Clamp(_hudWindowRect.x, 0, Screen.width - _hudWindowRect.width);
            _hudWindowRect.y = Mathf.Clamp(_hudWindowRect.y, 0, Screen.height - _hudWindowRect.height);

            if (Event.current.type == EventType.MouseUp)
            {
                if (Plugin.CfgHUDPositionX != null) Plugin.CfgHUDPositionX.Value = _hudWindowRect.x;
                if (Plugin.CfgHUDPositionY != null) Plugin.CfgHUDPositionY.Value = _hudWindowRect.y;
            }
        }

        private void DrawCompactHUDContent(int windowID)
        {
            float threshold = Plugin.CfgAlmostDoneThreshold?.Value ?? 0.50f;
            int maxItems = Plugin.CfgCompactHUDMaxItems?.Value ?? 5;

            var pinnedQuests = _sortedModels.Where(q => q.IsPinned && !q.IsCompleted && !q.IsRewarded).ToList();
            var unpinnedAlmostDone = _sortedModels.Where(q => !q.IsPinned && !q.IsCompleted && !q.IsRewarded && q.Percent >= threshold).ToList();

            var displayQuests = pinnedQuests.Concat(unpinnedAlmostDone).Take(maxItems).ToList();

            float w = _hudWindowRect.width;

            GUILayout.BeginHorizontal();
            GUILayout.Label("PINNED & ALMOST DONE", _hudTitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("[Q] Cycle", _footerStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            _hudScrollPos = GUILayout.BeginScrollView(_hudScrollPos, false, true, GUILayout.ExpandHeight(true));

            if (displayQuests.Count == 0)
            {
                GUILayout.Label("No pinned or almost done quests!", _hudItemStyle);
            }
            else
            {
                foreach (var q in displayQuests)
                {
                    GUILayout.BeginVertical(_cardBoxStyle);

                    GUILayout.BeginHorizontal();
                    if (q.IsPinned)
                    {
                        GUILayout.Label("PINNED", _badgePinnedStyle, GUILayout.ExpandWidth(false), GUILayout.Height(16));
                        GUILayout.Space(4);
                    }
                    GUILayout.Label(q.Title, _hudItemTitleStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{(q.Percent * 100f):F0}%", _hudItemTitleStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.Label(q.CachedDescription, _hudItemStyle);
                    GUILayout.Space(2);

                    Rect barRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(6), GUILayout.ExpandWidth(true));
                    if (Event.current.type == EventType.Repaint)
                    {
                        GUI.DrawTexture(barRect, _barBgTexture!);
                        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * q.Percent, barRect.height);
                        GUI.DrawTexture(fillRect, _barFillAlmostDoneTexture!);
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(3);
                }
            }

            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, w - 20, 30));

            DrawWindowResizeGrip(ref _hudWindowRect, ref _isResizingHUD);
        }

        // ── 2. Resizable Full Main Window ──────────────────────────────────
        private void DrawFullMainWindow()
        {
            _windowRect = GUI.Window(MAIN_WINDOW_ID, _windowRect, DrawMainWindowContent, "", _windowStyle);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

            if (Event.current.type == EventType.MouseUp)
            {
                if (Plugin.CfgPositionX != null) Plugin.CfgPositionX.Value = _windowRect.x;
                if (Plugin.CfgPositionY != null) Plugin.CfgPositionY.Value = _windowRect.y;
            }
        }

        private void DrawMainWindowContent(int windowID)
        {
            float w = _windowRect.width;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("QUEST TRACKER", _headerTitleStyle);
            GUILayout.Label("Real-time In-Game Quest Progress • Press [Q] to Cycle Views", _headerSubStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchQuery = GUILayout.TextField(_searchQuery, _searchStyle, GUILayout.Height(24));
            if (!string.IsNullOrEmpty(_searchQuery) && GUILayout.Button("Clear", GUILayout.Width(50), GUILayout.Height(24)))
            {
                _searchQuery = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (DrawTabButton("ALMOST DONE", QuestTab.AlmostDone)) _currentTab = QuestTab.AlmostDone;
            if (DrawTabButton("ALL QUESTS", QuestTab.All)) _currentTab = QuestTab.All;
            if (DrawTabButton("IN PROGRESS", QuestTab.InProgress)) _currentTab = QuestTab.InProgress;
            if (DrawTabButton("COMPLETED", QuestTab.Completed)) _currentTab = QuestTab.Completed;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            string[] categories = new string[] { "All", "Heroes", "Weapons", "Scrolls", "Artifacts", "Map", "General" };
            GUILayout.BeginHorizontal();
            for (int i = 0; i < categories.Length; i++)
            {
                bool active = (_selectedCategoryIndex == i);
                GUIStyle pillStyle = active ? _pillActiveStyle! : _pillStyle!;
                if (GUILayout.Button(categories[i], pillStyle, GUILayout.Height(22)))
                {
                    _selectedCategoryIndex = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            float threshold = Plugin.CfgAlmostDoneThreshold?.Value ?? 0.50f;
            bool hideCompletedInAlmostDone = Plugin.CfgHideCompletedInAlmostDone?.Value ?? true;

            var filtered = _sortedModels.Where(q =>
            {
                switch (_currentTab)
                {
                    case QuestTab.AlmostDone:
                        if (hideCompletedInAlmostDone && (q.IsCompleted || q.IsRewarded)) return false;
                        if (!q.IsPinned && q.Percent < threshold) return false;
                        break;
                    case QuestTab.InProgress:
                        if (q.IsCompleted || q.IsRewarded) return false;
                        break;
                    case QuestTab.Completed:
                        if (!q.IsCompleted && !q.IsRewarded) return false;
                        break;
                }

                if (_selectedCategoryIndex > 0)
                {
                    QuestCategory targetCat = (QuestCategory)(_selectedCategoryIndex - 1);
                    if (q.Category != targetCat) return false;
                }

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    string sq = _searchQuery.ToLowerInvariant();
                    bool matchTitle = q.Title.ToLowerInvariant().Contains(sq);
                    bool matchDesc = q.CachedDescription.ToLowerInvariant().Contains(sq);
                    bool matchId = q.QuestId.ToLowerInvariant().Contains(sq);
                    if (!matchTitle && !matchDesc && !matchId) return false;
                }

                return true;
            }).ToList();

            int totalFiltered = filtered.Count;
            int almostDoneCount = _sortedModels.Count(q => q.IsAlmostDone(threshold));
            int pinnedCount = _pinnedQuestIds.Count;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Displaying {totalFiltered} Quests  •  Pinned: {pinnedCount}  •  Almost Done: {almostDoneCount}", _footerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Press [Q] to cycle  •  Drag ◢ corner to resize", _footerStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUILayout.ExpandHeight(true));

            if (filtered.Count == 0)
            {
                GUILayout.Space(40);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No quests match your current filter.", _questTitleStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (var quest in filtered)
                {
                    DrawPrettyQuestCard(quest, threshold);
                    GUILayout.Space(6);
                }
            }

            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, w - 20, 40));

            DrawWindowResizeGrip(ref _windowRect, ref _isResizingMain);
        }

        private void DrawWindowResizeGrip(ref Rect windowRect, ref bool isResizing)
        {
            float gripSize = 22f;
            Rect gripRect = new Rect(windowRect.width - gripSize - 2, windowRect.height - gripSize - 2, gripSize, gripSize);

            GUI.Label(gripRect, "◢", _resizeGripStyle!);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && gripRect.Contains(e.mousePosition))
            {
                isResizing = true;
                _dragStartPos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                _windowStartSize = new Vector2(windowRect.width, windowRect.height);
                e.Use();
            }
        }

        private bool DrawTabButton(string text, QuestTab tab)
        {
            bool isActive = (_currentTab == tab);
            GUIStyle style = isActive ? _tabActiveStyle! : _tabStyle!;
            return GUILayout.Button(text, style, GUILayout.Height(28));
        }

        private void DrawPrettyQuestCard(QuestDataModel quest, float threshold)
        {
            GUILayout.BeginVertical(_cardBoxStyle);

            // Row 1: Badges
            GUILayout.BeginHorizontal();

            if (quest.IsPinned)
            {
                GUILayout.Label("PINNED", _badgePinnedStyle, GUILayout.ExpandWidth(false), GUILayout.Height(20));
                GUILayout.Space(4);
            }

            if (quest.IsCompleted || quest.IsRewarded)
            {
                GUILayout.Label("COMPLETED", _badgeCompletedStyle, GUILayout.ExpandWidth(false), GUILayout.Height(20));
            }
            else if (quest.Percent >= threshold)
            {
                GUILayout.Label($"ALMOST DONE ({(quest.Percent * 100f):F0}%)", _badgeAlmostDoneStyle, GUILayout.ExpandWidth(false), GUILayout.Height(20));
            }
            else
            {
                GUILayout.Label($"ACTIVE ({(quest.Percent * 100f):F0}%)", _badgeActiveStyle, GUILayout.ExpandWidth(false), GUILayout.Height(20));
            }

            GUILayout.Space(6);
            GUILayout.Label(quest.Title, _questTitleStyle);
            GUILayout.FlexibleSpace();

            // Pin Button ([PIN] / [PINNED])
            GUIStyle pinBtnStyle = quest.IsPinned ? _pinButtonActiveStyle! : _pinButtonStyle!;
            string pinText = quest.IsPinned ? "[PINNED]" : "[PIN]";
            if (GUILayout.Button(pinText, pinBtnStyle, GUILayout.Width(65), GUILayout.Height(20)))
            {
                TogglePinQuest(quest.QuestId);
            }

            GUILayout.Space(8);
            GUILayout.Label($"{quest.CurrentProgress} / {quest.RequiredProgress}", _questRatioStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            // Row 2: Cached Description
            if (!string.IsNullOrEmpty(quest.CachedDescription))
            {
                GUILayout.Label(quest.CachedDescription, _questDescStyle);
                GUILayout.Space(4);
            }

            // Row 3: Progress Bar
            Rect barRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(8), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(barRect, _barBgTexture!);

                float fillW = barRect.width * quest.Percent;
                if (fillW > 0)
                {
                    Rect fillRect = new Rect(barRect.x, barRect.y, fillW, barRect.height);
                    Texture2D fillTex = (quest.IsCompleted || quest.IsRewarded) ? _barFillCompletedTexture!
                                      : (quest.Percent >= threshold) ? _barFillAlmostDoneTexture!
                                      : _barFillActiveTexture!;
                    GUI.DrawTexture(fillRect, fillTex);
                }
            }

            GUILayout.EndVertical();
        }

        private void InitStylesIfNeeded()
        {
            if (_stylesInitialized && _bgTexture != null && _barFillAlmostDoneTexture != null && _cardBgTexture != null && _tabActiveTex != null && _pinActiveTex != null)
            {
                return;
            }
            _stylesInitialized = true;

            Color colBg = new Color(0.06f, 0.08f, 0.12f, 0.95f);
            Color colHudBg = new Color(0.05f, 0.07f, 0.10f, 0.92f);
            Color colCardBg = new Color(0.09f, 0.11f, 0.16f, 0.92f);
            Color colCardBorder = new Color(0.18f, 0.22f, 0.30f, 0.80f);
            Color colBarTrack = new Color(0.14f, 0.17f, 0.23f, 1.0f);

            _bgTexture = MakeTex(2, 2, colBg);
            _hudBgTexture = MakeTex(2, 2, colHudBg);
            _cardBgTexture = MakeBorderTex(4, 4, colCardBg, colCardBorder, 1);
            _barBgTexture = MakeTex(2, 2, colBarTrack);

            _barFillAlmostDoneTexture = MakeGradientTex(2, 16, new Color(1.0f, 0.75f, 0.15f, 1f), new Color(0.95f, 0.50f, 0.05f, 1f));
            _barFillActiveTexture = MakeGradientTex(2, 16, new Color(0.10f, 0.70f, 1.0f, 1f), new Color(0.05f, 0.45f, 0.85f, 1f));
            _barFillCompletedTexture = MakeGradientTex(2, 16, new Color(0.15f, 0.85f, 0.40f, 1f), new Color(0.05f, 0.60f, 0.25f, 1f));

            Color colScrollTrack = new Color(0.08f, 0.10f, 0.16f, 0.85f);
            Color colScrollThumbNormal = new Color(0.24f, 0.35f, 0.52f, 0.90f);
            Color colScrollThumbHover = new Color(0.95f, 0.72f, 0.15f, 1.0f);

            Texture2D scrollTrackTex = MakeTex(2, 2, colScrollTrack);
            Texture2D scrollThumbNormalTex = MakeTex(2, 2, colScrollThumbNormal);
            Texture2D scrollThumbHoverTex = MakeTex(2, 2, colScrollThumbHover);

            GUI.skin.verticalScrollbar.normal.background = scrollTrackTex;
            GUI.skin.verticalScrollbar.hover.background = scrollTrackTex;
            GUI.skin.verticalScrollbar.active.background = scrollTrackTex;
            GUI.skin.verticalScrollbar.fixedWidth = 14f;
            GUI.skin.verticalScrollbar.margin = new RectOffset(0, 0, 0, 0);
            GUI.skin.verticalScrollbar.padding = new RectOffset(1, 1, 1, 1);

            GUI.skin.verticalScrollbarThumb.normal.background = scrollThumbNormalTex;
            GUI.skin.verticalScrollbarThumb.hover.background = scrollThumbHoverTex;
            GUI.skin.verticalScrollbarThumb.active.background = scrollThumbHoverTex;
            GUI.skin.verticalScrollbarThumb.fixedWidth = 12f;

            GUI.skin.verticalScrollbarUpButton.fixedWidth = 0f;
            GUI.skin.verticalScrollbarUpButton.fixedHeight = 0f;
            GUI.skin.verticalScrollbarDownButton.fixedWidth = 0f;
            GUI.skin.verticalScrollbarDownButton.fixedHeight = 0f;

            _tabInactiveTex = MakeTex(2, 2, new Color(0.11f, 0.13f, 0.19f, 0.90f));
            _tabHoverTex    = MakeGradientTex(2, 16, new Color(0.18f, 0.24f, 0.36f, 1.0f), new Color(0.14f, 0.18f, 0.28f, 1.0f));
            _tabActiveTex   = MakeGradientTex(2, 16, new Color(0.20f, 0.45f, 0.85f, 1.0f), new Color(0.12f, 0.30f, 0.65f, 1.0f));

            _pillInactiveTex = MakeTex(2, 2, new Color(0.11f, 0.13f, 0.18f, 0.70f));
            _pillHoverTex    = MakeTex(2, 2, new Color(0.20f, 0.26f, 0.38f, 0.90f));
            _pillActiveTex   = MakeTex(2, 2, new Color(0.95f, 0.72f, 0.15f, 0.95f));

            _pinInactiveTex = MakeTex(2, 2, new Color(0.14f, 0.18f, 0.26f, 0.80f));
            _pinHoverTex    = MakeTex(2, 2, new Color(0.30f, 0.40f, 0.60f, 0.95f));
            _pinActiveTex   = MakeTex(2, 2, new Color(0.95f, 0.65f, 0.10f, 0.95f));

            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.normal.background = _bgTexture;
            _windowStyle.onNormal.background = _bgTexture;
            _windowStyle.padding = new RectOffset(14, 14, 12, 14);

            _hudWindowStyle = new GUIStyle(GUI.skin.window);
            _hudWindowStyle.normal.background = _hudBgTexture;
            _hudWindowStyle.onNormal.background = _hudBgTexture;
            _hudWindowStyle.padding = new RectOffset(10, 10, 8, 8);

            _headerTitleStyle = new GUIStyle(GUI.skin.label);
            _headerTitleStyle.fontSize = 17;
            _headerTitleStyle.fontStyle = FontStyle.Bold;
            _headerTitleStyle.normal.textColor = new Color(1.0f, 0.82f, 0.25f, 1.0f);

            _headerSubStyle = new GUIStyle(GUI.skin.label);
            _headerSubStyle.fontSize = 11;
            _headerSubStyle.normal.textColor = new Color(0.68f, 0.74f, 0.82f, 1.0f);

            _searchStyle = new GUIStyle(GUI.skin.textField);
            _searchStyle.fontSize = 12;
            _searchStyle.normal.textColor = Color.white;
            _searchStyle.padding = new RectOffset(6, 6, 2, 2);

            _tabStyle = CreateCustomStyle(_tabInactiveTex, _tabHoverTex, _tabActiveTex, new Color(0.72f, 0.76f, 0.84f, 1.0f), Color.white, 12, false);
            _tabActiveStyle = CreateCustomStyle(_tabActiveTex, _tabActiveTex, _tabActiveTex, Color.white, Color.white, 12, true);

            _pillStyle = CreateCustomStyle(_pillInactiveTex, _pillHoverTex, _pillActiveTex, new Color(0.72f, 0.76f, 0.84f, 1.0f), Color.white, 11, false);
            _pillActiveStyle = CreateCustomStyle(_pillActiveTex, _pillActiveTex, _pillActiveTex, Color.black, Color.black, 11, true);

            _pinButtonStyle = CreateCustomStyle(_pinInactiveTex, _pinHoverTex, _pinActiveTex, new Color(0.75f, 0.80f, 0.90f, 1.0f), Color.white, 10, false);
            _pinButtonActiveStyle = CreateCustomStyle(_pinActiveTex, _pinActiveTex, _pinActiveTex, Color.black, Color.black, 10, true);

            _cardBoxStyle = new GUIStyle(GUI.skin.box);
            _cardBoxStyle.normal.background = _cardBgTexture;
            _cardBoxStyle.padding = new RectOffset(10, 10, 8, 8);

            _questTitleStyle = new GUIStyle(GUI.skin.label);
            _questTitleStyle.fontSize = 13;
            _questTitleStyle.fontStyle = FontStyle.Bold;
            _questTitleStyle.normal.textColor = Color.white;

            _questRatioStyle = new GUIStyle(GUI.skin.label);
            _questRatioStyle.fontSize = 12;
            _questRatioStyle.fontStyle = FontStyle.Bold;
            _questRatioStyle.normal.textColor = new Color(1.0f, 0.82f, 0.25f, 1.0f);

            _questDescStyle = new GUIStyle(GUI.skin.label);
            _questDescStyle.fontSize = 11;
            _questDescStyle.normal.textColor = new Color(0.78f, 0.82f, 0.88f, 1.0f);

            _footerStyle = new GUIStyle(GUI.skin.label);
            _footerStyle.fontSize = 10;
            _footerStyle.normal.textColor = new Color(0.65f, 0.70f, 0.78f, 1.0f);

            _badgeAlmostDoneStyle = MakeBadgeStyle(new Color(1.0f, 0.65f, 0.10f, 1.0f), Color.black);
            _badgeActiveStyle = MakeBadgeStyle(new Color(0.12f, 0.50f, 0.90f, 1.0f), Color.white);
            _badgeCompletedStyle = MakeBadgeStyle(new Color(0.15f, 0.75f, 0.35f, 1.0f), Color.black);
            _badgePinnedStyle = MakeBadgeStyle(new Color(0.95f, 0.65f, 0.10f, 1.0f), Color.black);

            _hudTitleStyle = new GUIStyle(GUI.skin.label);
            _hudTitleStyle.fontSize = 11;
            _hudTitleStyle.fontStyle = FontStyle.Bold;
            _hudTitleStyle.normal.textColor = new Color(1.0f, 0.82f, 0.25f, 1.0f);

            _hudItemTitleStyle = new GUIStyle(GUI.skin.label);
            _hudItemTitleStyle.fontSize = 11;
            _hudItemTitleStyle.fontStyle = FontStyle.Bold;
            _hudItemTitleStyle.normal.textColor = Color.white;

            _hudItemStyle = new GUIStyle(GUI.skin.label);
            _hudItemStyle.fontSize = 10;
            _hudItemStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f, 1.0f);

            _resizeGripStyle = new GUIStyle(GUI.skin.label);
            _resizeGripStyle.fontSize = 14;
            _resizeGripStyle.alignment = TextAnchor.LowerRight;
            _resizeGripStyle.normal.textColor = new Color(0.65f, 0.72f, 0.85f, 0.80f);
        }

        private GUIStyle CreateCustomStyle(Texture2D normalTex, Texture2D hoverTex, Texture2D activeTex, Color normalTextCol, Color hoverTextCol, int fontSize, bool isBold)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);

            style.normal.background   = normalTex;
            style.hover.background    = hoverTex;
            style.active.background   = activeTex;
            style.onNormal.background = normalTex;
            style.onHover.background  = hoverTex;
            style.onActive.background = activeTex;

            style.normal.textColor   = normalTextCol;
            style.hover.textColor    = hoverTextCol;
            style.active.textColor   = hoverTextCol;
            style.onNormal.textColor = normalTextCol;
            style.onHover.textColor  = hoverTextCol;
            style.onActive.textColor = hoverTextCol;

            style.fontSize = fontSize;
            if (isBold) style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = false;

            return style;
        }

        private GUIStyle MakeBadgeStyle(Color bg, Color text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.background = MakeTex(2, 2, bg);
            style.normal.textColor = text;
            style.fontSize = 10;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(6, 6, 2, 2);
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            return style;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Texture2D result = new Texture2D(width, height);
            result.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeGradientTex(int width, int height, Color top, Color bottom)
        {
            Texture2D result = new Texture2D(width, height);
            result.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Color[] pix = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < width; x++)
                {
                    pix[y * width + x] = c;
                }
            }
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeBorderTex(int width, int height, Color bg, Color border, int borderWidth)
        {
            Texture2D result = new Texture2D(width, height);
            result.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Color[] pix = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x < borderWidth || x >= width - borderWidth || y < borderWidth || y >= height - borderWidth);
                    pix[y * width + x] = isBorder ? border : bg;
                }
            }
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnDestroy()
        {
            try
            {
                if (QuestService.I != null)
                {
                    QuestService.I.OnProgressChanged -= OnQuestProgressUpdated;
                    QuestService.I.OnQuestCompleted -= OnQuestProgressUpdated;
                }
            }
            catch { }
        }
    }
}
