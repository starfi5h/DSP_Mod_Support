namespace RateMonitor.Model.Processor
{
    public class PowerGenProcessor : IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.powerGenId <= 0) return;
            ref var ptr = ref factory.powerSystem.genPool[entityData.powerGenId];

            bool useLen = ptr.catalystPoint > 0 || CalDB.ForceGammaCatalyst;
            if (ptr.gamma)
            {
                if (ptr.productId == 1208 && ptr.productHeat != 0L)
                {
                    //float refSpeed = (3600f * ptr.capacityCurrentTick) / ptr.productHeat; //遊戲的參考速率算法,會受戴森球影響
                    if (useLen)
                    {
                        float accMul = 1f + (float)Cargo.accTableMilli[profile.incLevel];
                        float refSpeed = 12f * accMul;  //直接用常數計算臨界光子產量
                        profile.AddRefSpeed(1208, refSpeed); //臨界光子
                        //AddRefSpeed(1209, -0.1f); // 引力透鏡
                    }
                    else
                    {
                        profile.AddRefSpeed(1208, 6f); //臨界光子
                    }
                }
            }
            else
            {
                var fuelHeat = ptr.fuelHeat;
                if (fuelHeat <= 0L) // 燃燒最後一個留存的燃料 fuel item count = 0
                {
                    fuelHeat = LDB.items.Select(ptr.curFuelId)?.HeatValue ?? 0L;
                    if (fuelHeat == 0L) return; // Should not reach in normal case
                }
                float refSpeed = (3600f * ptr.useFuelPerTick) / fuelHeat;
                profile.AddRefSpeed(ptr.curFuelId, -refSpeed);
            }
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.powerGenId <= 0) return 0.0f;
            ref var ptr = ref factory.powerSystem.genPool[entityData.powerGenId];

            float ratio;
            if (ptr.gamma)
            {
                ratio = 0f;
                if (ptr.productHeat > 0f)
                {                   
                    float speed = (float)ptr.capacityCurrentTick / ptr.productHeat * 3600; //當前參考速率
                    if (incLevel > 0) //使用透鏡
                    {
                        float accMul = 1f + (float)Cargo.accTableMilli[incLevel];
                        ratio = speed / (12f * accMul);
                    }
                    else //不使用透鏡
                    {
                        ratio = speed / 6f;
                    }                    
                }
            }
            else
            {
                // 計算燃料發電機工作效率
                if (ptr.curFuelId == 0) return 0f; // 燃料用盡                
                ratio = (float)factory.powerSystem.netPool[ptr.networkId].generaterRatio; // 電網的發電功率
            }
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.powerGenId <= 0) return;
            ref var ptr = ref factory.powerSystem.genPool[entityData.powerGenId];

            entityRecord.worksate = EWorkingState.Inefficient;
            if (ptr.gamma) // 鍋
            {
                if (ptr.productCount >= 20f)
                {
                    entityRecord.itemId = ptr.productId;
                    entityRecord.worksate = EWorkingState.Full; // "产物堆积"
                }
                else if (ptr.catalystPoint == 0)
                {
                    entityRecord.worksate = EWorkingState.GammaNoLens; // "缺少透鏡"
                    entityRecord.itemId = ptr.catalystId;
                }
                else if (ptr.warmup < 0.999f)
                {
                    entityRecord.worksate = EWorkingState.GammaNoLens; // 熱機中
                }
                else
                {
                    entityRecord.worksate = EWorkingState.Inefficient; // 可能是戴森球供電不足, 不顯示
                }
            }
            else
            {
                entityRecord.worksate = EWorkingState.Inefficient; // 默認是電力充足而低速消耗, 此情況不顯示

                if (ptr.curFuelId == 0 && ptr.fuelEnergy == 0) // "需要燃料"
                {
                    entityRecord.worksate = EWorkingState.NeedFuel;
                }
            }
        }
    }
}
