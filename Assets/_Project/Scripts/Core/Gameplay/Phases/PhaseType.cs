namespace InkEcho.Network.Phases
{
    public enum PhaseType : byte
    {
        None = 0,
        Prompt = 1,
        Draw = 2,
        Guess = 3,
        Reveal = 4,
        Observe = 5,
        FinalGuess = 6,
    }
}
