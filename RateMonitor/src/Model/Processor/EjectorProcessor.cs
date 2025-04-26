namespace RateMonitor.Model.Processor
{
    public class EjectorProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.ejectorId <= 0) return;
            ref var ptr = ref factory.factorySystem.ejectorPool[entityData.ejectorId];

            float accMul = 1f + (float)Cargo.accTableMilli[profile.incLevel];
            float refSpeed = 36000000f / (ptr.chargeSpend + ptr.coldSpend);
            refSpeed = (profile.incUsed ? (refSpeed * accMul) : refSpeed);
            profile.AddRefSpeed(ptr.bulletId, -refSpeed);
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.ejectorId <= 0) return 0.0f;
            ref var ptr = ref factory.factorySystem.ejectorPool[entityData.ejectorId];

            float ratio;
            if (ptr.direction == 0) ratio = 0f;
            else if (ptr.incLevel < incLevel) ratio = 0.5f;
            else ratio = 1f;
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.ejectorId <= 0) return;
            ref var ptr = ref factory.factorySystem.ejectorPool[entityData.ejectorId];

            // UIptrWindow._OnUpdate
            if (ptr.direction != 0f) //可弹射
            {
                entityRecord.itemId = ptr.bulletId;
                entityRecord.worksate = EWorkingState.LackInc; //缺乏增產劑
            }
            else if (ptr.runtimeOrbitId == 0)
            {
                entityRecord.worksate = EWorkingState.EjectorNoOrbit; // 轨道未设置
            }
            else if (ptr.bulletCount == 0)
            {
                entityRecord.worksate = EWorkingState.Lack; //缺少弹射物
                entityRecord.itemId = ptr.bulletId;
            }
            else if (ptr.targetState == EjectorComponent.ETargetState.Blocked)
            {
                entityRecord.worksate = EWorkingState.EjectorBlocked; // 路径被遮挡
            }
            else if (ptr.targetState == EjectorComponent.ETargetState.AngleLimit)
            {
                entityRecord.worksate = EWorkingState.EjectorAngleLimit; // 俯仰限制
            }
        }
    }
}
