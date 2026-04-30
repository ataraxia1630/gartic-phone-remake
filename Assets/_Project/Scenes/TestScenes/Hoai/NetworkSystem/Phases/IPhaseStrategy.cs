namespace GarticPhone.Network.Phases
{
    public interface IPhaseStrategy
    {
        PhaseType Type { get; }
        void OnEnter(PhaseManager manager);
        void Tick(PhaseManager manager);
        void OnExit(PhaseManager manager);
    }
}
