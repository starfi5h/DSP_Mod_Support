namespace RateMonitor.Model.Processor
{
    public class NoneProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            return 1.0f;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            entityRecord.worksate = EWorkingState.Error; // Should not reach
        }
    }
}
