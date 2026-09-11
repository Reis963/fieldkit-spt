
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private ConfigEntry<bool> _questItemHighlighting;
        private ConfigEntry<bool> _questLocationHighlighting;
        private ConfigEntry<bool> _questVisitLocations;
        private ConfigEntry<bool> _questPlaceLocations;
        private ConfigEntry<bool> _questShowObjective;
        private ConfigEntry<bool> _questShowQuestName;
        private ConfigEntry<bool> _questShowDistance;
        private ConfigEntry<float> _questItemRenderDistance;
        private ConfigEntry<int> _questItemLabelSize;
        private ConfigEntry<string> _questItemColor;
        private ConfigEntry<string> _questVisitColor;
        private ConfigEntry<string> _questPlaceColor;
        private Vector2 _questMenuScroll;

        private void ConfigureQuestTools()
        {
            _questItemHighlighting = Config.Bind(
                "Quest Items",
                "Highlighting Enabled",
                false,
                "Highlight loose items required by active quests.");
            _questItemColor = Config.Bind(
                "Quest Items",
                "Highlight Color",
                "#C084FCFF",
                "RGBA color used for quest-item markers and labels.");

            _questLocationHighlighting = Config.Bind(
                "Quest Locations",
                "Highlighting Enabled",
                false,
                "Highlight incomplete locations for active quests.");
            _questVisitLocations = Config.Bind(
                "Quest Locations",
                "Visit Locations",
                true,
                "Highlight locations that still need to be visited.");
            _questPlaceLocations = Config.Bind(
                "Quest Locations",
                "Place Or Repair Locations",
                true,
                "Highlight locations where an item must be placed or repaired.");
            _questVisitColor = Config.Bind(
                "Quest Locations",
                "Visit Color",
                "#42D6FFFF",
                "RGBA color used for visit-location markers and labels.");
            _questPlaceColor = Config.Bind(
                "Quest Locations",
                "Place Or Repair Color",
                "#FFB347FF",
                "RGBA color used for place-or-repair markers and labels.");

            _questShowObjective = Config.Bind(
                "Quest Labels",
                "Show Objective",
                true,
                "Show the incomplete objective description.");
            _questShowQuestName = Config.Bind(
                "Quest Labels",
                "Show Quest Name",
                true,
                "Show the quest name below its objective.");
            _questShowDistance = Config.Bind(
                "Quest Labels",
                "Show Distance",
                true,
                "Show the distance to each quest marker.");
            _questItemRenderDistance = Config.Bind(
                "Quest Labels",
                "Render Distance",
                500f,
                new ConfigDescription(
                    "Maximum distance for quest markers.",
                    new AcceptableValueRange<float>(10f, 1500f)));
            _questItemLabelSize = Config.Bind(
                "Quest Labels",
                "Label Size",
                12,
                new ConfigDescription(
                    "Font size used by quest-marker labels.",
                    new AcceptableValueRange<int>(8, 24)));

            SubscribeQuestSettings();
        }

        private void DrawQuestMenu()
        {
            _questMenuScroll = BeginVerticalScrollView(
                _questMenuScroll,
                GUILayout.Height(MenuContentHeight));

            BeginCategoryColumns();
            BeginCategoryPanel("Quest items");
            GUILayout.BeginHorizontal();
            DrawOptionToggleLabel(
                _questItemHighlighting,
                " Highlight required items",
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                _questItemColor,
                GetQuestMarkerColor(QuestMarkerKind.Item),
                new Color(0.75f, 0.52f, 0.99f, 1f),
                "Quest-item highlight");
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
            GUILayout.Label(
                "Only loose items required by incomplete objectives of active quests are shown.");
            if (DrawResetGroupButton())
            {
                _questItemHighlighting.Value = false;
                _questItemColor.Value = "#C084FCFF";
            }
            EndCategoryPanel();

            GUILayout.Space(8f);
            BeginCategoryPanel("Quest locations");
            DrawOptionToggle(
                _questLocationHighlighting,
                " Highlight quest locations");
            GUI.enabled = _questLocationHighlighting.Value;

            GUILayout.BeginHorizontal();
            DrawOptionToggleLabel(
                _questVisitLocations,
                " Visit locations",
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                _questVisitColor,
                GetQuestMarkerColor(QuestMarkerKind.Visit),
                new Color(0.26f, 0.84f, 1f, 1f),
                "Visit-location highlight");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawOptionToggleLabel(
                _questPlaceLocations,
                " Place / repair locations",
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                _questPlaceColor,
                GetQuestMarkerColor(QuestMarkerKind.Place),
                new Color(1f, 0.7f, 0.28f, 1f),
                "Place-or-repair highlight");
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUI.enabled = _world != null && _localPlayer != null;
            if (GUILayout.Button("Scan quest locations now"))
                RefreshQuestTriggerCaches();
            GUI.enabled = true;
            GUILayout.Label("Scans at raid start and once after 5 seconds. Use this button for zones added later.");

            if (DrawResetGroupButton())
            {
                _questLocationHighlighting.Value = false;
                _questVisitLocations.Value = true;
                _questPlaceLocations.Value = true;
                _questVisitColor.Value = "#42D6FFFF";
                _questPlaceColor.Value = "#FFB347FF";
            }
            EndCategoryPanel();

            NextCategoryColumn();
            BeginCategoryPanel("Labels");
            DrawOptionToggle(
                _questShowObjective,
                " Show objective");
            DrawOptionToggle(
                _questShowQuestName,
                " Show quest name");
            DrawOptionToggle(
                _questShowDistance,
                " Show distance");
            if (DrawResetGroupButton())
            {
                _questShowObjective.Value = true;
                _questShowQuestName.Value = true;
                _questShowDistance.Value = true;
            }
            EndCategoryPanel();

            GUILayout.Space(8f);
            BeginCategoryPanel("Appearance");
            DrawOptionSlider(
                "Render distance",
                _questItemRenderDistance,
                10f,
                1500f,
                "0m");
            DrawOptionSlider(
                "Label size",
                _questItemLabelSize,
                8,
                24,
                "0");
            if (DrawResetGroupButton())
            {
                _questItemRenderDistance.Value = 500f;
                _questItemLabelSize.Value = 12;
            }
            EndCategoryPanel();
            EndCategoryColumns();

            EndVerticalScrollView();
        }

        private bool IsQuestOverlayEnabled()
        {
            return (_questItemHighlighting != null &&
                    _questItemHighlighting.Value) ||
                   (_questLocationHighlighting != null &&
                    _questLocationHighlighting.Value &&
                    ((_questVisitLocations != null &&
                      _questVisitLocations.Value) ||
                     (_questPlaceLocations != null &&
                      _questPlaceLocations.Value)));
        }

        private Color GetQuestMarkerColor(QuestMarkerKind kind)
        {
            switch (kind)
            {
                case QuestMarkerKind.Visit:
                    return ParseVisualColor(
                        _questVisitColor.Value,
                        new Color(0.26f, 0.84f, 1f, 1f));
                case QuestMarkerKind.Place:
                    return ParseVisualColor(
                        _questPlaceColor.Value,
                        new Color(1f, 0.7f, 0.28f, 1f));
                default:
                    return ParseVisualColor(
                        _questItemColor.Value,
                        new Color(0.75f, 0.52f, 0.99f, 1f));
            }
        }

        private void SubscribeQuestSettings()
        {
            _questItemHighlighting.SettingChanged +=
                OnQuestSettingChanged;
            _questLocationHighlighting.SettingChanged +=
                OnQuestSettingChanged;
            _questVisitLocations.SettingChanged +=
                OnQuestSettingChanged;
            _questPlaceLocations.SettingChanged +=
                OnQuestSettingChanged;
            _questShowObjective.SettingChanged +=
                OnQuestSettingChanged;
            _questShowQuestName.SettingChanged +=
                OnQuestSettingChanged;
        }

        private void UnsubscribeQuestSettings()
        {
            if (_questItemHighlighting != null)
                _questItemHighlighting.SettingChanged -=
                    OnQuestSettingChanged;
            if (_questLocationHighlighting != null)
                _questLocationHighlighting.SettingChanged -=
                    OnQuestSettingChanged;
            if (_questVisitLocations != null)
                _questVisitLocations.SettingChanged -=
                    OnQuestSettingChanged;
            if (_questPlaceLocations != null)
                _questPlaceLocations.SettingChanged -=
                    OnQuestSettingChanged;
            if (_questShowObjective != null)
                _questShowObjective.SettingChanged -=
                    OnQuestSettingChanged;
            if (_questShowQuestName != null)
                _questShowQuestName.SettingChanged -=
                    OnQuestSettingChanged;
        }

        private void OnQuestSettingChanged(
            object sender,
            EventArgs args)
        {
            InvalidateQuestObjectiveData();
            _lastRenderFrame = -1;

            if ((_enabled == null || !_enabled.Value) &&
                !IsQuestOverlayEnabled())
                ClearOverlay();
        }
    }
}
