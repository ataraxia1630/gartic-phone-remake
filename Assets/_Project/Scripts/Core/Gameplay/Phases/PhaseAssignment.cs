using Fusion;

namespace InkEcho.Network.Phases
{
    public struct PhaseAssignment
    {
        public PlayerRef Worker;
        public PlayerRef SecondaryWorker;
        public byte AlbumOriginSlotIndex;
        public byte ChainLinkIndex;

        public byte PairRole;

        public bool HasSecondary => SecondaryWorker.IsRealPlayer;

        public static PhaseAssignment Solo(PlayerRef worker, byte originSlot, byte chainLink)
        {
            return new PhaseAssignment
            {
                Worker = worker,
                SecondaryWorker = PlayerRef.None,
                AlbumOriginSlotIndex = originSlot,
                ChainLinkIndex = chainLink,
                PairRole = 0
            };
        }

        public static PhaseAssignment Pair(PlayerRef a, PlayerRef b, byte originSlot, byte chainLink, byte role)
        {
            return new PhaseAssignment
            {
                Worker = a,
                SecondaryWorker = b,
                AlbumOriginSlotIndex = originSlot,
                ChainLinkIndex = chainLink,
                PairRole = role
            };
        }
    }
}
