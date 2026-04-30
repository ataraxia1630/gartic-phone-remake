using Fusion;

namespace GarticPhone.Network.Data
{
    public struct GuessEntry : INetworkStruct
    {
        public PlayerRef Owner;
        public NetworkString<_64> Text;

        public static GuessEntry Empty(PlayerRef owner)
        {
            return new GuessEntry { Owner = owner, Text = new NetworkString<_64>("") };
        }
    }
}
