namespace RateMonitor.Model.Processor
{
    public class SiloProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.siloId <= 0) return;
            ref var ptr = ref factory.factorySystem.siloPool[entityData.siloId];

            float accMul = 1f + (float)Cargo.accTableMilli[profile.incLevel];
            float refSpeed = 36000000f / (ptr.chargeSpend + ptr.coldSpend);
            refSpeed = (profile.incUsed ? (refSpeed * accMul) : refSpeed);
            profile.AddRefSpeed(ptr.bulletId, -refSpeed);
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.siloId <= 0) return 0.0f;
            ref var ptr = ref factory.factorySystem.siloPool[entityData.siloId];

            float ratio;
            if (ptr.direction == 0) ratio = 0f;
            else if (ptr.incLevel < incLevel) ratio = 0.5f;
            else ratio = 1f;
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.siloId <= 0) return;
            ref var ptr = ref factory.factorySystem.siloPool[entityData.siloId];

            // UISiloWindow._OnUpdate
            if (ptr.direction != 0f) //可弹射
            {
                entityRecord.worksate = EWorkingState.LackInc; //缺乏增產劑
                entityRecord.itemId = ptr.bulletId;                
            }
            else if (ptr.hasNode)
            {
                entityRecord.worksate = EWorkingState.SiloNoNode; //待机无需求, 節點已滿
            }
            else if (ptr.bulletCount == 0)
            {
                entityRecord.worksate = EWorkingState.Lack; //缺少火箭
                entityRecord.itemId = ptr.bulletId;
            }
        }
    }
}
