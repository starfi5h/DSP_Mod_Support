namespace RateMonitor.Model.Processor
{
    public class PowerExcProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.powerExcId <= 0) return;
            ref var ptr = ref factory.powerSystem.excPool[entityData.powerExcId];

            float accMul = 1f + (float)Cargo.accTableMilli[profile.incLevel];
            float rate = accMul * (ptr.energyPerTick * 3600f / ptr.maxPoolEnergy);

            if (ptr.state == 1.0f) // Input
            {
                profile.AddRefSpeed(ptr.emptyId, -rate);
                profile.AddRefSpeed(ptr.fullId, rate);
                profile.workEnergyW = (ptr.energyPerTick * accMul) * 60;
            }
            else if (ptr.state == -1.0f) // Output
            {
                profile.AddRefSpeed(ptr.fullId, -rate);
                profile.AddRefSpeed(ptr.emptyId, rate);
            }
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.powerExcId <= 0) return 0.0f;
            ref var ptr = ref factory.powerSystem.excPool[entityData.powerExcId];

            float ratio = ptr.currEnergyPerTick == 0f ? 0f : 1f; // 簡單的判定是否在工作中。不考慮實際電力情況
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.powerExcId <= 0) return;
            ref var ptr = ref factory.powerSystem.excPool[entityData.powerExcId];

            entityRecord.worksate = EWorkingState.Idle;
            if (ptr.state == 1.0f)
            {
                if (ptr.fullCount >= 20)
                {
                    entityRecord.worksate = EWorkingState.Full;
                    entityRecord.itemId = ptr.fullId;
                }
                else if (ptr.emptyCount == 0)
                {
                    entityRecord.worksate = EWorkingState.Lack;
                    entityRecord.itemId = ptr.emptyId;
                }
            }
            else if (ptr.state == -1.0f)
            {
                if (ptr.emptyCount >= 20)
                {
                    entityRecord.worksate = EWorkingState.Full;
                    entityRecord.itemId = ptr.emptyId;
                }
                else if (ptr.fullCount == 0)
                {
                    entityRecord.worksate = EWorkingState.Lack;
                    entityRecord.itemId = ptr.fullId;
                }
            }
        }
    }
}
