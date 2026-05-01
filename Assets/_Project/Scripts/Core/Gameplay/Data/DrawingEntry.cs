using Fusion;

namespace InkEcho.Network.Data
{
    public struct DrawingEntry : INetworkStruct
    {
        public PlayerRef Owner;
        public ulong Hash;
        public ushort StrokeCount;

        public static DrawingEntry Empty(PlayerRef owner)
        {
            return new DrawingEntry { Owner = owner, Hash = 0UL, StrokeCount = 0 };
        }
    }
}
