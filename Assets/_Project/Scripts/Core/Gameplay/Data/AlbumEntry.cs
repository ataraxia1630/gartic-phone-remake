using Fusion;

namespace InkEcho.Network.Data
{
    public struct AlbumEntry : INetworkStruct
    {
        public byte ChainLinkIndex;
        public byte OriginSlotIndex;
        public PlayerRef OriginPlayer;
        public PlayerRef WorkerPlayer;

        public NetworkString<_32> Prompt;
        public ulong DrawingHash;
        public ushort DrawingStrokes;
        public NetworkString<_32> Guess;

        public static AlbumEntry Empty(byte chainLink, byte origin, PlayerRef owner)
        {
            return new AlbumEntry
            {
                ChainLinkIndex = chainLink,
                OriginSlotIndex = origin,
                OriginPlayer = owner,
                WorkerPlayer = PlayerRef.None,
                Prompt = new NetworkString<_32>(""),
                DrawingHash = 0UL,
                DrawingStrokes = 0,
                Guess = new NetworkString<_32>("")
            };
        }
    }
}
