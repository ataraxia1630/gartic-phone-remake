namespace GarticPhone.Network.GameModes
{
    public static class GameModeFactory
    {
        public static IGameMode Create(GameModeType type)
        {
            switch (type)
            {
                case GameModeType.Sandwich: return new Modes.SandwichGameMode();
                case GameModeType.Coop: return new Modes.CoopGameMode();
                default: return new Modes.SandwichGameMode();
            }
        }
    }
}
