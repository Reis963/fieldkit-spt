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
                        typeof(HitCameraShaker),
                        nameof(HitCameraShaker.Hit)),
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(Plugin),
                            nameof(ScaleVisualHitPunch))));

                MethodInfo damageReactionMethod =
                    AccessTools.Method(
                        typeof(EffectsController),
                        "method_7",
                        new[]
                        {
                            typeof(float),
                            typeof(EBodyPart),
                            typeof(EDamageType),
                            typeof(float),
                            typeof(EFT.Ballistics.MaterialType)
                        });
                MethodInfo damageForceMethod =
                    AccessTools.Method(
                        typeof(ForceEffector),
                        nameof(ForceEffector.AddForce),
                        new[]
                        {
                            typeof(float),
                            typeof(float),
                            typeof(float)
                        });

                if (damageReactionMethod != null)
                {
                    _harmony.Patch(
                        damageReactionMethod,
                        prefix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(BeginVisualDamageReaction))),
                        postfix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(EndVisualDamageReaction))));
                }

                if (damageForceMethod != null)
                {
                    _harmony.Patch(
                        damageForceMethod,
                        prefix: new HarmonyMethod(
                            AccessTools.Method(
                                typeof(Plugin),
                                nameof(ScaleVisualDamageCameraForce))));
                }

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
