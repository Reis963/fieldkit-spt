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

            BeginCategoryPanel("Health & Reactions");
            DrawOptionSlider(
                "Health regeneration",
                _healthRegeneration,
                0f,
                25f,
                "0.0 HP/s");
            DrawOptionSlider(
                "Hit punch",
                _visualHitPunchAmount,
                0f,
                1f,
                "P0");

            if (DrawResetGroupButton())
            {
                _healthRegeneration.Value = 0f;
                _visualHitPunchAmount.Value = 1f;
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
