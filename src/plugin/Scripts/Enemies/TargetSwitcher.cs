using Assets.Scripts.Actors.Enemies;
using MegabonkTogether.Services;
using Microsoft.Extensions.DependencyInjection;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.Enemies
{
    public class TargetSwitcher : MonoBehaviour
    {
        private Enemy enemy;
        private float timer = 0f;
        private float delay = 0f;
        private float switchMaxDistance = 100f;
        private (Transform transform, Rigidbody rigidBody) currentTarget = (null, null);

        private uint currentTargetNetplayId = 0;
        private (float min, float max) switchIntervalRange = (2f, 6f);
        private IPlayerManagerService playerManagerService;
        private ISynchronizationService synchronizationService;
        private DynamicData enemyData;

        private void Awake()
        {
            playerManagerService = Plugin.Services.GetService<IPlayerManagerService>();
            synchronizationService = Plugin.Services.GetService<ISynchronizationService>();
        }

        public uint StartSwitching(Enemy targetEnemy, bool pickACloseTarget = false)
        {
            enemy = targetEnemy;
            enemyData = DynamicData.For(enemy);
            ResetTimer();

            if (pickACloseTarget)
            {
                PickACloseTarget();
            }
            else
            {
                PickANewTarget();
            }

            return currentTargetNetplayId;
        }

        public void UpdateSwitchIntervalRange(float minSeconds, float maxSeconds)
        {
            switchIntervalRange = (minSeconds, maxSeconds);
        }

        public void UpdateSwitchMaxDistance(float distance)
        {
            switchMaxDistance = distance;
        }

        /// <summary>
        /// Players that can actually be chased right now.
        ///
        /// A player being alive in the player list is not enough: their NetPlayer may not be spawned yet
        /// (level transition), may already be cleaned up, or may be hidden because they died. Following
        /// one of those means chasing a stale position with nobody there, which is what "enemies go to
        /// the wall" looks like, and dereferencing one throws.
        /// </summary>
        private List<(Transform transform, Rigidbody rigidBody, uint netplayId)> GetTargetableCandidates()
        {
            var candidates = new List<(Transform, Rigidbody, uint)>();

            foreach (var player in playerManagerService.GetAllPlayersAlive())
            {
                if (player == null)
                {
                    continue;
                }

                if (playerManagerService.IsRemoteConnectionId(player.ConnectionId))
                {
                    var netplayer = playerManagerService.GetNetPlayerByNetplayId(player.ConnectionId);
                    if (netplayer == null || netplayer.Model == null || netplayer.Rigidbody == null)
                    {
                        continue;
                    }

                    if (!netplayer.Model.activeSelf) //Hidden by NetPlayer.OnDied
                    {
                        continue;
                    }

                    candidates.Add((netplayer.Model.transform, netplayer.Rigidbody, player.ConnectionId));
                }
                else
                {
                    var localPlayer = GameManager.Instance?.player;
                    if (localPlayer == null || localPlayer.playerMovement == null || localPlayer.playerMovement.rb == null)
                    {
                        continue;
                    }

                    candidates.Add((localPlayer.transform, localPlayer.playerMovement.rb, player.ConnectionId));
                }
            }

            return candidates;
        }

        private void PickANewTarget()
        {
            var candidates = GetTargetableCandidates();
            if (candidates.Count == 0)
            {
                return;
            }

            var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            currentTarget = (selected.transform, selected.rigidBody);
            currentTargetNetplayId = selected.netplayId;
        }

        private void PickACloseTarget()
        {
            var candidates = GetTargetableCandidates();
            if (candidates.Count == 0 || enemy == null || enemy.transform == null)
            {
                return;
            }

            var closestDistance = float.MaxValue;
            (Transform transform, Rigidbody rigidBody) closestTarget = (null, null);
            uint closestNetplayId = 0;

            var enemyPosition = enemy.transform.position;

            foreach (var candidate in candidates)
            {
                var distance = Vector3.Distance(enemyPosition, candidate.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = (candidate.transform, candidate.rigidBody);
                    closestNetplayId = candidate.netplayId;
                }
            }

            if (closestTarget.transform == null)
            {
                return;
            }

            currentTarget = closestTarget;
            currentTargetNetplayId = closestNetplayId;
        }

        private void Update()
        {
            if (enemy == null || enemyData == null)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer < delay)
            {
                return;
            }

            try
            {
                if (!synchronizationService.HasNetplaySessionStarted())
                {
                    return;
                }

                PickANewTarget();

                if (currentTarget.transform == null)
                {
                    return;
                }

                if (CanSwitch())
                {
                    enemyData.Set("targetId", currentTargetNetplayId);
                    enemy.target = currentTarget.rigidBody;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"TargetSwitcher could not pick a target: {ex.Message}");
            }
            finally
            {
                //Always rearm. This used to sit on the happy path only, so anything that threw or
                //returned early left timer >= delay and the whole thing retried every single frame,
                //for every enemy that has a switcher
                ResetTimer();
            }
        }

        private bool CanSwitch()
        {
            if (enemy == null || enemy.transform == null || currentTarget.transform == null)
            {
                return false;
            }

            float distance = Vector3.Distance(enemy.transform.position, currentTarget.transform.position);
            return distance <= switchMaxDistance;
        }

        private void ResetTimer()
        {
            timer = 0f;
            delay = UnityEngine.Random.Range(switchIntervalRange.min, switchIntervalRange.max);
        }
    }
}
