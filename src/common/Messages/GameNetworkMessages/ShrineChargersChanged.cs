using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    /// <summary>
    /// Host authoritative number of players currently standing in a charge shrine zone.
    ///
    /// ChargeShrine has no numPlayers field, unlike BossLamp, so the game never scaled a shrine with
    /// the number of players standing on it. The mod also tracked only a single charger per shrine and
    /// suppressed every additional trigger, so there was nothing to scale with either.
    /// </summary>
    [MemoryPackable]
    public partial class ShrineChargersChanged : IGameNetworkMessage
    {
        public uint ShrineNetplayId { get; set; }
        public byte ChargerCount { get; set; }
    }
}
