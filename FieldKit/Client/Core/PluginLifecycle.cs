
namespace FieldKit
{
    public sealed partial class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            _instance = this;
            LogSource = Logger;

            _enabled = Config.Bind("ESP", "Enabled", true, "Enable the ESP.");
            _showPmc = Config.Bind("ESP", "Show PMCs", true, "Show BEAR and USEC.");
            _showScav = Config.Bind("ESP", "Show Scavs", true, "Show ordinary scavs.");
            _showBoss = Config.Bind("ESP", "Show Bosses", true, "Show bosses and special scav roles.");
            _showBoxes = Config.Bind(
                "ESP", "Show Boxes", true,
                "Draw a box around each enabled ESP target.");
            _visibilityCheck = Config.Bind(
                "ESP", "Visibility Check", true,
                "Hide ESP beyond 25 m unless the head or chest is visible. Within 25 m, always show ESP. Max Distance applies in both cases.");
            _scopeEsp = Config.Bind(
                "ESP", "Scope ESP", true,
                "Render character ESP through magnified optic cameras.");
            _showHealthBar = Config.Bind(
                "ESP Information", "Show Health Bar", true,
                "Draw a health bar beside each character box.");
            _showHealthPercentage = Config.Bind(
                "ESP Information", "Show Health Percentage", true,
                "Include the target's total health percentage in its label.");
            _showPlayerName = Config.Bind(
                "ESP Information", "Show Player Name", true,
                "Include the target's nickname in its label.");
            _showRole = Config.Bind(
                "ESP Information", "Show Role", true,
                "Include the target's faction or AI role in its label.");
            _showWeapon = Config.Bind(
                "ESP Information", "Show Weapon", true,
                "Include the target's currently equipped weapon in its label.");
            _showDistance = Config.Bind(
                "ESP Information", "Show Distance", true,
                "Include the target's distance in its label.");
            _cameraDebug = Config.Bind("Diagnostics", "Camera Debug Logging", false,
                "Log main-camera selection and active optic-camera details.");
            _scopeColorBrightness = Config.Bind(
                "ESP", "Scope Color Brightness", 0.5f,
                new ConfigDescription(
                    "Compensates for tinting when EFT composites the optic texture.",
                    new AcceptableValueRange<float>(0.5f, 2f)));
            _pmcVisualColor = Config.Bind(
                "Visuals", "PMC Color", "#FF4040FF",
                "RGBA color used for visible PMC ESP.");
            _scavVisualColor = Config.Bind(
                "Visuals", "Scav Color", "#FFD91AFF",
                "RGBA color used for visible Scav ESP.");
            _bossVisualColor = Config.Bind(
                "Visuals", "Boss Color", "#FF26E6FF",
                "RGBA color used for visible Boss and special Scav ESP.");
            _pmcOccludedColor = Config.Bind(
                "Visuals", "PMC Occluded Color", "#4D1313BF",
                "RGBA color used for occluded PMC ESP.");
            _scavOccludedColor = Config.Bind(
                "Visuals", "Scav Occluded Color", "#4D4108BF",
                "RGBA color used for occluded Scav ESP.");
            _bossOccludedColor = Config.Bind(
                "Visuals", "Boss Occluded Color", "#4D0745BF",
                "RGBA color used for occluded Boss and special Scav ESP.");
            _maxDistance = Config.Bind("ESP", "Max Distance", 500f,
                new ConfigDescription("Maximum drawing distance.",
                    new AcceptableValueRange<float>(25f, 1500f)));
            _lineThickness = Config.Bind("ESP", "Box Thickness", 2f,
                new ConfigDescription("Box line thickness.",
                    new AcceptableValueRange<float>(1f, 8f)));
            _fontSize = Config.Bind("ESP", "Font Size", 13,
                new ConfigDescription("Label font size.",
                    new AcceptableValueRange<int>(9, 32)));
            _espFontName = Config.Bind(
                "ESP", "Font", "Segoe UI",
                "Font used by character ESP labels.");
            _textOutlineThickness = Config.Bind(
                "ESP", "Text Outline Thickness", 1f,
                new ConfigDescription(
                    "Black outline thickness around ESP labels.",
                    new AcceptableValueRange<float>(0f, 3f)));

            _menuKey = Config.Bind("Hotkeys", "Toggle Menu",
                new KeyboardShortcut(KeyCode.Insert));
            _espKey = Config.Bind("Hotkeys", "Toggle ESP",
                new KeyboardShortcut(KeyCode.Home));

