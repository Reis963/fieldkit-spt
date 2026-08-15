
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private bool DrawOptionToggle(
            BepInEx.Configuration.ConfigEntry<bool> setting,
            string label,
            params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            DrawOptionToggleLabel(setting, label);
            DrawOptionHotkey(setting);
            GUILayout.EndHorizontal();
            return setting.Value;
        }

        private bool DrawOptionToggleLabel(
            BepInEx.Configuration.ConfigEntry<bool> setting,
            string label,
            params GUILayoutOption[] options)
        {
            setting.Value = GUILayout.Toggle(
                setting.Value,
                label,
                options);
            return setting.Value;
        }

        private void DrawOptionHotkey(
            BepInEx.Configuration.ConfigEntry<bool> setting)
        {
            HandleInlineToggleHotkey(
                setting,
                GetToggleHotkeyLabel(setting));
        }

        private Vector2 BeginVerticalScrollView(
            Vector2 position,
            params GUILayoutOption[] options)
        {
            return GUILayout.BeginScrollView(
                position,
                false,
                false,
                GUIStyle.none,
                GUI.skin.verticalScrollbar,
                GUI.skin.scrollView,
                options);
        }

        private void EndVerticalScrollView()
        {
            GUILayout.Space(6f);
            GUILayout.EndScrollView();
        }

        private void DrawOptionSlider(
            string label,
            BepInEx.Configuration.ConfigEntry<float> setting,
            float minimum,
            float maximum,
            string format)
        {
            string displayValue = format.EndsWith(
                    "%",
                    System.StringComparison.Ordinal)
                ? setting.Value.ToString(
                      format.Substring(0, format.Length - 1)) + "%"
                : setting.Value.ToString(format);
            GUILayout.BeginHorizontal(GUILayout.Height(18f));
            GUILayout.Label(
                label,
                GUILayout.ExpandWidth(true));
            GUILayout.Label(
                displayValue,
                _sliderValueStyle,
                GUILayout.Width(64f));
            GUILayout.EndHorizontal();
            setting.Value = DrawStyledSlider(
                setting.Value,
                minimum,
                maximum);
            GUILayout.Space(2f);
        }

        private void DrawOptionSlider(
            string label,
            BepInEx.Configuration.ConfigEntry<int> setting,
            int minimum,
            int maximum,
            string format)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(18f));
            GUILayout.Label(
                label,
                GUILayout.ExpandWidth(true));
            GUILayout.Label(
                setting.Value.ToString(format),
                _sliderValueStyle,
                GUILayout.Width(64f));
            GUILayout.EndHorizontal();
            setting.Value = Mathf.RoundToInt(
                DrawStyledSlider(
                    setting.Value,
                    minimum,
                    maximum));
            GUILayout.Space(2f);
        }

        private float DrawStyledSlider(
            float value,
            float minimum,
            float maximum)
        {
            Rect sliderRect = GUILayoutUtility.GetRect(
                1f,
                20f,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(20f));
            Rect trackRect = new Rect(
                sliderRect.x + 2f,
                sliderRect.y + 7f,
                Mathf.Max(1f, sliderRect.width - 4f),
                6f);
            float normalized = Mathf.InverseLerp(
                minimum,
                maximum,
                value);
            if (Event.current.type == EventType.Repaint)
            {
                Color previous = GUI.color;
                if (!GUI.enabled)
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);
                if (_sliderBaseTexture != null)
                    GUI.DrawTexture(trackRect, _sliderBaseTexture);
                if (_sliderFillTexture != null && normalized > 0f)
                {
                    GUI.DrawTexture(
                        new Rect(
                            trackRect.x,
                            trackRect.y,
                            trackRect.width * normalized,
                            trackRect.height),
                        _sliderFillTexture);
                }
                GUI.color = previous;
            }

            value = GUI.HorizontalSlider(
                sliderRect,
                value,
                minimum,
                maximum,
                GUIStyle.none,
                GUI.skin.horizontalSliderThumb);
            return value;
        }

        private int DrawDropdown(
            string id,
            int selectedIndex,
            string[] options)
        {
            selectedIndex = Mathf.Clamp(
                selectedIndex,
                0,
                options.Length - 1);

            bool clicked = GUILayout.Button(
                options[selectedIndex],
                _dropdownButtonStyle);
            Rect anchor = GUILayoutUtility.GetLastRect();
            GUI.Label(
                new Rect(
                    anchor.xMax - 27f,
                    anchor.y,
                    24f,
                    anchor.height),
                "\u25BE",
                _dropdownArrowStyle);

            if (clicked)
            {
                if (_openDropdownId == id)
                    CloseDropdown();
                else
                    _openDropdownId = id;
            }

            if (_openDropdownId == id)
            {
                GUILayout.BeginVertical(_dropdownMenuStyle);
                for (int i = 0; i < options.Length; i++)
                {
                    bool selected = i == selectedIndex;
                    bool next = GUILayout.Toggle(
                        selected,
                        options[i],
                        _dropdownItemStyle);
                    if (next != selected)
                    {
                        if (!selected)
                            selectedIndex = i;
                        CloseDropdown();
                    }
                }
                GUILayout.EndVertical();
            }

            return selectedIndex;
        }

        private void CloseDropdown()
        {
            _openDropdownId = null;
        }
    }
}
