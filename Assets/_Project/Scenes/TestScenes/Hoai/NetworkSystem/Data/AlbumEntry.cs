using Fusion;

namespace GarticPhone.Network.Data
{
    public struct AlbumEntry : INetworkStruct
    {
        public byte RoundIndex;
        public byte OriginSlotIndex;
        public PlayerRef OriginPlayer;

        public NetworkString<_32> Prompt;
        public ulong DrawingHash;
        public ushort DrawingStrokes;
        public NetworkString<_32> Guess;

        public static AlbumEntry Empty(byte round, byte origin, PlayerRef owner)
        {
            return new AlbumEntry
            {
                RoundIndex = round,
                OriginSlotIndex = origin,
                OriginPlayer = owner,
                Prompt = new NetworkString<_32>("") ,
                DrawingHash = 0UL,
                DrawingStrokes = 0,
                Guess = new NetworkString<_32>("")
            };
        }
    }
}
