
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void InstallQuestPatches()
        {
            try
            {
                MethodInfo gameStarted = AccessTools.DeclaredMethod(
                    typeof(GameWorld), nameof(GameWorld.OnGameStarted), Type.EmptyTypes);
                if (gameStarted == null || gameStarted.IsStatic ||
                    gameStarted.ReturnType != typeof(void))
                    throw new MissingMethodException("GameWorld.OnGameStarted()");
                _harmony.Patch(gameStarted, postfix: new HarmonyMethod(
                    typeof(Plugin), nameof(OnQuestRaidStartedPostfix)));
            }
            catch (Exception exception)
            {
                LogSource.LogWarning("Quest-zone startup scan hook failed; use the manual scan: " + exception.Message);
            }

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

        private static void OnQuestRaidStartedPostfix(GameWorld __instance)
        {
            Plugin plugin = _instance;
            if (plugin == null)
                return;
            if (plugin._world != __instance)
                plugin.AttachWorld(__instance);
            plugin.RefreshQuestTriggerCaches();
            plugin._nextQuestTriggerRefresh = Time.unscaledTime + 5f;
            plugin._questStartupRefreshPending = true;
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
