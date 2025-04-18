using System;
using System.Collections.Generic;

namespace RateMonitor
{
    public class ProductionProfile : IComparable
    {
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
                if (ptr.productive && !ptr.forceAccMode) hashId = -hashId;
            }
            else if (entityData.labId > 0)
            {
                ref var ptr = ref factory.factorySystem.labPool[entityData.labId];
                hashId |= (long)ptr.recipeId << 32;
                if (ptr.productive && !ptr.forceAccMode) hashId = -hashId;
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
            return hashId;
        }

        public ProductionProfile(PlanetFactory factory, in EntityData entityData, int globalIncLevel, bool forceInc)
        {
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
                accMode = !(ptr.productive && !ptr.forceAccMode);
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                CalculateRefSpeed(in ptr, incLevel);                
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // AssemblerComponent.SetPCState
            }
            else if (entityData.labId > 0)
            {
                ref var ptr = ref factory.factorySystem.labPool[entityData.labId];
                recipeId = ptr.recipeId;
                accMode = !(ptr.productive && !ptr.forceAccMode);
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                CalculateRefSpeed(in ptr, incLevel);                
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // LabComponent.SetPCState
            }
            else if (entityData.minerId > 0)
            {
                ref var ptr = ref factory.factorySystem.minerPool[entityData.minerId];
                incUsed = false;
                incLevel = 0;
                CalculateRefSpeed(in ptr, factory);
                if (desc?.isVeinCollector ?? false) workEnergyW *= 3; // TODO: Get the real multiplier (OnMaxChargePowerSliderValueChange)
            }
            else if (entityData.fractionatorId > 0)
            {
                ref var ptr = ref factory.factorySystem.fractionatorPool[entityData.fractionatorId];
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                CalculateRefSpeed(in ptr, incLevel);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // FractionatorComponent.SetPCState
            }
            else if (entityData.ejectorId > 0)
            {
                ref var ptr = ref factory.factorySystem.ejectorPool[entityData.ejectorId];
                accMode = true;
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                CalculateRefSpeed(in ptr, incLevel);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // EjectorComponent.SetPCState
            }
            else if (entityData.siloId > 0)
            {
                ref var ptr = ref factory.factorySystem.siloPool[entityData.siloId];
                accMode = true;
                incUsed = ptr.incUsed | forceInc;
                if (!incUsed) incLevel = 0;
                CalculateRefSpeed(in ptr, incLevel);
                workEnergyW *= (float)Cargo.powerTableRatio[incLevel]; // SiloComponent.SetPCState
            }
            else if (entityData.powerGenId > 0)
            {
                ref var ptr = ref factory.powerSystem.genPool[entityData.powerGenId];
                bool useLen = ptr.catalystPoint > 0 || CalDB.ForceGammaCatalyst;
                accMode = true;
                incUsed = ptr.catalystIncLevel > 0 || forceInc;
                if (!useLen) incLevel = 0;
                CalculateRefSpeed(in ptr, incLevel, useLen);
            }
            else if (entityData.powerExcId > 0)
            {
                ref var ptr = ref factory.powerSystem.excPool[entityData.powerExcId];
                accMode = true;
                int localInc = 0;
                if (ptr.fullCount > 0) localInc = ptr.fullInc / ptr.fullCount;
                if (ptr.emptyCount > 0) localInc = ptr.emptyInc / ptr.emptyCount;
                incUsed = localInc > 0 || forceInc;
                CalculateRefSpeed(in ptr, incLevel);
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
                foreach (var entityRecord in entityRecords)
                {
                    countArray[(int)entityRecord.worksate]++;
                    idleMachineCount++;
                }
                countArray[(int)EntityRecord.EWorkState.Running] = TotalMachineCount - idleMachineCount;

                recordSummary = "";
                for (int i = 0; i < EntityRecord.MAX_WORKSTATE; i++)
                {
                    if (countArray[i] != 0) recordSummary += $"{EntityRecord.workStateTexts[i]}[{countArray[i]}]  ";
                }                
            }
            return recordSummary;
        }

        private void AddRefSpeed(int itemId, float refSpeed)
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

        private void CalculateRefSpeed(in AssemblerComponent ptr, int incLevel)
        {
            float incMul = 1f + (float)Cargo.incTableMilli[incLevel];
            float accMul = 1f + (float)Cargo.accTableMilli[incLevel];
            if (ptr.requires != null && ptr.products != null)
            {
                float baseSpeed = (3600f * ptr.speed) / ptr.timeSpend;
                float finalSpeed = baseSpeed;
                if (incUsed)
                {
                    finalSpeed = ((ptr.productive && !ptr.forceAccMode) ? finalSpeed : (finalSpeed * accMul));
                }
                for (int i = 0; i < ptr.requires.Length; i++)
                {
                    AddRefSpeed(ptr.requires[i], -finalSpeed * ptr.requireCounts[i]);
                }
                finalSpeed = baseSpeed;
                if (incUsed)
                {
                    finalSpeed = ((ptr.productive && !ptr.forceAccMode) ? (finalSpeed * incMul) : (finalSpeed * accMul));
                }
                for (int i = 0; i < ptr.products.Length; i++)
                {
                    AddRefSpeed(ptr.products[i], finalSpeed * ptr.productCounts[i]);
                }
            }
        }

        private void CalculateRefSpeed(in LabComponent ptr, int incLevel)
        {
            float incMul = 1f + (float)Cargo.incTableMilli[incLevel];
            float accMul = 1f + (float)Cargo.accTableMilli[incLevel];
            if (ptr.matrixMode)
            {
                float baseSpeed = (3600f * ptr.speed) / ptr.timeSpend;
                float finalSpeed = baseSpeed;
                if (incUsed)
                {
                    finalSpeed = ((ptr.productive && !ptr.forceAccMode) ? finalSpeed : (finalSpeed * accMul));
                }
                for (int i = 0; i < ptr.requires.Length; i++)
                {
                    AddRefSpeed(ptr.requires[i], -finalSpeed * ptr.requireCounts[i]);
                }
                finalSpeed = baseSpeed;
                if (incUsed)
                {
                    finalSpeed = ((ptr.productive && !ptr.forceAccMode) ? (finalSpeed * incMul) : (finalSpeed * accMul));
                }
                for (int i = 0; i < ptr.products.Length; i++)
                {
                    AddRefSpeed(ptr.products[i], finalSpeed * ptr.productCounts[i]);
                }
            }
            else if (ptr.researchMode)
            {
                for (int i = 0; i < ptr.matrixPoints.Length; i++)
                {
                    if (ptr.techId > 0 && ptr.matrixPoints[i] > 0)
                    {
                        float hashSpeed = GameMain.data.history.techSpeed * (float)ptr.matrixPoints[i] * 60f * 60f;
                        AddRefSpeed(LabComponent.matrixIds[i], -hashSpeed / 3600f);
                    }
                }
            }
        }

        private void CalculateRefSpeed(in MinerComponent ptr, PlanetFactory factory)
        {
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
                AddRefSpeed(itemId, refSpeed);
            }
        }

        private void CalculateRefSpeed(in FractionatorComponent ptr, int incAbility)
        {
            float accMul = 1f + (float)Cargo.accTableMilli[incAbility];

            float refSpeed = CalDB.BeltSpeeds[2] * CalDB.MaxBeltStack * (incUsed ? accMul : 1f) * ptr.produceProb;
            if (ptr.fluidId > 0)
            {
                AddRefSpeed(ptr.fluidId, -refSpeed);
            }
            if (ptr.productId > 0)
            {
                AddRefSpeed(ptr.productId, refSpeed);
            }
        }

        private void CalculateRefSpeed(in EjectorComponent ptr, int incAbility)
        {
            float accMul = 1f + (float)Cargo.accTableMilli[incAbility];

            float refSpeed = 36000000f / (ptr.chargeSpend + ptr.coldSpend);
            refSpeed = (incUsed ? (refSpeed * accMul) : refSpeed);
            AddRefSpeed(ptr.bulletId, -refSpeed);
        }

        private void CalculateRefSpeed(in SiloComponent ptr, int incAbility)
        {
            float accMul = 1f + (float)Cargo.accTableMilli[incAbility];

            float refSpeed = 36000000f / (ptr.chargeSpend + ptr.coldSpend);
            refSpeed = (incUsed ? (refSpeed * accMul) : refSpeed);
            AddRefSpeed(ptr.bulletId, -refSpeed);
        }

        private void CalculateRefSpeed(in PowerGeneratorComponent ptr, int incAbility, bool useLen)
        {
            if (ptr.gamma)
            {
                if (ptr.productId == 1208 && ptr.productHeat != 0L)
                {
                    //float refSpeed = (3600f * ptr.capacityCurrentTick) / ptr.productHeat; //遊戲的參考速率算法,會受戴森球影響
                    if (useLen)
                    {
                        float accMul = 1f + (float)Cargo.accTableMilli[incAbility];
                        float refSpeed = 12f * accMul;  //直接用常數計算臨界光子產量
                        AddRefSpeed(1208, refSpeed); //臨界光子
                        //AddRefSpeed(1209, -0.1f); // 引力透鏡
                    }
                    else
                    {
                        AddRefSpeed(1208, 6f); //臨界光子
                    }
                }
            }
            else
            {
                if (ptr.fuelHeat > 0L && ptr.fuelId > 0)
                {
                    float refSpeed = (3600f * ptr.useFuelPerTick) / ptr.fuelHeat;
                    AddRefSpeed(ptr.fuelId, -refSpeed);
                }
            }
        }

        public void CalculateRefSpeed(in PowerExchangerComponent ptr, int incLevel)
        {
            float accMul = 1f + (float)Cargo.accTableMilli[incLevel];
            float rate = accMul * (ptr.energyPerTick * 3600f / ptr.maxPoolEnergy);

            if (ptr.state == 1.0f) // Input
            {
                AddRefSpeed(ptr.emptyId, -rate);
                AddRefSpeed(ptr.fullId, rate);
                workEnergyW = (ptr.energyPerTick * accMul) * 60;
            }
            else if (ptr.state == -1.0f) // Output
            {
                AddRefSpeed(ptr.fullId, -rate);
                AddRefSpeed(ptr.emptyId, rate);
            }
        }
    }
}
