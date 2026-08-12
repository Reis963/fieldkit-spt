
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private static readonly string[] MenuTabs =
        {
            "Character",
            "Entities",
            "Quests",
            "Other"
        };

        private const float PreferredMenuWidth = 880f;
        private const float PreferredMenuHeight = 660f;
        private const float MinimumMenuWidth = 700f;
        private const float MinimumMenuHeight = 500f;
        private const float MenuSidebarWidth = 152f;
        private float MaximumMenuScale =>
            Mathf.Max(
                0.5f,
                Mathf.Min(
                    Screen.width / (PreferredMenuWidth + 32f),
                    Screen.height / (PreferredMenuHeight + 32f)));
        private float MenuScale =>
            _menuUiScale == null
                ? 1f
                : Mathf.Clamp(
                    _menuUiScale.Value,
                    0.5f,
                    MaximumMenuScale);
        private float VirtualScreenWidth =>
            Screen.width / MenuScale;
        private float VirtualScreenHeight =>
            Screen.height / MenuScale;
        private float MenuWidth =>
            Mathf.Clamp(
                VirtualScreenWidth - 32f,
                Mathf.Min(MinimumMenuWidth, VirtualScreenWidth),
                PreferredMenuWidth);
        private float CurrentMenuWidth =>
            Mathf.Clamp(
                VirtualScreenWidth - 32f,
                Mathf.Min(MinimumMenuWidth, VirtualScreenWidth),
                PreferredMenuWidth);
        private float MenuHeight =>
            Mathf.Clamp(
                VirtualScreenHeight - 32f,
                Mathf.Min(MinimumMenuHeight, VirtualScreenHeight),
                PreferredMenuHeight);
        private float MenuColumnWidth =>
            Mathf.Max(420f, MenuWidth - MenuSidebarWidth - 58f);
        private float MenuContentHeight =>
            Mathf.Max(300f, MenuHeight - 84f);
        private ConfigEntry<float> _menuWindowX;
        private ConfigEntry<float> _menuWindowY;
        private ConfigEntry<float> _colorPickerWindowX;
        private ConfigEntry<float> _colorPickerWindowY;
        private ConfigEntry<string> _guiPrimaryColor;
        private ConfigEntry<int> _savedMenuTab;
        private int _menuTab;
        private bool _menuOpen;
        private Rect _menuRect =
            new Rect(
                30f,
                30f,
                PreferredMenuWidth,
                PreferredMenuHeight);
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;
        private Texture2D _menuCursorTexture;
        private bool _menuCursorApplied;
        private UnityEngine.EventSystems.EventSystem
            _blockedEventSystem;
        private bool _blockedEventSystemWasEnabled;
        private GUISkin _adminSkin;
        private GUIStyle _tabStyle;
        private GUIStyle _selectedTabStyle;
        private GUIStyle _menuHeaderStyle;
        private GUIStyle _menuTitleStyle;
        private GUIStyle _menuSubtitleStyle;
        private GUIStyle _sidebarStyle;
        private GUIStyle _sidebarHeaderStyle;
        private GUIStyle _contentPaneStyle;
        private GUIStyle _pageTitleStyle;
        private GUIStyle _closeButtonStyle;
        private GUIStyle _sliderValueStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _resetButtonStyle;
        private GUIStyle _dropdownButtonStyle;
        private GUIStyle _dropdownArrowStyle;
        private GUIStyle _dropdownMenuStyle;
        private GUIStyle _dropdownItemStyle;
        private GUIStyle _optionTooltipStyle;
        private string _pendingOptionTooltip;
        private float _optionTooltipHoverStarted;
        private bool _categoryResetRequested;
        private string _openDropdownId;
        private Rect _colorPickerRect =
            new Rect(805f, 270f, 390f, 250f);
        private readonly List<Texture2D> _themeTextures =
            new List<Texture2D>(16);

        private void EnsureMenuCursorTexture()
        {
            if (_menuCursorTexture != null)
                return;

            const int width = 14;
            const int height = 21;
            bool[,] fill = new bool[width, height];
            Vector2[] shape =
            {
                new Vector2(1f, 1f),
                new Vector2(1f, 17f),
                new Vector2(5f, 13f),
                new Vector2(8f, 20f),
                new Vector2(11f, 19f),
                new Vector2(7f, 12f),
                new Vector2(13f, 12f)
            };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = false;
                    for (int current = 0, previous = shape.Length - 1;
                         current < shape.Length;
                         previous = current++)
                    {
                        Vector2 a = shape[current];
                        Vector2 b = shape[previous];
                        if ((a.y > y) != (b.y > y) &&
                            x < (b.x - a.x) * (y - a.y) /
                                (b.y - a.y) + a.x)
                            inside = !inside;
                    }
                    fill[x, y] = inside;
                }
            }

            Color32[] pixels = new Color32[width * height];
            Color32 outline = new Color32(8, 10, 14, 255);
            Color32 fillColor =
                new Color32(242, 246, 252, 255);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool filled = fill[x, y];
                    bool bordered = false;
                    if (!filled)
                    {
                        for (int offsetY = -1;
                             offsetY <= 1 && !bordered;
                             offsetY++)
                        {
                            for (int offsetX = -1;
                                 offsetX <= 1;
                                 offsetX++)
                            {
                                int sampleX = x + offsetX;
                                int sampleY = y + offsetY;
                                if (sampleX >= 0 &&
                                    sampleX < width &&
                                    sampleY >= 0 &&
                                    sampleY < height &&
                                    fill[sampleX, sampleY])
                                {
                                    bordered = true;
                                    break;
                                }
                            }
                        }
                    }

                    pixels[
                        (height - 1 - y) * width + x] =
                        filled
                            ? fillColor
                            : bordered
                                ? outline
                                : new Color32(0, 0, 0, 0);
                }
            }

            _menuCursorTexture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false)
            {
                name = "FieldKit Menu Cursor",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            _menuCursorTexture.SetPixels32(pixels);
            _menuCursorTexture.Apply(false, false);
            _themeTextures.Add(_menuCursorTexture);
        }

        private void ConfigureGuiSettings()
        {
            _menuWindowX = Config.Bind(
                "GUI Layout", "Main Window X", 30f,
                "Saved horizontal position of the main admin window.");
            _menuWindowY = Config.Bind(
                "GUI Layout", "Main Window Y", 30f,
                "Saved vertical position of the main admin window.");
            _colorPickerWindowX = Config.Bind(
                "GUI Layout", "Color Picker X", 805f,
                "Saved horizontal position of the color picker.");
            _colorPickerWindowY = Config.Bind(
                "GUI Layout", "Color Picker Y", 270f,
                "Saved vertical position of the color picker.");
            _guiPrimaryColor = Config.Bind(
                "GUI Appearance", "Primary Color", "#78CFF5FF",
                "Primary RGBA accent color used by the FieldKit menu.");
            _guiPrimaryColor.SettingChanged +=
                OnGuiPrimaryColorChanged;
            _savedMenuTab = Config.Bind(
                "GUI Layout", "Selected Tab", 0,
                new ConfigDescription(
                    "Last selected admin-tools tab.",
                    new AcceptableValueRange<int>(
                        0,
                        MenuTabs.Length - 1)));

            _menuRect.x = _menuWindowX.Value;
            _menuRect.y = _menuWindowY.Value;
            _colorPickerRect.x = _colorPickerWindowX.Value;
            _colorPickerRect.y = _colorPickerWindowY.Value;
            _menuTab = Mathf.Clamp(
                _savedMenuTab.Value,
                0,
                MenuTabs.Length - 1);
        }

        private void DrawMenu(int windowId)
        {
            GUILayout.BeginHorizontal(
                _menuHeaderStyle,
                GUILayout.Height(30f));
            GUILayout.Label("FieldKit", _menuTitleStyle);
            GUILayout.Label(
                "Developer Tools for SPT",
                _menuSubtitleStyle,
                GUILayout.ExpandWidth(true));
            if (GUILayout.Button(
                    new GUIContent("×", "Close menu"),
                    _closeButtonStyle))
                SetMenuOpen(false);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(
                _sidebarStyle,
                GUILayout.Width(MenuSidebarWidth),
                GUILayout.ExpandHeight(true));
            GUILayout.Label("NAVIGATION", _sidebarHeaderStyle);
            for (int i = 0; i < MenuTabs.Length; i++)
            {
                GUIStyle style = i == _menuTab
                    ? _selectedTabStyle
                    : _tabStyle;
                if (!GUILayout.Button(MenuTabs[i], style))
                    continue;

                _menuTab = i;
                _savedMenuTab.Value = _menuTab;
                CloseDropdown();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("SPT 4.0.13", _sidebarHeaderStyle);
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.BeginVertical(
                _contentPaneStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            GUILayout.Label(MenuTabs[_menuTab], _pageTitleStyle);

            switch (_menuTab)
            {
                case 1:
                    DrawEntityMenu();
                    break;
                case 2:
                    DrawQuestMenu();
                    break;
                case 3:
                    DrawOtherMenu();
                    break;
                default:
                    DrawCharacterMenu();
                    break;
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            DrawOptionTooltip();
            GUI.DragWindow(new Rect(0f, 0f, _menuRect.width - 38f, 36f));
        }

        private void UpdateMenuGeometry()
        {
            _menuRect.width = CurrentMenuWidth;
            _menuRect.height = MenuHeight;
            _menuRect.x = Mathf.Clamp(
                _menuRect.x,
                0f,
                Mathf.Max(
                    0f,
                    VirtualScreenWidth - _menuRect.width));
            _menuRect.y = Mathf.Clamp(
                _menuRect.y,
                0f,
                Mathf.Max(
                    0f,
                    VirtualScreenHeight - _menuRect.height));
        }

        private void BeginCategoryColumns()
        {
            GUILayout.BeginVertical(
                GUILayout.Width(MenuColumnWidth));
        }

        private void NextCategoryColumn()
        {
            GUILayout.Space(4f);
        }

        private static void EndCategoryColumns()
        {
            GUILayout.EndVertical();
        }

        private void BeginCategoryPanel(
            string title,
            bool showResetButton = true)
        {
            GUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.Width(MenuColumnWidth));
            _categoryResetRequested = false;

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                title,
                _sectionTitleStyle,
                GUILayout.ExpandWidth(true));
            if (showResetButton)
            {
                _categoryResetRequested = GUILayout.Button(
                    new GUIContent("\u21BB", "Reset group"),
                    _resetButtonStyle,
                    GUILayout.Width(28f),
                    GUILayout.Height(28f));
            }
            GUILayout.EndHorizontal();
        }

        private static void EndCategoryPanel()
        {
            GUILayout.EndVertical();
        }

        private bool DrawResetGroupButton()
        {
            bool resetRequested = _categoryResetRequested;
            _categoryResetRequested = false;
            return resetRequested;
        }

        private void PersistGuiLayout()
        {
            if (_menuWindowX == null)
                return;

            if (!Mathf.Approximately(
                _menuWindowX.Value,
                _menuRect.x))
                _menuWindowX.Value = _menuRect.x;
            if (!Mathf.Approximately(
                _menuWindowY.Value,
                _menuRect.y))
                _menuWindowY.Value = _menuRect.y;
            if (!Mathf.Approximately(
                _colorPickerWindowX.Value,
                _colorPickerRect.x))
                _colorPickerWindowX.Value = _colorPickerRect.x;
            if (!Mathf.Approximately(
                _colorPickerWindowY.Value,
                _colorPickerRect.y))
                _colorPickerWindowY.Value = _colorPickerRect.y;
        }

    }
}
