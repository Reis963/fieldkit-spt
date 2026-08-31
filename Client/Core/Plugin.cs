
namespace FieldKit
{
    [BepInPlugin("com.hysocs.fieldkit", "Hysocs-FieldKit", "1.3.0")]
    [BepInDependency("com.SPT.core", "4.1.3")]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource;
        private static Plugin _instance;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<bool> _showPmc;
        private ConfigEntry<bool> _showScav;
        private ConfigEntry<bool> _showBoss;
        private ConfigEntry<bool> _showBoxes;
        private ConfigEntry<bool> _visibilityCheck;
        private ConfigEntry<bool> _scopeEsp;
        private ConfigEntry<bool> _showHealthBar;
        private ConfigEntry<bool> _showHealthPercentage;
        private ConfigEntry<bool> _showPlayerName;
        private ConfigEntry<bool> _showRole;
        private ConfigEntry<bool> _showWeapon;
        private ConfigEntry<bool> _showDistance;
        private ConfigEntry<bool> _cameraDebug;
        private ConfigEntry<int> _espPaletteVersion;
        private ConfigEntry<float> _scopeColorBrightness;
        private ConfigEntry<string> _pmcVisualColor;
        private ConfigEntry<string> _scavVisualColor;
        private ConfigEntry<string> _bossVisualColor;
        private ConfigEntry<string> _pmcOccludedColor;
        private ConfigEntry<string> _scavOccludedColor;
        private ConfigEntry<string> _bossOccludedColor;
        private ConfigEntry<float> _maxDistance;
        private ConfigEntry<float> _lineThickness;
        private ConfigEntry<int> _fontSize;
        private ConfigEntry<string> _espFontName;
        private ConfigEntry<float> _textOutlineThickness;
        private ConfigEntry<KeyboardShortcut> _menuKey;
        private ConfigEntry<KeyboardShortcut> _espKey;
        private bool _menuShortcutLatched;
        private bool _guiThemeRefreshRequested;

        private readonly List<Target> _targets = new List<Target>(48);
        private readonly List<BoxCommand> _boxes = new List<BoxCommand>(48);
        private readonly List<LineCommand> _lines = new List<LineCommand>(768);
        private readonly List<FilledPolygonCommand> _filledPolygons =
            new List<FilledPolygonCommand>(64);
        private readonly List<Text> _labels = new List<Text>(48);
        private readonly List<ScopeOverlay> _scopeOverlays =
            new List<ScopeOverlay>(2);
        private readonly RaycastHit[] _visibilityHits = new RaycastHit[64];
        private readonly HashSet<int> _localPlayerColliderIds =
            new HashSet<int>();
        private readonly HashSet<int> _transparentVisibilityColliderIds =
            new HashSet<int>();
        private readonly HashSet<int> _opaqueVisibilityColliderIds =
            new HashSet<int>();
        private static readonly int VisibilityMask =
            Physics.DefaultRaycastLayers &
            ~(1 << LayerMask.NameToLayer("Grass")) &
            ~(1 << LayerMask.NameToLayer("Foliage"));
        private static readonly EBodyPart[] HealthParts =
        {
            EBodyPart.Head,
            EBodyPart.Chest,
            EBodyPart.Stomach,
            EBodyPart.LeftArm,
            EBodyPart.RightArm,
            EBodyPart.LeftLeg,
            EBodyPart.RightLeg
        };
        private GameWorld _world;
        private Player _localPlayer;
        private Camera _camera;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private BoxGraphic _boxGraphic;
        private Font _font;
        private Harmony _harmony;
        private int _lastRenderFrame = -1;
        private bool _overlayHasContent;
        private float _nextWorldRefresh;
        private bool _scopeRefreshRequested;
        private bool _shuttingDown;
    }
}
