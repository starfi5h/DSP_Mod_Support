namespace RateMonitor.Model.Processor
{
    public class MinerProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.minerId <= 0) return;
            ref var ptr = ref factory.factorySystem.minerPool[entityData.minerId];

            // Note: miner output rate is not uniform (scale with veinCount)
            double miningSpeedScale = GameMain.data.history.miningSpeedScale;
            int waterItemId = factory.planet.waterItemId;
            var veinPool = factory.veinPool;

            int itemId = 0;
            float refSpeed = 0f;
            switch (ptr.type)
            {
                case EMinerType.Water:
                    itemId = waterItemId;
                    refSpeed = (float)(3600.0 / ptr.period * miningSpeedScale * ptr.speed);
                    break;
                case EMinerType.Vein:
                    itemId = ((ptr.veinCount > 0) ? veinPool[ptr.veins[ptr.currentVeinIndex]].productId : 0);
                    refSpeed = (float)(3600.0 / ptr.period * miningSpeedScale * ptr.speed * ptr.veinCount);
                    break;
                case EMinerType.Oil:
                    itemId = veinPool[ptr.veins[0]].productId;
                    refSpeed = (float)(3600.0 / ptr.period * miningSpeedScale * ptr.speed * veinPool[ptr.veins[0]].amount * VeinData.oilSpeedMultiplier);
                    break;
            }
            if (itemId > 0)
            {
                profile.AddRefSpeed(itemId, refSpeed);
            }
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.minerId <= 0) return 0.0f;
            ref var ptr = ref factory.factorySystem.minerPool[entityData.minerId];

            float ratio = ptr.workstate > EWorkState.Idle ? ptr.speedDamper * ptr.speed * ptr.speed / 100000000.0f : 0f;
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.minerId <= 0) return;
            ref var ptr = ref factory.factorySystem.minerPool[entityData.minerId];

            entityRecord.worksate = EWorkingState.MinerSlowMode; //輸出受限
            if (ptr.workstate == EWorkState.Full) entityRecord.worksate = EWorkingState.Full; //堵住
            else if (ptr.workstate == EWorkState.Idle) entityRecord.worksate = EWorkingState.Idle; //已採完
        }
    }
}
