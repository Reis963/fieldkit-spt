namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void InstallCharacterPatches()
        {
            try
            {
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(ActiveHealthController),
                        nameof(ActiveHealthController.ChangeEnergy)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalEnergyDrain))));
                _harmony.Patch(
                    AccessTools.Method(
                        typeof(ActiveHealthController),
                        nameof(ActiveHealthController.ChangeHydration)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleLocalHydrationDrain))));

                LogSource.LogInfo("Character patches installed.");
            }
            catch (Exception exception)
            {
                LogSource.LogError(
                    "Failed to install character patches: " + exception);
            }
        }

        private static void ScaleLocalEnergyDrain(
            ActiveHealthController __instance,
            ref float __0)
        {
            if (__0 < 0f && IsLocalHealthController(__instance))
                __0 *= _instance._energyDrainMultiplier.Value;
        }

        private static void ScaleLocalHydrationDrain(
            ActiveHealthController __instance,
            ref float __0)
        {
            if (__0 < 0f && IsLocalHealthController(__instance))
                __0 *= _instance._hydrationDrainMultiplier.Value;
        }

        private static bool IsLocalHealthController(
            ActiveHealthController controller)
        {
            return _instance != null &&
                _instance._localPlayer != null &&
                ReferenceEquals(
                    controller,
                    _instance._localPlayer.ActiveHealthController);
        }
    }
}
