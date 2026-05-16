using Fusion;

namespace InkEcho.Network.Data
{
    public struct PromptEntry : INetworkStruct
    {
        public PlayerRef Owner;
        public NetworkString<_64> Text;

        public static PromptEntry Empty(PlayerRef owner)
        {
            return new PromptEntry { Owner = owner, Text = new NetworkString<_64>("") };
        }
    }
}
