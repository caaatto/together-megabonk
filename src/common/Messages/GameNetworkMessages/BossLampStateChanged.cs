using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    /// <summary>
    /// Host authoritative on/off state of a graveyard boss lamp.
    ///
    /// Only entering and leaving a lamp trigger used to be synchronized, not the lamp turning on or
    /// going out again. A lamp goes out after a locally rolled random delay
    /// (BossLamp.CheckRandomDeactivate), so every player ended up with a different lampCount, and
    /// GraveyardBossRoom derives the boss armor from that count. That is why the boss can keep its
    /// shield on a guest while all four lamps look lit.
    /// </summary>
    [MemoryPackable]
    public partial class BossLampStateChanged : IGameNetworkMessage
    {
        public uint LampNetplayId { get; set; }
        public bool IsTurnedOn { get; set; }
    }
}
