namespace RateMonitor.Model.Processor
{
    public class LabProcessor: IEntityProcessor
    {
        public void CalculateRefSpeed(PlanetFactory factory, int entityId, ProductionProfile profile)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.labId <= 0) return;
            ref var ptr = ref factory.factorySystem.labPool[entityData.labId];

            float incMul = 1f + (float)Cargo.incTableMilli[profile.incLevel];
            float accMul = 1f + (float)Cargo.accTableMilli[profile.incLevel];
            if (ptr.matrixMode && ptr.recipeExecuteData != null)
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
            else if (ptr.researchMode)
            {
                for (int i = 0; i < LabComponent.matrixPoints.Length; i++)
                {
                    if (ptr.techId > 0 && LabComponent.matrixPoints[i] > 0)
                    {
                        float hashSpeed = GameMain.data.history.techSpeed * (float)LabComponent.matrixPoints[i] * 60f * 60f;
                        profile.AddRefSpeed(LabComponent.matrixIds[i], -hashSpeed / 3600f);
                    }
                }
            }
        }

        public float CalculateWorkingRatio(PlanetFactory factory, int entityId, int incLevel)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.labId <= 0) return 0f;
            ref var ptr = ref factory.factorySystem.labPool[entityData.labId];

            float ratio;
            if (!ptr.replicating) ratio = 0f;
            else if (ptr.extraPowerRatio < Cargo.powerTable[incLevel]) ratio = 0.5f;  // 未達指定增產劑等級
            else ratio = 1.0f;
            return ratio;
        }

        public void DetermineWorkState(PlanetFactory factory, int entityId, int incLevel, EntityRecord entityRecord)
        {
            var entityData = factory.entityPool[entityId];
            if (entityData.labId <= 0)
            {
                entityRecord.worksate = EWorkingState.Removed;
                return;
            }
            ref var ptr = ref factory.factorySystem.labPool[entityData.labId];
            if (ptr.matrixMode) GetLabStateMatrixMode(in ptr, incLevel, entityRecord);
            else GetLabStateResearchMode(in ptr, incLevel, entityRecord);
        }

        private void GetLabStateMatrixMode(in LabComponent labComponent, int incLevel, EntityRecord entityRecord)
        {
            var recipeExecuteData = labComponent.recipeExecuteData;

            if (recipeExecuteData == null) // 無配方
            {
                entityRecord.worksate = EWorkingState.Removed;
                return;
            }

            // 生產模式 UILabWindow.stateText
            if (labComponent.replicating) // 如果在運轉又被送來檢測, 那就是某個原料缺乏增產劑
            {
                entityRecord.worksate = EWorkingState.LackInc;
                for (int i = 0; i < recipeExecuteData.requireCounts.Length; i++)
                {
                    if (labComponent.incServed[i] < labComponent.served[i] * incLevel)
                    {
                        entityRecord.itemId = recipeExecuteData.requires[i];
                        break;
                    }
                }
                return;
            }

            if (labComponent.time >= recipeExecuteData.timeSpend) // 产物堆积
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
                for (int i = 0; i < productLength; i++)
                {
                    if (labComponent.produced[i] > recipeExecuteData.productCounts[i] * 19)
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
                    if (labComponent.served[i] < recipeExecuteData.requireCounts[i])
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

        private void GetLabStateResearchMode(in LabComponent labComponent, int incLevel, EntityRecord entityRecord)
        {
            // 科研模式 UILabWindow.stateText
            if (labComponent.replicating)
            {
                // 如果在運轉又被送來檢測, 那就是某個原料缺乏增產劑
                // 具體可以找InternalUpdateResearch中extraPowerRatio的設置
                entityRecord.worksate = EWorkingState.LackInc;
                for (int i = 0; i < labComponent.matrixServed.Length; i++)
                {
                    if (labComponent.matrixIncServed[i] < labComponent.matrixServed[i] * incLevel)
                    {
                        entityRecord.itemId = LabComponent.matrixIds[i];
                        break;
                    }
                }
                return;
            }

            // 缺少原材料
            entityRecord.itemId = 0;
            for (int i = 0; i < labComponent.matrixServed.Length; i++)
            {
                if (labComponent.matrixServed[i] < LabComponent.matrixPoints[i])
                {
                    entityRecord.itemId = LabComponent.matrixIds[i];
                    break;
                }
            }
            if (entityRecord.itemId != 0)
            {
                entityRecord.worksate = EWorkingState.LackMatrix; // 矩阵不足
                return;
            }
        }
    }
}
