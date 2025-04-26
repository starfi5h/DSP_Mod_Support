namespace RateMonitor.Model.Processor
{
    public class FractionatorProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.fractionatorId <= 0) return;
            ref var ptr = ref factory.factorySystem.fractionatorPool[entityData.fractionatorId];

            float accMul = 1f + (float)Cargo.accTableMilli[profile.incLevel];
            float refSpeed = CalDB.BeltSpeeds[2] * CalDB.MaxBeltStack * (profile.incUsed ? accMul : 1f) * ptr.produceProb;
            if (ptr.fluidId > 0)
            {
                profile.AddRefSpeed(ptr.fluidId, -refSpeed);
            }
            if (ptr.productId > 0)
            {
                profile.AddRefSpeed(ptr.productId, refSpeed);
            }
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.fractionatorId <= 0) return 0.0f;
            ref var ptr = ref factory.factorySystem.fractionatorPool[entityData.fractionatorId];

            float ratio;
            if (!ptr.isWorking) ratio = 0f;
            else if (ptr.incLevel < incLevel) ratio = 0.5f;
            else ratio = 1f;
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.fractionatorId <= 0) return;
            ref var ptr = ref factory.factorySystem.fractionatorPool[entityData.fractionatorId];

            // FractionatorWindow._OnUpdate
            if (ptr.isWorking)
            {
                entityRecord.itemId = ptr.fluidId;
                entityRecord.worksate = EWorkingState.LackInc;
            }
            else if (ptr.productOutputCount >= ptr.productOutputMax || ptr.fluidOutputCount >= ptr.fluidOutputMax)
            {
                entityRecord.itemId = ptr.productId;
                entityRecord.worksate = EWorkingState.Full; //产物堆积
            }
            else if (ptr.fluidId > 0)
            {
                entityRecord.itemId = ptr.fluidId;
                entityRecord.worksate = EWorkingState.Lack; //缺少原材料
                return;
            }
        }
    }
}
