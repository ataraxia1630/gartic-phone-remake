using Fusion;

namespace GarticPhone.Network.Players
{
    public struct PlayerSlotData : INetworkStruct
    {
        public NetworkString<_16> DisplayName;
        public NetworkBool IsConnected;
        public NetworkBool IsReady;
        public NetworkBool HasSubmittedPhase;
        public byte SlotIndex;
        public byte PartnerSlotIndex;

        public const byte NoPartner = byte.MaxValue;

        public static PlayerSlotData New(string name, byte slotIndex)
        {
            return new PlayerSlotData
            {
                DisplayName = name,
                IsConnected = true,
                IsReady = false,
                HasSubmittedPhase = false,
                SlotIndex = slotIndex,
                PartnerSlotIndex = NoPartner,
            };
        }
    }
}
