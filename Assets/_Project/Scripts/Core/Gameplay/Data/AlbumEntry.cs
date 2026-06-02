using Fusion;

namespace InkEcho.Network.Data
{
    public struct AlbumEntry : INetworkStruct
    {
        public byte ChainLinkIndex;
        public byte OriginSlotIndex;
        public PlayerRef OriginPlayer;
        public PlayerRef WorkerPlayer;

        public NetworkString<_64> Prompt;
        public ulong DrawingHash;
        public ushort DrawingStrokes;

        public NetworkString<_32> GuessRole0;
        public NetworkString<_32> GuessRole1;

        public static AlbumEntry Empty(byte chainLink, byte origin, PlayerRef owner)
        {
            return new AlbumEntry
            {
                ChainLinkIndex = chainLink,
                OriginSlotIndex = origin,
                OriginPlayer = owner,
                WorkerPlayer = PlayerRef.None,
                Prompt = new NetworkString<_64>(""),
                DrawingHash = 0UL,
                DrawingStrokes = 0,
                GuessRole0 = new NetworkString<_32>(""),
                GuessRole1 = new NetworkString<_32>("")
            };
        }
    }
}
