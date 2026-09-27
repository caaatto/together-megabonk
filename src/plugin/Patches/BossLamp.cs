using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(BossLamp))]
    internal static class BossLampPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IGameBalanceService gameBalanceService = Plugin.Services.GetService<IGameBalanceService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Update charge time based on game balance settings (number of players)
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BossLamp.Awake))]
        public static void Awake_Postfix(BossLamp __instance)
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            __instance.chargeTime = gameBalanceService.GetBossLampRequiredCharge();
        }

        /// <summary>
        /// Synchronize starting to charge lamp
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossLamp.OnTriggerEnter))]
        public static bool OnTriggerEnter_Prefix(BossLamp __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }

            var lampNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");

            if (lampNetplayId.HasValue)
            {
                return synchronizationService.OnStartingToChargingLamp(lampNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Lamp has no netplay id set!");
            }

            return true;
        }

        /// <summary>
        /// Only the host decides when a lamp turns on. GraveyardBossRoom derives the boss armor from
        /// lampCount, which LampActivate and LampDeactivate maintain, so letting each client reach that
        /// state on its own is what leaves the boss shielded for a guest while every lamp looks lit
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossLamp.Complete))]
        public static bool Complete_Prefix(BossLamp __instance)
        {
            return GateLampStateChange(__instance, isTurnedOn: true);
        }

        /// <summary>
        /// Same for a lamp going out again
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossLamp.Deactivate))]
        public static bool Deactivate_Prefix(BossLamp __instance)
        {
            return GateLampStateChange(__instance, isTurnedOn: false);
        }

        /// <summary>
        /// The deactivate delay is rolled locally between randomDeactivateTimeMin and Max, so every
        /// client would pick a different moment for a lamp to go out. The host owns that roll
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossLamp.CheckRandomDeactivate))]
        public static bool CheckRandomDeactivate_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            return synchronizationService.IsServerMode() ?? false;
        }

        private static bool GateLampStateChange(BossLamp lamp, bool isTurnedOn)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true; //We are replaying what the host told us
            }

            if (!(synchronizationService.IsServerMode() ?? false))
            {
                return false; //Wait for the host to say so
            }

            var lampNetplayId = DynamicData.For(lamp.gameObject).Get<uint?>("netplayId");
            if (lampNetplayId.HasValue)
            {
                synchronizationService.OnBossLampStateChanged(lampNetplayId.Value, isTurnedOn);
            }
            else
            {
                Plugin.Log.LogWarning("Lamp has no netplay id set!");
            }

            return true;
        }

        /// <summary>
        /// Synchronize stopping charging lamp
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BossLamp.OnTriggerExit))]
        public static bool OnTriggerExit_Prefix(BossLamp __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }
            var lampNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");
            if (lampNetplayId.HasValue)
            {
                return synchronizationService.OnStoppingChargingLamp(lampNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Lamp has no netplay id set!");
            }
            return true;
        }
    }
}
