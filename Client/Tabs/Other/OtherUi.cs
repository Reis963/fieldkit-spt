
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawOtherMenu()
        {
            _otherMenuScroll = BeginVerticalScrollView(
                _otherMenuScroll);

            BeginCategoryColumns();
            BeginCategoryPanel("Menu customization");

            int menuFontIndex = FindEspFontIndex(
                _menuFontName.Value);
            int nextMenuFontIndex = DrawDropdown(
                "menu-font",
                menuFontIndex,
                EspFontNames);
            if (nextMenuFontIndex != menuFontIndex)
                _menuFontName.Value =
                    EspFontNames[nextMenuFontIndex];

            float maximumUiScale = MaximumMenuScale;
            _pendingMenuUiScale = Mathf.Clamp(
                _pendingMenuUiScale,
                0.5f,
                maximumUiScale);
            GUILayout.BeginHorizontal(GUILayout.Height(18f));
            GUILayout.Label(
                "UI scale (max " +
                maximumUiScale.ToString("0.00") + "x)",
                GUILayout.ExpandWidth(true));
            GUILayout.Label(
                _pendingMenuUiScale.ToString("0.0") + "x",
                _sliderValueStyle,
                GUILayout.Width(64f));
            GUILayout.EndHorizontal();
            _pendingMenuUiScale = DrawStyledSlider(
                _pendingMenuUiScale,
                0.5f,
                maximumUiScale);
            GUILayout.BeginHorizontal();
            GUI.enabled = !Mathf.Approximately(
                _menuUiScale.Value,
                _pendingMenuUiScale);
            if (GUILayout.Button("Apply UI scale"))
                _menuUiScale.Value = _pendingMenuUiScale;
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                "Primary color",
                GUILayout.ExpandWidth(true));
            DrawColorSquare(
                _guiPrimaryColor,
                ParseVisualColor(
                    _guiPrimaryColor.Value,
                    new Color32(24, 215, 164, 255)),
                new Color32(24, 215, 164, 255),
                "Menu primary color");
            DrawHotkeyColumnSpacer();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                "Open / close menu",
                GUILayout.ExpandWidth(true));
            DrawStandaloneHotkey(_menuKey);
            GUILayout.EndHorizontal();

            if (DrawResetGroupButton())
            {
                _guiPrimaryColor.Value = "#18D7A4FF";
                _menuFontName.Value = "Segoe UI";
                _menuUiScale.Value = 1f;
                _pendingMenuUiScale = 1f;
                _menuKey.Value =
                    new KeyboardShortcut(KeyCode.Insert);
            }

            EndCategoryPanel();
            EndCategoryColumns();

            EndVerticalScrollView();
        }
    }
}
