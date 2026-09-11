
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private Vector2 _espMenuScroll;
        private ConfigEntry<string> _openColorSetting;
        private string _openColorLabel;
        private Color _openColorFallback;
        private GUIStyle _espRoleFoldoutStyle;

        private void DrawEspMenu()
        {
            _espMenuScroll = BeginVerticalScrollView(
                    _espMenuScroll,
                    GUILayout.Height(MenuContentHeight));

            BeginCategoryColumns();
            BeginCategoryPanel("ESP Targets");
            DrawOptionToggle(_enabled, " Enable ESP");
            DrawColorColumnHeaders();
            DrawAllRoleRow();
            for (int i = 0; i < _espRoleGroups.Count; i++)
                DrawRoleGroup(_espRoleGroups[i]);
            DrawOptionToggle(_showBoxes, " Show Boxes");
            DrawOptionToggle(
                _visibilityCheck, " Visibility Check");
            DrawOptionToggle(_scopeEsp, " Scope ESP");
            if (DrawResetGroupButton())
            {
                _enabled.Value = true;
                for (int i = 0; i < _espRoles.Count; i++)
                {
                    EspRoleSettings role = _espRoles[i];
                    role.Enabled.Value = true;
                    role.VisibleColor.Value =
                        "#" + ColorUtility.ToHtmlStringRGBA(
                            role.DefaultVisible);
                    role.HiddenColor.Value =
                        "#" + ColorUtility.ToHtmlStringRGBA(
                            role.DefaultHidden);
                }
                _showBoxes.Value = true;
                _visibilityCheck.Value = true;
                _scopeEsp.Value = true;
                ResetVisualColors();
            }
            EndCategoryPanel();

            BeginCategoryPanel("ESP Information");
            DrawOptionToggle(_showHealthBar, " Show Health Bar");
            DrawOptionToggle(
                _showHealthPercentage, " Show Health Percentage");
            DrawOptionToggle(_showPlayerName, " Show Player Name");
            DrawOptionToggle(_showRole, " Show Role");
            DrawOptionToggle(_showWeapon, " Show Weapon");
            DrawOptionToggle(_showDistance, " Show Distance");
            if (DrawResetGroupButton())
            {
                _showHealthBar.Value = true;
                _showHealthPercentage.Value = true;
                _showPlayerName.Value = true;
                _showRole.Value = true;
                _showWeapon.Value = true;
                _showDistance.Value = true;
            }
            EndCategoryPanel();

            NextCategoryColumn();
            BeginCategoryPanel("ESP Geometry & Text");
            DrawOptionSlider(
                "Distance", _maxDistance, 25f, 1500f, "0m");
            DrawOptionSlider(
                "Box thickness", _lineThickness, 1f, 8f, "0.0");
            DrawOptionSlider(
                "Scope color", _scopeColorBrightness,
                0.5f, 2f, "0.00");

            int fontIndex = FindEspFontIndex(_espFontName.Value);
            int nextFontIndex = DrawDropdown(
                "esp-font",
                fontIndex,
                EspFontNames);
            if (nextFontIndex != fontIndex)
                _espFontName.Value = EspFontNames[nextFontIndex];
            DrawOptionSlider(
                "Text size", _fontSize, 9, 32, "0");
            DrawOptionSlider(
                "Text outline",
                _textOutlineThickness, 0f, 3f, "0.0");
            if (DrawResetGroupButton())
            {
                _maxDistance.Value = 500f;
                _lineThickness.Value = 2f;
                _scopeColorBrightness.Value = 0.5f;
                _fontSize.Value = 13;
                _espFontName.Value = "Segoe UI";
                _textOutlineThickness.Value = 1f;
            }
            EndCategoryPanel();
            EndCategoryColumns();

            EndVerticalScrollView();
        }

        private void DrawRoleColorRow(EspRoleSettings role)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(
                role.FollowerSubcategory
                    ? 68f
                    : 42f);
            DrawOptionToggleLabel(
                role.Enabled,
                " " + RoleLeafName(role),
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                role.VisibleColor,
                ParseVisualColor(
                    role.VisibleColor.Value,
                    role.DefaultVisible),
                role.DefaultVisible,
                role.Label + " visible");
            DrawColorSquare(
                role.HiddenColor,
                ParseVisualColor(
                    role.HiddenColor.Value,
                    role.DefaultHidden),
                role.DefaultHidden,
                role.Label + " hidden");
            DrawOptionHotkey(role.Enabled);
            GUILayout.EndHorizontal();
        }

        private void DrawAllRoleRow()
        {
            int selected = CountEnabledRoles(_espRoles);
            bool all = selected == _espRoles.Count && _espRoles.Count > 0;
            bool any = selected > 0;

            GUILayout.BeginHorizontal();
            if (DrawRoleFoldoutButton(_espAllRolesExpanded))
            {
                _espAllRolesExpanded = !_espAllRolesExpanded;
                for (int i = 0; i < _espRoleGroups.Count; i++)
                    _espRoleGroups[i].Expanded = _espAllRolesExpanded;
            }
            bool toggled = GUILayout.Toggle(
                all,
                (any && !all ? " Some" : " All") +
                " roles (" + selected + "/" + _espRoles.Count + ")",
                GUILayout.ExpandWidth(true));
            if (toggled != all)
                SetRolesEnabled(_espRoles, toggled);
            GUILayout.Space(52f);
            GUILayout.Space(52f);
            GUILayout.Space(76f);
            GUILayout.EndHorizontal();
        }

        private void DrawRoleGroup(EspRoleGroup group)
        {
            int selected = CountEnabledRoles(group.Roles);
            bool all = selected == group.Roles.Count &&
                       group.Roles.Count > 0;
            bool any = selected > 0;

            GUILayout.BeginHorizontal();
            GUILayout.Space(13f);
            if (DrawRoleFoldoutButton(group.Expanded))
                group.Expanded = !group.Expanded;
            bool toggled = GUILayout.Toggle(
                all,
                (any && !all ? " Some " : " ") +
                group.Name + " (" + selected + "/" +
                group.Roles.Count + ")",
                GUILayout.ExpandWidth(true));
            if (toggled != all)
                SetRolesEnabled(group.Roles, toggled);
            GUILayout.Space(52f);
            GUILayout.Space(52f);
            GUILayout.Space(76f);
            GUILayout.EndHorizontal();

            if (!group.Expanded)
                return;
            for (int i = 0; i < group.Roles.Count; i++)
                DrawRoleColorRow(group.Roles[i]);
        }

        private bool DrawRoleFoldoutButton(bool expanded)
        {
            if (_espRoleFoldoutStyle == null)
            {
                _espRoleFoldoutStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fixedWidth = 25f,
                        fixedHeight = 24f,
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(0, 0, 0, 1),
                        margin = new RectOffset(2, 4, 1, 1)
                    };
            }
            return GUILayout.Button(
                expanded ? "\u25BC" : "\u25B6",
                _espRoleFoldoutStyle);
        }

        private static int CountEnabledRoles(
            System.Collections.Generic.IList<EspRoleSettings> roles)
        {
            int count = 0;
            for (int i = 0; i < roles.Count; i++)
            {
                if (roles[i].Enabled.Value)
                    count++;
            }
            return count;
        }

        private static void SetRolesEnabled(
            System.Collections.Generic.IList<EspRoleSettings> roles,
            bool enabled)
        {
            for (int i = 0; i < roles.Count; i++)
                roles[i].Enabled.Value = enabled;
        }

        private static string RoleLeafName(EspRoleSettings role)
        {
            string prefix = role.Group + " - ";
            string leaf = role.Label.StartsWith(
                    prefix, System.StringComparison.Ordinal)
                ? role.Label.Substring(prefix.Length)
                : role.Label;
            return role.FollowerSubcategory
                ? "Follower / " + leaf
                : leaf;
        }

        private void DrawColorColumnHeaders()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                "",
                GUILayout.ExpandWidth(true));
            GUILayout.Label(
                "Visible",
                _sliderValueStyle,
                GUILayout.Width(52f));
            GUILayout.Label(
                "Hidden",
                _sliderValueStyle,
                GUILayout.Width(52f));
            GUILayout.Label(
                "Hotkey",
                _sliderValueStyle,
                GUILayout.Width(76f));
            GUILayout.EndHorizontal();
        }

        private void DrawColorSquare(
            ConfigEntry<string> setting,
            Color color,
            Color fallback,
            string label)
        {
            Rect slot = GUILayoutUtility.GetRect(
                52f, 26f, GUILayout.Width(52f), GUILayout.Height(26f));
            Rect rect = new Rect(
                slot.x + 13f, slot.y, 26f, 26f);
            if (GUI.Button(
                rect,
                GUIContent.none,
                _colorChipStyle))
            {
                _openColorSetting = setting;
                _openColorLabel = label;
                _openColorFallback = fallback;
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(rect.x + 5f, rect.y + 5f, 16f, 16f),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawColorPickerPopout()
        {
            if (_openColorSetting == null)
                return;

            _colorPickerRect.width = 360f;
            _colorPickerRect.height = 300f;
            _colorPickerRect.x = Mathf.Clamp(
                _colorPickerRect.x,
                0f,
                Mathf.Max(
                    0f,
                    VirtualScreenWidth - _colorPickerRect.width));
            _colorPickerRect.y = Mathf.Clamp(
                _colorPickerRect.y,
                0f,
                Mathf.Max(
                    0f,
                    VirtualScreenHeight - _colorPickerRect.height));
            _colorPickerRect = GUI.Window(
                731909,
                _colorPickerRect,
                DrawColorPickerWindow,
                "");
        }

        private void DrawColorPickerWindow(int windowId)
        {
            GUILayout.BeginHorizontal(
                _pickerHeaderStyle,
                GUILayout.Height(27f));
            GUILayout.Label(
                _openColorLabel,
                _menuTitleStyle,
                GUILayout.ExpandWidth(true));
            if (GUILayout.Button(
                    "\u00D7",
                    _closeButtonStyle,
                    GUILayout.Width(24f)))
                _openColorSetting = null;
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            if (_openColorSetting != null)
            {
                if (DrawRgbaColorPicker(
                        "RGBA", _openColorSetting, _openColorFallback))
                    ApplyConfiguredTargetColors();

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(
                        "Reset color",
                        GUILayout.Width(104f)))
                {
                    _openColorSetting.Value = "#" +
                        ColorUtility.ToHtmlStringRGBA(
                            _openColorFallback);
                    ApplyConfiguredTargetColors();
                }
                GUILayout.EndHorizontal();
            }

            GUI.DragWindow(
                new Rect(0f, 0f, _colorPickerRect.width - 28f, 29f));
        }

        private bool DrawRgbaColorPicker(
            string label,
            ConfigEntry<string> setting,
            Color fallback)
        {
            Color color = ParseVisualColor(setting.Value, fallback);
            GUILayout.Label(
                label + "  #" + ColorUtility.ToHtmlStringRGBA(color),
                _sectionTitleStyle);

            Rect preview = GUILayoutUtility.GetRect(
                1f, 16f, GUILayout.ExpandWidth(true));
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(preview, Texture2D.whiteTexture);
            GUI.color = previousColor;

            float red = DrawColorChannel("Red", color.r);
            float green = DrawColorChannel("Green", color.g);
            float blue = DrawColorChannel("Blue", color.b);
            float alpha = DrawColorChannel("Alpha", color.a);
            Color updated = new Color(red, green, blue, alpha);

            if (ApproximatelyEqual(color, updated))
                return false;

            setting.Value = "#" + ColorUtility.ToHtmlStringRGBA(updated);
            return true;
        }

        private float DrawColorChannel(string label, float value)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(18f));
            GUILayout.Label(label, GUILayout.ExpandWidth(true));
            GUILayout.Label(
                Mathf.RoundToInt(value * 255f).ToString(),
                _sliderValueStyle,
                GUILayout.Width(48f));
            GUILayout.EndHorizontal();
            return DrawStyledSlider(
                value,
                0f,
                1f);
        }

        private static bool ApproximatelyEqual(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) &&
                   Mathf.Approximately(left.g, right.g) &&
                   Mathf.Approximately(left.b, right.b) &&
                   Mathf.Approximately(left.a, right.a);
        }

        private Color GetVisualColor(EspKind kind, bool occluded = false)
        {
            ConfigEntry<string> setting = occluded
                ? GetOccludedColorSetting(kind)
                : GetVisibleColorSetting(kind);
            Color fallback = occluded
                ? GetOccludedFallback(kind)
                : GetVisualFallback(kind);
            return setting != null
                ? ParseVisualColor(setting.Value, fallback)
                : fallback;
        }

        private ConfigEntry<string> GetVisibleColorSetting(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return _pmcVisualColor;
                case EspKind.Scav:
                    return _scavVisualColor;
                case EspKind.Boss:
                    return _bossVisualColor;
                default:
                    return null;
            }
        }

        private ConfigEntry<string> GetOccludedColorSetting(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return _pmcOccludedColor;
                case EspKind.Scav:
                    return _scavOccludedColor;
                case EspKind.Boss:
                    return _bossOccludedColor;
                default:
                    return null;
            }
        }

        private static Color GetVisualFallback(EspKind kind)
        {
            switch (kind)
            {
                case EspKind.Pmc:
                    return new Color(1f, 0.25f, 0.25f, 1f);
                case EspKind.Scav:
                    return new Color(1f, 0.85f, 0.1f, 1f);
                case EspKind.Boss:
                    return new Color(1f, 0.15f, 0.9f, 1f);
                default:
                    return Color.white;
            }
        }

        private static Color GetOccludedFallback(EspKind kind)
        {
            Color visible = GetVisualFallback(kind);
            return new Color(
                visible.r * 0.3f,
                visible.g * 0.3f,
                visible.b * 0.3f,
                visible.a * 0.75f);
        }

        private void ResetVisualColors()
        {
            _pmcVisualColor.Value = "#FF4040FF";
            _scavVisualColor.Value = "#FFD91AFF";
            _bossVisualColor.Value = "#FF26E6FF";
            _pmcOccludedColor.Value = "#4D1313BF";
            _scavOccludedColor.Value = "#4D4108BF";
            _bossOccludedColor.Value = "#4D0745BF";
            ApplyConfiguredTargetColors();
        }

        private static Color ParseVisualColor(
            string value,
            Color fallback)
        {
            Color parsed;
            return !string.IsNullOrEmpty(value) &&
                   ColorUtility.TryParseHtmlString(value, out parsed)
                ? parsed
                : fallback;
        }

        private void ApplyConfiguredTargetColors()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                Target target = _targets[i];
                target.Color = GetRoleColor(target, false);
                target.DisplayColor = GetDisplayColor(target);
            }
        }

    }
}
