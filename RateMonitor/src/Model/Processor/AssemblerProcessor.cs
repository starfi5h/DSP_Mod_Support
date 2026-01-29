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
            if (ptr.recipeExecuteData != null)
            {
                var recipeExecuteData = ptr.recipeExecuteData;
                float baseSpeed = (3600f * ptr.speed) / recipeExecuteData.timeSpend;
                float finalSpeed = baseSpeed;
                if (profile.incUsed)
                {
                    finalSpeed = ((recipeExecuteData.productive && !ptr.forceAccMode) ? finalSpeed : (finalSpeed * accMul));
                }
                for (int i = 0; i < recipeExecuteData.requires.Length; i++)
                {
                    profile.AddRefSpeed(recipeExecuteData.requires[i], -finalSpeed * recipeExecuteData.requireCounts[i]);
                }
                finalSpeed = baseSpeed;
                if (profile.incUsed)
                {
                    finalSpeed = ((recipeExecuteData.productive && !ptr.forceAccMode) ? (finalSpeed * incMul) : (finalSpeed * accMul));
                }
                for (int i = 0; i < recipeExecuteData.products.Length; i++)
                {
                    profile.AddRefSpeed(recipeExecuteData.products[i], finalSpeed * recipeExecuteData.productCounts[i]);
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
            var recipeExecuteData = ptr.recipeExecuteData;

            if (recipeExecuteData == null) // 無配方
            {
                entityRecord.worksate = EWorkingState.Removed;
                return;
            }

            if (ptr.replicating) // 如果在運轉又被送來檢測, 那就是某個原料缺乏增產劑
            {
                entityRecord.worksate = EWorkingState.LackInc;
                for (int i = 0; i < recipeExecuteData.requireCounts.Length; i++)
                {
                    if (ptr.incServed[i] < ptr.served[i] * incLevel)
                    {
                        entityRecord.itemId = recipeExecuteData.requires[i];
                        break;
                    }
                }
                return;
            }

            if (ptr.time >= recipeExecuteData.timeSpend) // 产物堆积
            {
                entityRecord.worksate = EWorkingState.Full;

                if (recipeExecuteData.products.Length == 0)
                    return;
                if (recipeExecuteData.products.Length == 1)
                {
                    entityRecord.itemId = recipeExecuteData.products[0];
                    return;
                }

                // 在有多個產物時，需要找出是那一個產物堆積了
                int productLength = recipeExecuteData.products.Length;
                if (ptr.recipeType == ERecipeType.Assemble)
                {
                    for (int i = 0; i < productLength; i++)
                    {
                        if (ptr.produced[i] > recipeExecuteData.productCounts[i] * 9)
                        {
                            entityRecord.itemId = recipeExecuteData.products[i];
                            return;
                        }
                    }
                }
                if (ptr.recipeType == ERecipeType.Smelt)
                {
                    for (int i = 0; i < productLength; i++)
                    {
                        if (ptr.produced[i] + recipeExecuteData.productCounts[i] > 100)
                        {
                            entityRecord.itemId = recipeExecuteData.products[i];
                            return;
                        }
                    }
                }
                for (int i = 0; i < productLength; i++)
                {
                    if (ptr.produced[i] > recipeExecuteData.productCounts[i] * 19)
                    {
                        entityRecord.itemId = recipeExecuteData.products[i];
                        return;
                    }
                }
                return;
            }
            else // 缺少原材料
            {
                entityRecord.itemId = 0;
                for (int i = 0; i < recipeExecuteData.requireCounts.Length; i++)
                {
                    if (ptr.served[i] < recipeExecuteData.requireCounts[i])
                    {
                        entityRecord.itemId = recipeExecuteData.requires[i];
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
