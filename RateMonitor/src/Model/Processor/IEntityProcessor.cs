namespace RateMonitor.Model.Processor
{
    public interface IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile);
        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel);
        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord);
    }
}
