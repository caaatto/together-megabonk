using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(ChargeShrine))]
    internal static class ChargeShrinePatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();

        /// <summary>
        /// Synchronize starting to charge shrine.
        /// The server check and notify other clients if they need to start (Nothing happens if already charging)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChargeShrine.OnTriggerEnter))]
        public static bool OnTriggerEnter_Prefix(ChargeShrine __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }

            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }

            var shrineNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");

            if (shrineNetplayId.HasValue)
            {
                return synchronizationService.OnStartingToChargingShrine(shrineNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Charge shrine has no netplay id set!");
            }

            return true;
        }

        /// <summary>
        /// Synchronize stopping to charge shrine.
        /// The server check and notify other clients if they need to stop (Nothing happens if a player is still charging)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChargeShrine.OnTriggerExit))]
        public static bool OnTriggerExit_Prefix(ChargeShrine __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true;
            }
            if (!Plugin.CAN_SEND_MESSAGES)
            {
                return true;
            }
            var shrineNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");
            if (shrineNetplayId.HasValue)
            {
                return synchronizationService.OnStoppingChargingShrine(shrineNetplayId.Value);
            }
            else
            {
                Plugin.Log.LogWarning("Charge shrine has no netplay id set!");
            }
            return true;
        }

        /// <summary>
        /// A shrine that stops short of completion on one player but completed on another leaves the host
        /// waiting for an encounter that player never gets
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChargeShrine.OnTriggerExit))]
        public static void OnTriggerExit_Postfix(ChargeShrine __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted() || __instance.completed || __instance.charging)
            {
                return;
            }

            var shrineNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");
            Plugin.Log.LogInfo($"Charge shrine {shrineNetplayId} stopped at {__instance.chargeProgress}/{__instance.currentChargeTime}");
        }

        /// <summary>
        /// Every player charges and completes the shrine on its own, and in shared experience each completion
        /// opens an encounter the host waits for. Logged to spot a player whose shrine never completes
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChargeShrine.Complete))]
        public static void Complete_Postfix(ChargeShrine __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            var shrineNetplayId = DynamicData.For(__instance.gameObject).Get<uint?>("netplayId");
            Plugin.Log.LogInfo($"Charge shrine {shrineNetplayId} completed locally (chargeTime {__instance.chargeTime}, currentChargeTime {__instance.currentChargeTime})");
        }
    }
}
