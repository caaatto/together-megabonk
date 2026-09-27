using Assets.Scripts.Utility;
using HarmonyLib;
using MegabonkTogether.Configuration;
using MegabonkTogether.Helpers;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using Utility;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(SpawnPlayerPortal))]
    internal static class SpawnPlayerPortalPatches
    {
        private static readonly ISynchronizationService synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        private static readonly IPlayerManagerService playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
        private static TextMeshProUGUI synchronizeText;
        private static float dotAnimTimer = 0f;
        private static int dotCount = 0;
        public static Coroutine WaitForLobbyCoroutine;

        private const float LOG_INTERVAL_SECONDS = 5f;
        private const float GAVE_UP_MESSAGE_SECONDS = 2f;

        /// <summary>
        /// Wait for all players to be ready before starting the game
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SpawnPlayerPortal.StartPortal))]
        public static void StartPortal_Prefix()
        {
            if (!synchronizationService.HasNetplaySessionInitialized())
            {
                return;
            }

            if (!synchronizationService.IsLobbyReady())
            {
                MyTime.Pause();

                if (WaitForLobbyCoroutine == null)
                {
                    WaitForLobbyCoroutine = CoroutineRunner.Instance.Run(WaitForLobbyReady());
                }
            }

        }

        private static IEnumerator WaitForLobbyReady()
        {
            Plugin.Log.LogInfo("Waiting for lobby to be ready");

            if (synchronizeText == null)
            {
                synchronizeText = new GameObject("synchronizeText").AddComponent<TMPro.TextMeshProUGUI>();
            }

            synchronizeText.enabled = true;
            synchronizeText.transform.SetParent(UiManager.Instance.transform);
            synchronizeText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            synchronizeText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            synchronizeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            synchronizeText.rectTransform.anchoredPosition = new Vector2(0, 0);
            synchronizeText.alignment = TMPro.TextAlignmentOptions.Center;
            synchronizeText.text = "Waiting for other players";
            synchronizeText.fontSize = 48;

            dotAnimTimer = 0f;
            dotCount = 0;

            synchronizationService.TransitionToState(GameEvent.Ready);

            var timeout = ModConfig.LobbyReadyTimeoutSeconds.Value;
            var waited = 0f;
            var nextLogAt = 0f;
            var gaveUp = false;

            try
            {
                while (!synchronizationService.IsLobbyReady())
                {
                    //Unscaled all the way through: the game is really paused here with shared experience.
                    //Yielding null rather than a WaitForSeconds for the same reason, a scaled wait never
                    //resumes at timeScale 0
                    var delta = Time.unscaledDeltaTime;
                    dotAnimTimer += delta;
                    waited += delta;

                    if (dotAnimTimer >= 1f)
                    {
                        dotAnimTimer = 0f;
                        dotCount = (dotCount + 1) % 4;
                        string dots = new string('.', dotCount);
                        synchronizeText.text = $"Waiting for other players {dots}";
                    }

                    if (waited >= nextLogAt)
                    {
                        //Used to log on every iteration, about six lines a second, which buries everything
                        //useful in the log people are asked to attach to a bug report
                        nextLogAt = waited + LOG_INTERVAL_SECONDS;
                        Plugin.Log.LogInfo($"Lobby not ready yet, waited {waited:F0}s");
                    }

                    //IsLobbyReady needs at least two players, so once everybody else is gone it can never
                    //become true again. This used to wait forever on a paused game with no way out
                    if (playerManagerService.GetAllPlayers().Count() < 2)
                    {
                        Plugin.Log.LogWarning("Every other player left while waiting for the lobby, continuing alone instead of waiting forever.");
                        synchronizeText.text = "The other players left";
                        gaveUp = true;
                        break;
                    }

                    if (timeout > 0f && waited >= timeout)
                    {
                        Plugin.Log.LogWarning($"Lobby still not ready after {timeout}s, continuing anyway.");
                        synchronizeText.text = "Gave up waiting for the other players";
                        gaveUp = true;
                        break;
                    }

                    yield return null;
                }
            }
            finally
            {
                //Has to happen on every exit. Leaving these undone keeps the game paused and leaves the
                //handle set, so StartPortal can never start another wait
                synchronizationService.TransitionToState(GameEvent.Start);

                var seed = playerManagerService.GetSeed();
                MyRandom.random = new Il2CppSystem.Random(seed);

                WaitForLobbyCoroutine = null;

                MyTime.Unpause();
            }

            Plugin.Log.LogInfo("Done waiting for the lobby, starting the game");

            if (gaveUp)
            {
                //Keep the reason on screen briefly, otherwise nobody knows why the run started like this
                var shown = 0f;
                while (shown < GAVE_UP_MESSAGE_SECONDS)
                {
                    shown += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (synchronizeText != null)
            {
                synchronizeText.enabled = false;
            }
        }
    }
}
