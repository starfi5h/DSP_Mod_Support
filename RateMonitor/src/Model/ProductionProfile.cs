using RateMonitor.Model.Processor;
using System;
using System.Collections.Generic;

namespace RateMonitor.Model
{
    public class ProductionProfile : IComparable
    {
        public IEntityProcessor entityProcessor; //策略模式
        public int protoId;  // 建築物品id
        public int recipeId; // 配方id
        public bool accMode; // true:加速模式 false:增產模式
        public bool incUsed; // 是否使用增產劑(默認由第一個機器決定)
        public int incLevel; // 當前使用增產劑等級。不使用=0
        public float workEnergyW; // 以W為單位每個工廠的最大耗能(包含增產劑)
        public float idleEnergyW;

        // Calculate result
        public readonly List<int> itemIds = new(); // 原料/產物物品id
        public readonly List<float> itemRefSpeeds = new(); // 淨速率 Net ref speed per machine
        public int productCount; // 產品數
        public int materialCount; // 原料數 
        public float incCost; // 原料消耗的增產點數 = itemRefSpeeds * incLevel
        public int highestProductId; // 代表產物id

        // RealTime
        public readonly List<int> entityIds = new();
        public readonly List<EntityRecord> entityRecords = new();
        public int TotalMachineCount => entityIds.Count;
        public float WorkingMachineCount { get; set; } // 等效工作中的機器個數(由外部CalculateEstRate更新)

        // UI
        private bool isEntityRecordsDirty = true;
        private string recordSummary = "";


        public static long GetHash(PlanetFactory factory, in EntityData entityData)
        {
            long hashId = entityData.protoId;
            if (entityData.assemblerId > 0)
            {
                ref var ptr = ref factory.factorySystem.assemblerPool[entityData.assemblerId];
                hashId |= (long)ptr.recipeId << 32;
                if ((ptr.recipeExecuteData?.productive ?? false) && !ptr.forceAccMode) hashId = -hashId;
            }
            else if (entityData.labId > 0)
            {
                ref var ptr = ref factory.factorySystem.labPool[entityData.labId];
                hashId |= (long)ptr.recipeId << 32;
                if ((ptr.recipeExecuteData?.productive ?? false) && !ptr.forceAccMode) hashId = -hashId;
            }
            else if (entityData.minerId > 0)
            {
                // For vein and oil type, the miner production is not uniform, so they need to separate
                ref var ptr = ref factory.factorySystem.minerPool[entityData.minerId];
                if (ptr.type != EMinerType.Water) hashId |= (long)entityData.minerId << 32;
            }
            else if (entityData.powerGenId > 0)
            {
                ref var ptr = ref factory.powerSystem.genPool[entityData.powerGenId];
                if (ptr.gamma) return hashId;
                // For fuel generator, add fuelId too
                hashId |= (long)ptr.fuelId << 32;
            }
            else if (entityData.powerExcId > 0)
            {
                ref var ptr = ref factory.powerSystem.excPool[entityData.powerExcId];
                if (ptr.state < 0f) hashId = -hashId;
            }
            else if (entityData.spraycoaterId > 0)
            {
                ref var ptr = ref factory.cargoTraffic.spraycoaterPool[entityData.spraycoaterId];
                int beltSpeed = factory.cargoTraffic.beltPool[ptr.cargoBeltId].speed;
                int beltStack = SpraycoaterProcessor.TryGetCargoStack(factory, in ptr);
                int incItemId = ptr.incItemId;
                hashId |= (long)incItemId << 36 | (long)beltSpeed << 32 | (long)beltStack << 28;
            }
            return hashId;
        }

