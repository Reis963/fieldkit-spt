
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private ConfigEntry<string> _menuFontName;
        private ConfigEntry<float> _menuUiScale;
        private float _pendingMenuUiScale;
        private Vector2 _otherMenuScroll;

        private void ConfigureOtherTools()
        {
            _menuFontName = Config.Bind(
                "GUI Appearance",
                "Menu Font",
                "Segoe UI",
                "Font used by the FieldKit menu.");
            _menuUiScale = Config.Bind(
                "GUI Appearance",
                "UI Scale",
                1f,
                new ConfigDescription(
                    "Scale the complete FieldKit menu for high-resolution displays.",
                    new AcceptableValueRange<float>(0.5f, 50f)));
            _pendingMenuUiScale = _menuUiScale.Value;
            _menuFontName.SettingChanged +=
                OnMenuFontSettingChanged;
        }
    }
}
