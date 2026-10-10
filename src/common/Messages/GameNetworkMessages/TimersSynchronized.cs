using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    /// <summary>
    /// Host authoritative snapshot of the MyTime timers.
    ///
    /// With Shared Experience the game really pauses while a player chooses, and every player pauses
    /// for a different amount of time, so these timers drift apart over a run. That is why one player
    /// sees 00:00 and the final swarm while another still has minutes left on the clock.
    /// Enemy spawning and swarm events already come from the host, so aligning the clients on the
    /// host clock only makes them agree with what the host is already driving.
    /// </summary>
    [MemoryPackable]
    public partial class TimersSynchronized : IGameNetworkMessage
    {
        public float StageTimer { get; set; }
        public float RunTimer { get; set; }
        public float FinalSwarmTimer { get; set; }
        public float DifficultyTimer { get; set; }
        public float CryptTimer { get; set; }
    }
}
