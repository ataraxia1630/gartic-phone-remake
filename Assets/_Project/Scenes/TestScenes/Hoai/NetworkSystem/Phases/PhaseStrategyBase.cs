namespace GarticPhone.Network.Phases
{
    public abstract class PhaseStrategyBase : IPhaseStrategy
    {
        public abstract PhaseType Type { get; }
        public virtual void OnEnter(PhaseManager manager) { }
        public virtual void Tick(PhaseManager manager) { }
        public virtual void OnExit(PhaseManager manager) { }
    }
}
