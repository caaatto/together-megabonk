using MemoryPack;

namespace MegabonkTogether.Common.Messages.GameNetworkMessages
{
    [MemoryPackable]
    public partial class EncounterClosed : IGameNetworkMessage
    {
        public uint OwnerId { get; set; }

        /// <summary>
        /// How many encounters the sender has finished in total. Counting instead of just signalling
        /// makes a late or duplicated confirmation harmless (see EncounterService)
        /// </summary>
        public uint CompletedCount { get; set; }
    }
}
