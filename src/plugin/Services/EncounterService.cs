using UnityEngine;
using System.Collections.Concurrent;

namespace MegabonkTogether.Services
{
    public interface IEncounterService
    {
        public bool IsClosable();

        /// <summary>
        /// Records that <paramref name="playerId"/> has finished <paramref name="completedCount"/> encounters
        /// </summary>
        public void AddClosedEncounterForPlayer(uint playerId, uint completedCount);

        /// <summary>
        /// Marks the local player as done with the encounter it is currently resolving and
        /// returns the ordinal of that encounter, to be sent to the other players
        /// </summary>
        public uint RegisterLocalEncounterFinished();

        /// <summary>
        /// Ends the current encounter locally and moves on to the next one
        /// </summary>
        public void ClearClosedEncounters();

        public void Close();
        public void Unclose();

        /// <summary>
        /// Arms the failsafe clock on first call, then reports whether the encounter stayed open too long
        /// </summary>
        public bool HasBeenOpenTooLong(float timeoutSeconds);
        public void ResetFailsafe();

        /// <summary>
        /// Session level reset, every player has to start counting from the same place
        /// </summary>
        public void Reset();
    }

    /// <summary>
    /// Keeps track of who is still choosing during a shared experience encounter.
    ///
    /// This used to be a plain set of player ids that got cleared after every encounter, which made a
    /// confirmation impossible to attribute: an EncounterClosed that arrived late, or one sent by a dead
    /// player (a dead player answers for encounters it never even opens, see EncounterWindowPatches),
    /// landed in the set of the *next* encounter. The host then closed that one while somebody was still
    /// choosing, and the real confirmation arrived after the clear and counted for nothing, leaving that
    /// player stuck forever. It got likelier the longer a run lasted, which is exactly what #74 describes.
    ///
    /// Confirmations are therefore counted, not collected: every player reports how many encounters it has
    /// finished in total, the highest report wins, and duplicates or reordered packets are harmless.
    /// </summary>
    internal class EncounterService(IPlayerManagerService playerManagerService) : IEncounterService
    {
        private readonly ConcurrentDictionary<uint, uint> completedCountPerPlayer = new();
        private uint localCompletedCount = 0;
        private bool localHasFinishedCurrentRound = false;
        private bool forceClose = false;
        private float? openedAtRealtime = null;

        /// <summary>
        /// The encounter the local player is resolving right now
        /// </summary>
        private uint CurrentRound => localCompletedCount + 1;

        public void AddClosedEncounterForPlayer(uint playerId, uint completedCount)
        {
            completedCountPerPlayer.AddOrUpdate(
                playerId,
                completedCount,
                (_, previous) => completedCount > previous ? completedCount : previous);
        }

        public uint RegisterLocalEncounterFinished()
        {
            localHasFinishedCurrentRound = true;
            return CurrentRound;
        }

        public void ClearClosedEncounters()
        {
            //Always advance, including when we were force closed without having chosen. Staying behind
            //would mean never waiting for anybody again for the rest of the run
            localCompletedCount++;
            localHasFinishedCurrentRound = false;
            forceClose = false;
            openedAtRealtime = null;
        }

        public bool IsClosable()
        {
            if (forceClose)
            {
                return true;
            }

            if (!localHasFinishedCurrentRound)
            {
                return false; //We are still choosing, nothing to close yet
            }

            var localPlayer = playerManagerService.GetLocalPlayer();
            if (localPlayer == null)
            {
                return false;
            }

            var round = CurrentRound;

            foreach (var player in playerManagerService.GetAllPlayers())
            {
                if (player == null || player.ConnectionId == localPlayer.ConnectionId)
                {
                    continue;
                }

                if (!completedCountPerPlayer.TryGetValue(player.ConnectionId, out var completed) || completed < round)
                {
                    return false;
                }
            }

            return true;
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

        public void Reset()
        {
            completedCountPerPlayer.Clear();
            localCompletedCount = 0;
            localHasFinishedCurrentRound = false;
            forceClose = false;
            openedAtRealtime = null;
        }
    }
}
