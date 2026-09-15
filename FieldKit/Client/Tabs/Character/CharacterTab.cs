namespace FieldKit
{
    public sealed partial class Plugin
    {
        private ConfigEntry<float> _healthRegeneration;
        private ConfigEntry<float> _energyDrainMultiplier;
        private ConfigEntry<float> _hydrationDrainMultiplier;
        private Vector2 _characterMenuScroll;
        private float _lastCharacterRecoveryTime;
        private float _nextCharacterRecoveryTime;

        private void ConfigureCharacterTools()
        {
            _healthRegeneration = BindCharacterRange(
                "Health Regeneration Per Second", 0f, 0f, 25f,
                "Restore this much health per second to each living body part.");
            _energyDrainMultiplier = BindCharacterRange(
                "Energy Drain Multiplier", 1f, 0f, 2f,
                "Scale negative local energy changes; zero disables drain.");
            _hydrationDrainMultiplier = BindCharacterRange(
                "Hydration Drain Multiplier", 1f, 0f, 2f,
                "Scale negative local hydration changes; zero disables drain.");
        }

        private ConfigEntry<float> BindCharacterRange(
            string name,
            float defaultValue,
            float minimum,
            float maximum,
            string description)
        {
            return Config.Bind(
                "Character",
                name,
                defaultValue,
                new ConfigDescription(
                    description,
                    new AcceptableValueRange<float>(minimum, maximum)));
        }
    }
}
