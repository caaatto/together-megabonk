using Assets.Scripts.Utility;
using System;
using HarmonyLib;
using MegabonkTogether.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using UnityEngine;

namespace MegabonkTogether.Patches
{
    [HarmonyPatch(typeof(ChestOpening))]
    internal static class ChestOpeningPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();

        /// <summary>
        /// Skip the opening animation when in a netplay session and not in shared experience
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestOpening.OpenChest))]
        public static void OpenChest_Postfix(ChestOpening __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (synchronizationService.IsSharedExperienceEnabled())
            {
                return;
            }

            __instance.skipped = true;
        }
    }


    //TODO: Refacto with LevelUpScreenPatch
    [HarmonyPatch(typeof(ChestWindowUi))]
    internal static class ChestWindowUiPatches
    {
        private static readonly Services.ISynchronizationService synchronizationService = Plugin.Services.GetService<Services.ISynchronizationService>();
        public static Coroutine CurrentRoutine;
        private static TMPro.TextMeshProUGUI infoText;
        private static MyButton openButton;

        /// <summary>
        /// Fix for issue #93 (soft lock when opening a chest).
        /// ChestWindowUi.Open dereferences b_open, so a null in there throws a NullReferenceException
        /// inside the game code and leaves the encounter half open forever.
        /// We restore the button we cached ourselves when possible, and when the window really is
        /// unusable we abort the encounter instead of soft locking every player.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChestWindowUi.Open))]
        public static bool Open_Prefix(ChestWindowUi __instance, Assets.Scripts.UI.InGame.Rewards.EEncounter encounterType)
        {
            if (__instance.b_open == null && openButton != null)
            {
                //A previous OnClose stored a null back into b_open, put ours back
                __instance.b_open = openButton;
            }

            if (__instance.chestOpening != null && __instance.b_open != null &&
                __instance.b_leave != null && __instance.b_take != null &&
                __instance.b_banish != null && __instance.t_itemName != null)
            {
                return true;
            }

            Plugin.Log.LogError($"[chest #93] ChestWindowUi.Open would dereference a destroyed ref: " +
                $"encounter={encounterType} shared={synchronizationService.IsSharedExperienceEnabled()} " +
                $"chestOpening={__instance.chestOpening == null} b_open={__instance.b_open == null} " +
                $"b_leave={__instance.b_leave == null} b_take={__instance.b_take == null} " +
                $"b_banish={__instance.b_banish == null} t_itemName={__instance.t_itemName == null}");

            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return true; //Solo run, keep the vanilla behaviour
            }

            AbortChestEncounter(__instance);
            return false;
        }

        /// <summary>
        /// Release a chest encounter we could not open, so neither we nor the other players stay stuck
        /// </summary>
        private static void AbortChestEncounter(ChestWindowUi __instance)
        {
            try
            {
                if (__instance != null)
                {
                    __instance.gameObject.SetActive(false);
                }

                var uiManager = UiManager.Instance;
                if (uiManager != null && uiManager.encounterWindows != null)
                {
                    //Otherwise every later reward gets queued behind an encounter that never finishes
                    uiManager.encounterWindows.encounterInProgress = false;
                }

                ScreenTextHelper.Clear();
                MyTime.Unpause();

                if (synchronizationService.IsSharedExperienceEnabled())
                {
                    //Tell the others we are not holding the encounter open
                    synchronizationService.RewardFinished();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[chest #93] Failed to abort the chest encounter cleanly: {ex}");
            }
        }

        /// <summary>
        /// Didn't find a proper way to hide the open button ¯\_(ツ)_/¯
        /// Also let the original method run if shared experience is enabled
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestWindowUi.Open))]
        public static void Open_Postfix(ChestWindowUi __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            //Before the shared experience early return below, the accidental press happens in both modes
            MyInputManagerPatches.BeginInputGrace();

            if (synchronizationService.IsSharedExperienceEnabled())
            {
                return;
            }

            if (openButton == null && __instance.b_open != null)
            {
                openButton = __instance.b_open;
            }

            __instance.OpenButton();

            if (openButton != null)
            {
                //Only drop the reference once we are sure we can hand it back in OnClose
                __instance.b_open = null;
            }
        }

        /// <summary>
        /// Add a 5 seconds of invulnerability and a warning after opening a chest in netplay on shared experience only
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestWindowUi.OpeningFinished))]
        public static void OpeningFinished_Postfix(ChestWindowUi __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            if (synchronizationService.IsSharedExperienceEnabled())
            {
                return;
            }

            if (!GameManager.Instance.player.playerInput.CanInput())
            {
                Plugin.Log.LogInfo("Player cannot input, skipping invulnerability after chest opening.");
                return;
            }

            MyTime.Unpause();

            CoroutineRunner.Instance.Stop(CurrentRoutine);
            CurrentRoutine = CoroutineRunner.Instance.Run(Wait5SecBeforeCanTakeDamageAgain(__instance));
        }

        /// <summary>
        /// Remove the invulnerability after closing the chest window on netplay
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ChestWindowUi.OnClose))]
        public static void OnClose_Postfix(ChestWindowUi __instance)
        {
            if (!synchronizationService.HasNetplaySessionStarted())
            {
                return;
            }

            //Restore first and unconditionally: this used to sit behind the CurrentRoutine check and
            //behind the shared experience early return of Open_Postfix, so b_open either stayed null
            //or got a null written back into it, which crashed the next chest (issue #93)
            if (openButton != null)
            {
                __instance.b_open = openButton;
            }

            if (CurrentRoutine == null)
            {
                Plugin.Log.LogDebug("No active invulnerability routine, skipping.");
                return;
            }

            CoroutineRunner.Instance.Stop(CurrentRoutine);
            CurrentRoutine = CoroutineRunner.Instance.Run(CancelWaitForVulnerabilityAfter1sec());
        }

        private static IEnumerator CancelWaitForVulnerabilityAfter1sec()
        {
            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            if (infoText == null)
            {
                yield break;
            }

            GameManager.Instance.player.isTeleporting = true;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = true;

            infoText.enabled = true;
            infoText.transform.SetParent(UiManager.Instance.encounterWindows.transform);
            infoText.rectTransform.anchorMin = new Vector2(0, 0);
            infoText.rectTransform.anchorMax = new Vector2(0, 0);
            infoText.rectTransform.pivot = new Vector2(0, 0);
            infoText.rectTransform.anchoredPosition = new Vector2(100, 100);
            infoText.alignment = TMPro.TextAlignmentOptions.Center;
            infoText.fontSize = 36;
            infoText.color = Color.white;

            float timer = 0.3f;
            while (timer > 0f)
            {
                int seconds = Mathf.FloorToInt(timer);
                int milliseconds = Mathf.FloorToInt((timer - seconds) * 1000);
                infoText.text = $"You will be vulnerable in {seconds.ToString("D2")}:{milliseconds.ToString("D3")} secondes";
                yield return null;
                timer -= Time.deltaTime;

                if (!Plugin.Instance.IS_MANUAL_INVINCIBLE)
                {
                    infoText.enabled = false;
                    Plugin.Instance.NetPlayersDisplayer.Show();
                    yield break;
                }
            }

            GameManager.Instance.player.isTeleporting = false;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = false;
            infoText.enabled = false;

            Plugin.Instance.NetPlayersDisplayer.Show();
        }

        private static IEnumerator Wait5SecBeforeCanTakeDamageAgain(ChestWindowUi __instance)
        {
            if (infoText == null)
            {
                var go = new GameObject("InfoText");
                UnityEngine.GameObject.DontDestroyOnLoad(go);
                infoText = go.AddComponent<TMPro.TextMeshProUGUI>();
            }

            if (infoText == null)
            {
                yield break;
            }

            GameManager.Instance.player.isTeleporting = true;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = true;

            Plugin.Instance.NetPlayersDisplayer.Hide();

            infoText.enabled = true;
            infoText.transform.SetParent(__instance.transform);
            infoText.rectTransform.anchorMin = new Vector2(0, 0);
            infoText.rectTransform.anchorMax = new Vector2(0, 0);
            infoText.rectTransform.pivot = new Vector2(0, 0);
            infoText.rectTransform.anchoredPosition = new Vector2(100, 100);
            infoText.alignment = TMPro.TextAlignmentOptions.Center;
            infoText.fontSize = 36;

            float timer = 5f;
            while (timer > 0f)
            {
                int seconds = Mathf.FloorToInt(timer);
                int milliseconds = Mathf.FloorToInt((timer - seconds) * 1000);
                infoText.text = $"You will be vulnerable in {seconds.ToString("D2")}:{milliseconds.ToString("D3")} secondes";
                yield return null;
                timer -= Time.deltaTime;

                if (!Plugin.Instance.IS_MANUAL_INVINCIBLE)
                {
                    infoText.enabled = false;
                    yield break;
                }
            }

            GameManager.Instance.player.isTeleporting = false;
            Plugin.Instance.IS_MANUAL_INVINCIBLE = false;

            infoText.text = "You are now vulnerable!";
            infoText.color = Color.red;
        }
    }
}
