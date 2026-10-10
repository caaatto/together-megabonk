using HarmonyLib;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(FullMap))]
    internal static class FullMapPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetRequiredService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetRequiredService<IPlayerManagerService>();

        /// <summary>
        /// Reveal fog around all NetPlayers too
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(FullMap.FixedUpdate))]
        public static void FixedUpdate_Postfix(FullMap __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            //The point of this guard is not to reveal positions that are not final yet, which is a
            //loading concern. It used to check CanInput(), which is also false while the local player is
            //dead, so the whole netplayer reveal stopped exactly when a spectating player needs the map
            if (synchronizationService.IsLoading() || synchronizationService.IsLoadingNextLevel()) return;

            var netPlayers = playerManagerService.GetAllSpawnedNetPlayers();

            foreach (var netPlayer in netPlayers)
            {
                if (netPlayer?.Model != null)
                {
                    playerManagerService.AddGetNetplayerPositionRequest(netPlayer.ConnectionId);
                    __instance.QueueRevealFog(netPlayer.Model.transform.position);
                    __instance.RevealFog();
                    playerManagerService.UnqueueNetplayerPositionRequest();
                }
            }
        }
    }

}