        public ProductionProfile(PlanetFactory factory, in EntityData entityData, int globalIncLevel, bool forceInc)
        {
            entityProcessor = EntityProcessorManager.GetProcessor(entityData);
            protoId = entityData.protoId;
            var desc = LDB.items.Select(protoId)?.prefabDesc;
            if (desc != null && desc.isPowerConsumer)
            {
                workEnergyW = desc.workEnergyPerTick * 60f;
                idleEnergyW = desc.idleEnergyPerTick * 60f;
            }
            incLevel = globalIncLevel;

            // RefSpeed的計算參考ProductionExtraInfoCalculator.CalculateFactory
            if (entityData.assemblerId > 0)
            {
                ref var ptr = ref factory.factorySystem.assemblerPool[entityData.assemblerId];
                recipeId = ptr.recipeId;
                accMode = !((ptr.recipeExecuteData?.productive ?? false) && !ptr.forceAccMode);
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // AssemblerComponent.SetPCState
            }
            else if (entityData.labId > 0)
            {
                ref var ptr = ref factory.factorySystem.labPool[entityData.labId];
                recipeId = ptr.recipeId;
                accMode = !((ptr.recipeExecuteData?.productive ?? false) && !ptr.forceAccMode);
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // LabComponent.SetPCState
            }
            else if (entityData.minerId > 0)
            {
                ref var ptr = ref factory.factorySystem.minerPool[entityData.minerId];
                incUsed = false;
                incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
                if (desc?.isVeinCollector ?? false) workEnergyW *= 3; // TODO: Get the real multiplier (OnMaxChargePowerSliderValueChange)
            }
            else if (entityData.fractionatorId > 0)
            {
                ref var ptr = ref factory.factorySystem.fractionatorPool[entityData.fractionatorId];
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // FractionatorComponent.SetPCState
            }
            else if (entityData.ejectorId > 0)
            {
                ref var ptr = ref factory.factorySystem.ejectorPool[entityData.ejectorId];
                accMode = true;
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // EjectorComponent.SetPCState
            }
            else if (entityData.siloId > 0)
            {
                ref var ptr = ref factory.factorySystem.siloPool[entityData.siloId];
                accMode = true;
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // SiloComponent.SetPCState
            }
            else if (entityData.powerGenId > 0)
            {
                ref var ptr = ref factory.powerSystem.genPool[entityData.powerGenId];
                bool useLen = ptr.catalystPoint > 0 || CalDB.ForceGammaCatalyst;
                accMode = true;
                incUsed = ptr.catalystIncLevel > 0 || forceInc;
                if (!useLen) incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
            }
            else if (entityData.powerExcId > 0)
            {
                ref var ptr = ref factory.powerSystem.excPool[entityData.powerExcId];
                accMode = true;
                int localInc = 0;
                if (ptr.fullCount > 0) localInc = ptr.fullInc / ptr.fullCount;
                if (ptr.emptyCount > 0) localInc = ptr.emptyInc / ptr.emptyCount;
                incUsed = localInc > 0 || forceInc;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
            }
            else if (entityData.spraycoaterId > 0)
            {
                ref var ptr = ref factory.cargoTraffic.spraycoaterPool[entityData.spraycoaterId];
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                entityProcessor.CalculateRefSpeed(factory, entityData.id, this);
            }
        }

        public int CompareTo(object obj)
        {
            ProductionProfile statItem = obj as ProductionProfile;

            if (highestProductId != statItem.highestProductId) return highestProductId - statItem.highestProductId;
            if (recipeId != statItem.recipeId) return recipeId - statItem.recipeId;
            if (protoId != statItem.protoId) return protoId - statItem.protoId;
            if (accMode != statItem.accMode) return accMode ? -1 : 1;
            return 0;
        }

        public void ResetTimer()
        {
            entityRecords.Clear();
            isEntityRecordsDirty = true;
        }

        public void AddEnittyRecord(EntityRecord entityRecord)
        {
            entityRecords.Add(entityRecord);
            isEntityRecordsDirty = true;
        }

        public string GetRecordSummary()
        {
            if (isEntityRecordsDirty)
            {
                // Lazy-loading: calculate only when need. refresh when data is dirty
                isEntityRecordsDirty = false;

                int idleMachineCount = 0;
                var countArray = new int[EntityRecord.MAX_WORKSTATE];
                var lackItems = new List<int>();
                var lackIncItems = new List<int>();

                foreach (var entityRecord in entityRecords)
                {
                    countArray[(int)entityRecord.worksate]++;
                    idleMachineCount++;
                    if (entityRecord.worksate == EWorkingState.Lack || entityRecord.worksate == EWorkingState.LackInc)
                    {
                        // record the itemId info
                        var itemIds = entityRecord.worksate == EWorkingState.Lack ? lackItems : lackIncItems;
                        if (!itemIds.Contains(entityRecord.itemId)) itemIds.Add(entityRecord.itemId);
                    }
                }
                countArray[(int)EWorkingState.Running] = TotalMachineCount - idleMachineCount;

                recordSummary = "";
                for (int i = 0; i < EntityRecord.MAX_WORKSTATE; i++)
                {
                    if (countArray[i] == 0) continue;

                    if (i == (int)EWorkingState.Lack || i == (int)EWorkingState.LackInc)
                    {
                        string itemStrings = "";
                        var itemIds = i == (int)EWorkingState.Lack ? lackItems : lackIncItems;
                        foreach (int itemId in itemIds)
                        {
                            if (itemId == 0) continue;
                            itemStrings += LDB.ItemName(itemId) + ",";
                        }
                        if (string.IsNullOrWhiteSpace(itemStrings))
                        {
                            recordSummary += $"{EntityRecord.workStateTexts[i]}[{countArray[i]}]  ";
                        }
                        else
                        {
                            recordSummary += $"{EntityRecord.workStateTexts[i]}[{countArray[i]}]({itemStrings.TrimEnd(',')})  ";
                        }
                    }
                    else
                    {
                        recordSummary += $"{EntityRecord.workStateTexts[i]}[{countArray[i]}]  ";
                    }
                }                
            }
            return recordSummary;
        }

        public void AddRefSpeed(int itemId, float refSpeed)
        {
            if (refSpeed > 0f && itemId > highestProductId) highestProductId = itemId;
            if (refSpeed < 0f) incCost += incLevel * refSpeed;
            for (int i = 0; i < itemIds.Count; i++)
            {
                if (itemIds[i] == itemId)
                {
                    itemRefSpeeds[i] += refSpeed;
                    return;
                }
            }
            itemIds.Add(itemId);
            itemRefSpeeds.Add(refSpeed);
            if (refSpeed >= 0f) productCount++;
            if (refSpeed <= 0f) materialCount++;
        }
    }
}
