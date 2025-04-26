namespace RateMonitor.Model.Processor
{
    public class AssemblerProcessor: IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.assemblerId <= 0) return;
            ref var ptr = ref factory.factorySystem.assemblerPool[entityData.assemblerId];

            float incMul = 1f + (float)Cargo.incTableMilli[profile.incLevel];
            float accMul = 1f + (float)Cargo.accTableMilli[profile.incLevel];
            if (ptr.requires != null && ptr.products != null)
            {
                float baseSpeed = (3600f * ptr.speed) / ptr.timeSpend;
                float finalSpeed = baseSpeed;
                if (profile.incUsed)
                {
                    finalSpeed = ((ptr.productive && !ptr.forceAccMode) ? finalSpeed : (finalSpeed * accMul));
                }
                for (int i = 0; i < ptr.requires.Length; i++)
                {
                    profile.AddRefSpeed(ptr.requires[i], -finalSpeed * ptr.requireCounts[i]);
                }
                finalSpeed = baseSpeed;
                if (profile.incUsed)
                {
                    finalSpeed = ((ptr.productive && !ptr.forceAccMode) ? (finalSpeed * incMul) : (finalSpeed * accMul));
                }
                for (int i = 0; i < ptr.products.Length; i++)
                {
                    profile.AddRefSpeed(ptr.products[i], finalSpeed * ptr.productCounts[i]);
                }
            }
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.assemblerId <= 0) return 0f;
            ref var ptr = ref factory.factorySystem.assemblerPool[entityData.assemblerId];

            float ratio;
            if (!ptr.replicating) ratio = 0f;
            else if (ptr.extraPowerRatio < Cargo.powerTable[incLevel]) ratio = 0.5f;  // 未達指定增產劑等級
            else ratio = 1.0f;
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            // UIAssemblerWindow.stateText
            var entityData = factory.entityPool[entityId];
            if (entityData.assemblerId <= 0)
            {
                entityRecord.worksate = EWorkingState.Removed;
                return;
            }
            ref var ptr = ref factory.factorySystem.assemblerPool[entityData.assemblerId];

            if (ptr.replicating) // 如果在運轉又被送來檢測, 那就是某個原料缺乏增產劑
            {
                entityRecord.worksate = EWorkingState.LackInc;
                for (int i = 0; i < ptr.requireCounts.Length; i++)
                {
                    if (ptr.incServed[i] < ptr.served[i] * incLevel)
                    {
                        entityRecord.itemId = ptr.requires[i];
                        break;
                    }
                }
                return;
            }

            if (ptr.time >= ptr.timeSpend) // 产物堆积
            {
                entityRecord.worksate = EWorkingState.Full;

                if (ptr.products.Length == 0)
                    return;
                if (ptr.products.Length == 1)
                {
                    entityRecord.itemId = ptr.products[0];
                    return;
                }

                // 在有多個產物時，需要找出是那一個產物堆積了
                int productLength = ptr.products.Length;
                if (ptr.recipeType == ERecipeType.Assemble)
                {
                    for (int i = 0; i < productLength; i++)
                    {
                        if (ptr.produced[i] > ptr.productCounts[i] * 9)
                        {
                            entityRecord.itemId = ptr.products[i];
                            return;
                        }
                    }
                }
                if (ptr.recipeType == ERecipeType.Smelt)
                {
                    for (int i = 0; i < productLength; i++)
                    {
                        if (ptr.produced[i] + ptr.productCounts[i] > 100)
                        {
                            entityRecord.itemId = ptr.products[i];
                            return;
                        }
                    }
                }
                for (int i = 0; i < productLength; i++)
                {
                    if (ptr.produced[i] > ptr.productCounts[i] * 19)
                    {
                        entityRecord.itemId = ptr.products[i];
                        return;
                    }
                }
                return;
            }
            else // 缺少原材料
            {
                entityRecord.itemId = 0;
                for (int i = 0; i < ptr.requireCounts.Length; i++)
                {
                    if (ptr.served[i] < ptr.requireCounts[i])
                    {
                        entityRecord.itemId = ptr.requires[i];
                        break;
                    }
                }
                if (entityRecord.itemId != 0)
                {
                    entityRecord.worksate = EWorkingState.Lack;
                    return;
                }
            }
        }
    }
}