            ConfigureRoleEsp();
            ConfigureGuiSettings();
            ConfigureCharacterTools();
            ConfigureQuestTools();
            ConfigureOtherTools();
            ConfigureToggleHotkeys();
            _font = LoadFont();
            _espFontName.SettingChanged += OnEspFontSettingChanged;
            _textOutlineThickness.SettingChanged +=
                OnEspOutlineSettingChanged;
            _harmony = new Harmony("com.hysocs.fieldkit.patches");
            InstallCharacterPatches();
            InstallQuestPatches();
            InstallMenuInputPatches();
            _enabled.SettingChanged += OnEspEnabledSettingChanged;
            _scopeEsp.SettingChanged += OnScopeEspSettingChanged;
            Canvas.preWillRenderCanvases += RenderEspFrame;

            PrintLoadedMessage();
        }

        private void Update()
        {
            HandleMenuShortcutUpdate();
            if (_guiThemeRefreshRequested)
            {
                DisposeGuiTheme();
                _guiThemeRefreshRequested = false;
            }

            UpdateToggleHotkeys();

            if (_scopeRefreshRequested ||
                Time.unscaledTime >= _nextWorldRefresh)
            {
                RefreshWorld();
                _nextWorldRefresh = Time.unscaledTime + 0.5f;
            }

            if (_world == null)
                ClearOverlay();

            UpdateCharacterTools();
            UpdateQuestStartupRefresh();
        }

        private void LateUpdate()
        {
            MaintainMenuCursor();
        }

        private void OnGUI()
        {
            MaintainMenuCursor();
            HandleMenuShortcutGuiEvent();
            if (!_menuOpen)
                return;

            EnsureGuiTheme();
            GUISkin previousSkin = GUI.skin;
            Color previousColor = GUI.color;
            Color previousBackground = GUI.backgroundColor;
            Color previousContent = GUI.contentColor;
            Matrix4x4 previousMatrix = GUI.matrix;

            try
            {
                GUI.skin = _adminSkin;
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;

                if (_menuOpen)
                {
                    float menuScale = MenuScale;
                    GUI.matrix = Matrix4x4.Scale(
                        new Vector3(menuScale, menuScale, 1f)) *
                        previousMatrix;
                    UpdateMenuGeometry();
                    _menuRect = GUI.Window(
                        731904,
                        _menuRect,
                        DrawMenu,
                        "");
                    DrawColorPickerPopout();
                    GUI.matrix = previousMatrix;
                }
                if (Event.current.rawType == EventType.MouseUp)
                    PersistGuiLayout();
            }
            finally
            {
                GUI.skin = previousSkin;
                GUI.color = previousColor;
                GUI.backgroundColor = previousBackground;
                GUI.contentColor = previousContent;
                GUI.matrix = previousMatrix;
            }
        }

        private void OnDestroy()
        {
            _shuttingDown = true;
            PersistGuiLayout();
            if (_enabled != null)
                _enabled.SettingChanged -= OnEspEnabledSettingChanged;
            if (_scopeEsp != null)
                _scopeEsp.SettingChanged -= OnScopeEspSettingChanged;
            UnsubscribeQuestSettings();
            if (_guiPrimaryColor != null)
                _guiPrimaryColor.SettingChanged -=
                    OnGuiPrimaryColorChanged;
            if (_menuFontName != null)
                _menuFontName.SettingChanged -=
                    OnMenuFontSettingChanged;
            Canvas.preWillRenderCanvases -= RenderEspFrame;
            SetMenuOpen(false);
            DetachWorld();
            DisposeGuiTheme();

            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch { }

            _instance = null;

            if (_canvas != null)
                Destroy(_canvas.gameObject);

            _canvas = null;
            _canvasRect = null;
            _boxGraphic = null;
            _labels.Clear();
            DestroyScopeOverlays();
        }

        private void OnEspEnabledSettingChanged(
            object sender,
            EventArgs eventArgs)
        {
            _lastRenderFrame = -1;
            _scopeRefreshRequested = true;
            if ((_enabled == null || !_enabled.Value) &&
                !IsQuestOverlayEnabled())
                ClearOverlay();
        }

        private void OnScopeEspSettingChanged(
            object sender,
            EventArgs eventArgs)
        {
            _lastRenderFrame = -1;
            _scopeRefreshRequested = true;

            if (_scopeEsp == null || !_scopeEsp.Value)
                DestroyScopeOverlays();
        }

    }
}
