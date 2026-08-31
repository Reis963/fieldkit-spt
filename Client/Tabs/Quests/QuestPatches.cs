
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void InstallQuestPatches()
        {
            try
            {
                MethodInfo conditionChanged = AccessTools.Method(
                    typeof(QuestController),
                    nameof(QuestController
                        .OnConditionChangedHandler));
                if (conditionChanged == null)
                {
                    LogSource.LogWarning(
                        "Quest-condition update hook was not found.");
                    return;
                }

                _harmony.Patch(
                    conditionChanged,
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(OnQuestConditionChangedPostfix))));
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Failed to install quest-condition update hook: " +
                    exception.Message);
            }
        }

        private static void OnQuestConditionChangedPostfix()
        {
            Plugin plugin = _instance;
            if (plugin == null)
                return;

            plugin.InvalidateQuestObjectiveData();
            plugin._lastRenderFrame = -1;
        }
    }
}
