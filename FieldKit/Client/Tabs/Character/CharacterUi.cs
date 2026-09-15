namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void DrawCharacterMenu()
        {
            _characterMenuScroll = BeginVerticalScrollView(
                _characterMenuScroll,
                GUILayout.Height(MenuContentHeight));

            BeginCategoryColumns();

            BeginCategoryPanel("Health");
            DrawOptionSlider(
                "Health regeneration",
                _healthRegeneration,
                0f,
                25f,
                "0.0 HP/s");

            if (DrawResetGroupButton())
            {
                _healthRegeneration.Value = 0f;
            }
            EndCategoryPanel();

            NextCategoryColumn();

            BeginCategoryPanel("Energy & Hydration");
            DrawOptionSlider(
                "Energy drain",
                _energyDrainMultiplier,
                0f,
                2f,
                "0.00x");
            DrawOptionSlider(
                "Hydration drain",
                _hydrationDrainMultiplier,
                0f,
                2f,
                "0.00x");

            if (DrawResetGroupButton())
            {
                _energyDrainMultiplier.Value = 1f;
                _hydrationDrainMultiplier.Value = 1f;
            }
            EndCategoryPanel();

            EndCategoryColumns();
            GUILayout.Space(36f);
            EndVerticalScrollView();
        }
    }
}
