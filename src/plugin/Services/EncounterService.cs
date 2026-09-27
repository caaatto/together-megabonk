using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;

namespace MegabonkTogether.Services
{
    public interface IEncounterService
    {
        public bool IsClosable();

        public void AddClosedEncounterForPlayer(uint playerId);

        public void ClearClosedEncounters();

        public void Close();
        public void Unclose();

        /// <summary>
        /// Arms the failsafe clock on first call, then reports whether the encounter stayed open too long
        /// </summary>
        public bool HasBeenOpenTooLong(float timeoutSeconds);
        public void ResetFailsafe();
    }

    internal class EncounterService(IPlayerManagerService playerManagerService) : IEncounterService
    {
        private readonly ConcurrentDictionary<uint, byte> closedEncounterPerPlayer = new();
        private bool forceClose = false;
        private float? openedAtRealtime = null;

        public void AddClosedEncounterForPlayer(uint playerId)
        {
            closedEncounterPerPlayer.TryAdd(playerId, 0);
        }

        public void ClearClosedEncounters()
        {
            closedEncounterPerPlayer.Clear();
            forceClose = false;
            openedAtRealtime = null;
        }

        public bool IsClosable()
        {
            var allPlayerCount = playerManagerService.GetAllPlayers().Count();
            return closedEncounterPerPlayer.Count >= allPlayerCount || forceClose;
        }

        public void Close()
        {
            forceClose = true;
        }

        public void Unclose()
        {
            forceClose = false;
        }

        /// <summary>
        /// Uses realtimeSinceStartup on purpose: shared experience really pauses the game, so
        /// Time.deltaTime is 0 while an encounter is open and a scaled timer would never advance
        /// </summary>
        public bool HasBeenOpenTooLong(float timeoutSeconds)
        {
            if (timeoutSeconds <= 0f)
            {
                return false;
            }

            openedAtRealtime ??= Time.realtimeSinceStartup;

            return Time.realtimeSinceStartup - openedAtRealtime.Value >= timeoutSeconds;
        }

        public void ResetFailsafe()
        {
            openedAtRealtime = null;
        }
    }
}
