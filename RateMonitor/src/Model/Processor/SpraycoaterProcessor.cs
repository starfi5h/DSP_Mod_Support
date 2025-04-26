namespace RateMonitor.Model.Processor
{
    public class SpraycoaterProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.spraycoaterId <= 0) return;
            ref var ptr = ref factory.cargoTraffic.spraycoaterPool[entityData.spraycoaterId];

            float incMul = 1f + (float)Cargo.incTableMilli[profile.incLevel];
            if (ptr.incItemId > 0 && ptr.cargoBeltId > 0) //要有增產劑和輸入帶的才進行計算
            {
                var itemProto = LDB.items.Select(ptr.incItemId);
                if (itemProto != null)
                {
                    // BeltComponent.speed is 1,2,5 so must be multiplied by 6 to get 6,12,30 (cargo/s)
                    // For mk3 belt, max cargo infeed speed = 1800(beltRatePerMin) * 4(beltMaxStack) = 7200/min
                    // The mk3 spray usage = 7200 / (60+15)(numbersOfSprays) = 96/min
                    // Note: Practically infeed speed rarely reach belt limit, so the theory max is often much higher than the real rate
                    int beltSpeed = factory.cargoTraffic.beltPool[ptr.cargoBeltId].speed;
                    int beltRatePerMin = 6 * beltSpeed * 60;
                    int beltStack = TryGetCargoStack(factory, in ptr);
                    int numbersOfSprays = (int)(itemProto.HpMax * incMul);

                    float refSpeed = (beltStack * beltRatePerMin) / numbersOfSprays;
                    profile.AddRefSpeed(ptr.incItemId, -refSpeed);
                }
            }            
        }

        public static int TryGetCargoStack(PlanetFactory factory, in SpraycoaterComponent ptr)
        {
            int stack = CalDB.MaxBeltStack;
            try
            {
                if (ptr.cargoBeltId == 0) return stack;
                ref var beltComponent = ref factory.cargoTraffic.beltPool[ptr.cargoBeltId];
                var cargoPath = factory.cargoTraffic.GetCargoPath(beltComponent.segPathId);

                // Search downstream for cargo stack 從下游找貨物，得到堆疊數
                for (var k = beltComponent.segIndex + beltComponent.segPivotOffset;
                     k <= beltComponent.segIndex + beltComponent.segLength - 1;
                     k++)
                {
                    if (cargoPath.GetCargoAtIndex(k, out Cargo cargo, out _, out _))
                    {
                        return cargo.stack;
                    }
                }
                // Search upstream for cargo stack 從上游找貨物，得到堆疊數
                for (var k = beltComponent.segIndex + beltComponent.segPivotOffset - 1; k >= beltComponent.segIndex; k--)
                {
                    if (cargoPath.GetCargoAtIndex(k, out Cargo cargo, out _, out _))
                    {
                        return cargo.stack;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning("[Error] SpraycoaterProcessor:TryGetCargoStack");
                Plugin.Log.LogWarning(ex);
            }
            return stack;
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.spraycoaterId <= 0) return 0f;
            ref var ptr = ref factory.cargoTraffic.spraycoaterPool[entityData.spraycoaterId];

            float ratio = 1.0f;
            if (ptr.sprayTime >= 10000) ratio = 0f; // 超過冷卻時間，但是下一個貨物還沒送達
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.spraycoaterId <= 0)
            {
                entityRecord.worksate = EWorkingState.Removed;
                return;
            }
            ref var ptr = ref factory.cargoTraffic.spraycoaterPool[entityData.spraycoaterId];

            entityRecord.worksate = EWorkingState.Inefficient;
            if (ptr.incItemId == 0) entityRecord.worksate = EWorkingState.Lack;
            if (incLevel > 0 && !ptr.incUsed) entityRecord.worksate = EWorkingState.LackInc;
        }
    }
}
