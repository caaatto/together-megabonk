using HarmonyLib;
using MegabonkTogether.Common;
using MegabonkTogether.Configuration;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(MyInputManager))]
    internal static class MyInputManagerPatches
    {
        private static readonly IAutoUpdaterService autoUpdaterService = Plugin.Services.GetService<IAutoUpdaterService>();
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();

        private static float inputGraceUntilRealtime = 0f;

        /// <summary>
        /// Starts ignoring button presses for a moment, called when a reward, chest or level up window
        /// opens. Those windows can appear without warning while you are holding a key in the fight, and
        /// the press then picks something for you straight away (issue #4).
        ///
        /// Realtime on purpose: shared experience really pauses the game while such a window is open, so
        /// a scaled clock would never let the grace expire.
        /// </summary>
        public static void BeginInputGrace()
        {
            var grace = ModConfig.RewardInputGraceSeconds.Value;
            if (grace <= 0f)
            {
                return;
            }

            inputGraceUntilRealtime = Time.realtimeSinceStartup + grace;
        }

        /// <summary>
        /// Only edge triggered presses are held back. Movement runs on axes (Move Horizontal and
        /// Move Vertical), so the player can still walk out of danger while the window is up
        /// </summary>
        private static bool IsInputGraceActive()
        {
            if (inputGraceUntilRealtime <= 0f)
            {
                return false;
            }

            if (Time.realtimeSinceStartup >= inputGraceUntilRealtime)
            {
                inputGraceUntilRealtime = 0f;
                return false;
            }

            return synchronizationService.HasNetplaySessionStarted();
        }

        /// <summary>
        /// Prevent input when an update is available
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyInputManager.GetButtonDown))]
        public static bool GetButtonDown_Prefix(ref bool __result)
        {
            if (autoUpdaterService.IsAnUpdateAvailable())
            {
                __result = false;
                return false;
            }

            if (IsInputGraceActive())
            {
                __result = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prevent input when an update is available
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyInputManager.GetButtonUp))]
        public static bool GetButtonUp_Prefix(ref bool __result)
        {
            if (autoUpdaterService.IsAnUpdateAvailable())
            {
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prevent input when an update is available
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyInputManager.GetButton))]
        public static bool GetButton_Prefix(ref bool __result)
        {
            if (autoUpdaterService.IsAnUpdateAvailable())
            {
                __result = false;
                return false;
            }
            return true;
        }

    }


}
