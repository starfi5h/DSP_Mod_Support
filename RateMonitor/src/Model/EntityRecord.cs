using System;

namespace RateMonitor
{
    public class EntityRecord
    {
        public enum EWorkState
        {
            Running = 0, // 正常运转
            Error = 1, // 在計算過程中出錯
            Idle = 2, // 停止运转 (待机)
            Removed = 3, // 无法监控
            Full = 4, // 产物堆积
            Lack = 5, // 缺少原材料            
            LackInc = 6, // 缺少增产剂
            LackMatrix = 7, // 矩阵不足
            SlowMode
        }
        public const int MAX_WORKSTATE = 9;
        public readonly static string[] workStateTexts = new string[MAX_WORKSTATE];        

        public static void InitStrings(bool isZHCN)
        {
            workStateTexts[0] = "正常运转".Translate();
            workStateTexts[1] = "ERROR";
            workStateTexts[2] = "待机".Translate();
            workStateTexts[3] = "无法监控".Translate();
            workStateTexts[4] = "产物堆积".Translate();
            workStateTexts[5] = "缺少原材料".Translate();
            workStateTexts[6] = isZHCN ? "缺少增产剂" : "Lack of proliferator".Translate();
            workStateTexts[7] = "矩阵不足".Translate();
            workStateTexts[8] = isZHCN ? "减速模式" : "Slow Mode".Translate();
        }

        public int entityInfoIndex;
        public int entityId;
        public EWorkState worksate;
        public int itemId;

        // 判定可以參考SingleProducerStatPlan

        public override string ToString()
        {
            return workStateTexts[(int)worksate];
        }

        public EntityRecord(PlanetFactory factory, int entityId, int entityInfoIndex)
        {
            this.entityInfoIndex = entityInfoIndex;
            this.entityId = entityId;
            worksate = EWorkState.Idle;
            itemId = 0;

            if (entityId < 0 || entityId >= factory.entityPool.Length)
            {
                this.entityId = 0;
                worksate = EWorkState.Removed;
                return;
            }
            ref var entity = ref factory.entityPool[entityId];
            if (entity.id != entityId)
            {
                this.entityId = 0;
                worksate = EWorkState.Removed;
                return;
            }

            try
            {
                if (entity.assemblerId > 0)
                {
                    GetAssemblerState(factory.factorySystem.assemblerPool[entity.assemblerId]);
                }
                else if (entity.labId > 0)
                {
                    ref var ptr = ref factory.factorySystem.labPool[entity.labId];
                    if (ptr.matrixMode) GetLabStateMatrixMode(in ptr);
                    else GetLabStateResearchMode(in ptr);
                }
                else if (entity.minerId > 0)
                {
                    GetMinerState(in factory.factorySystem.minerPool[entity.minerId]);
                }
                else if (entity.fractionatorId > 0)
                {
                    GetFractionatorState(in factory.factorySystem.fractionatorPool[entity.fractionatorId]);
                }
                else if (entity.ejectorId > 0)
                {
                    GetEjectorState(in factory.factorySystem.ejectorPool[entity.ejectorId]);
                }
                else if (entity.siloId > 0)
                {
                    GetSiloState(in factory.factorySystem.siloPool[entity.siloId]);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug("EntityRecord error!");
                Plugin.Log.LogDebug(ex);
                worksate = EWorkState.Error;
            }
        }

        void GetAssemblerState(in AssemblerComponent assemblerComponent)
        {
            // UIAssemblerWindow.stateText
            
            if (assemblerComponent.replicating) // 如果在運轉又被送來檢測, 那就是某個原料缺乏增產劑
            {
                worksate = EWorkState.LackInc;
                int maxIncLevel = 1; // TODO: 設定目標增產點數
                for (int i = 0; i < assemblerComponent.requireCounts.Length; i++)
                {
                    if (assemblerComponent.incServed[i] < assemblerComponent.served[i] * maxIncLevel)
                    {
                        itemId = assemblerComponent.requires[i];
                        break;
                    }
                }
                return;
            }

            if (assemblerComponent.time >= assemblerComponent.timeSpend) // 产物堆积
            {
                worksate = EWorkState.Full;

                if (assemblerComponent.products.Length == 0)
                    return;
                if (assemblerComponent.products.Length == 1)
                {
                    itemId = assemblerComponent.products[0];
                    return;
                }

                // 在有多個產物時，需要找出是那一個產物堆積了
                int productLength = assemblerComponent.products.Length;
                if (assemblerComponent.recipeType == ERecipeType.Assemble)
                {
                    for (int i = 0; i < productLength; i++)
                    {
                        if (assemblerComponent.produced[i] > assemblerComponent.productCounts[i] * 9)
                        {
                            itemId = assemblerComponent.products[i];
                            return;
                        }
                    }
                }
                if (assemblerComponent.recipeType == ERecipeType.Smelt)
                {
                    for (int i = 0; i < productLength; i++)
                    {
                        if (assemblerComponent.produced[i] + assemblerComponent.productCounts[i] > 100)
                        {
                            itemId = assemblerComponent.products[i];
                            return;
                        }
                    }
                }
                for (int i = 0; i < productLength; i++)
                {
                    if (assemblerComponent.produced[i] > assemblerComponent.productCounts[i] * 19)
                    {
                        itemId = assemblerComponent.products[i];
                        return;
                    }
                }
                return;
            }
            else // 缺少原材料
            {
                itemId = 0;
                for (int i = 0; i < assemblerComponent.requireCounts.Length; i++)
                {
                    if (assemblerComponent.served[i] < assemblerComponent.requireCounts[i])
                    {
                        itemId = assemblerComponent.requires[i];
                        break;
                    }
                }
                if (itemId != 0)
                {
                    worksate = EWorkState.Lack; 
                    return;
                }
            }
        }

        void GetLabStateMatrixMode(in LabComponent labComponent)
        {
            // 生產模式 UILabWindow.stateText

            if (labComponent.replicating) // 如果在運轉又被送來檢測, 那就是某個原料缺乏增產劑
            {
                worksate = EWorkState.LackInc;
                int maxIncLevel = 1; // TODO: 設定目標增產點數
                for (int i = 0; i < labComponent.requireCounts.Length; i++)
                {
                    if (labComponent.incServed[i] < labComponent.served[i] * maxIncLevel)
                    {
                        itemId = labComponent.requires[i];
                        break;
                    }
                }
                return;
            }

            if (labComponent.time >= labComponent.timeSpend) // 产物堆积
            {
                worksate = EWorkState.Full;

                if (labComponent.products.Length == 0)
                    return;
                if (labComponent.products.Length == 1)
                {
                    itemId = labComponent.products[0];
                    return;
                }

                // 在有多個產物時，需要找出是那一個產物堆積了
                int productLength = labComponent.products.Length;
                for (int i = 0; i < productLength; i++)
                {
                    if (labComponent.produced[i] > labComponent.productCounts[i] * 19)
                    {
                        itemId = labComponent.products[i];
                        return;
                    }
                }
                return;
            }
            else // 缺少原材料
            {
                itemId = 0;
                for (int i = 0; i < labComponent.requireCounts.Length; i++)
                {
                    if (labComponent.served[i] < labComponent.requireCounts[i])
                    {
                        itemId = labComponent.requires[i];
                        break;
                    }
                }
                if (itemId != 0)
                {
                    worksate = EWorkState.Lack;
                    return;
                }
            }
        }

        void GetLabStateResearchMode(in LabComponent labComponent)
        {
            // 科研模式 UILabWindow.stateText 

            if (labComponent.replicating) // 如果在運轉又被送來檢測, 那就是某個原料缺乏增產劑
            {
                worksate = EWorkState.LackInc;
                int maxIncLevel = CalDB.IncLevel;
                for (int i = 0; i < labComponent.requireCounts.Length; i++)
                {
                    if (labComponent.incServed[i] < labComponent.served[i] * maxIncLevel)
                    {
                        itemId = labComponent.requires[i];
                        break;
                    }
                }
                return;
            }

            // 缺少原材料
            itemId = 0;
            for (int i = 0; i < labComponent.matrixServed.Length; i++)
            {
                if (labComponent.matrixServed[i] < labComponent.matrixPoints[i])
                {
                    itemId = labComponent.requires[i];
                    break;
                }
            }
            if (itemId != 0)
            {
                worksate = EWorkState.Lack;
                return;
            }
        }

        void GetMinerState(in MinerComponent miner)
        {
            worksate = EWorkState.SlowMode; //輸出受限
            if (miner.workstate == global::EWorkState.Full) worksate = EWorkState.Full; //堵住
            else if (miner.workstate == global::EWorkState.Idle) worksate = EWorkState.Idle; //已採完
        }

        void GetFractionatorState(in FractionatorComponent fractionator)
        {
            // FractionatorWindow._OnUpdate
            if (fractionator.isWorking)
            {
                itemId = fractionator.fluidId;
                worksate = EWorkState.LackInc;
            }
            else if (fractionator.productOutputCount >= fractionator.productOutputMax || fractionator.fluidOutputCount >= fractionator.fluidOutputMax)
            {
                itemId = fractionator.productId;
                worksate = EWorkState.Full; //产物堆积
            }
            else if (fractionator.fluidId > 0)
            {
                itemId = fractionator.fluidId;
                worksate = EWorkState.Lack; //缺少原材料
                return;
            }
        }

        void GetEjectorState(in EjectorComponent ejector)
        {
            // UIEjectorWindow._OnUpdate
            if (ejector.direction != 0f) //可弹射
            {
                itemId = ejector.bulletId;
                worksate = EWorkState.LackInc;
            }
            else if (ejector.bulletCount == 0) //缺少弹射物
            {
                itemId = ejector.bulletId;
                worksate = EWorkState.Lack;
            }
            // idle:路径被遮挡, 俯仰限制, 轨道未设置
        }

        void GetSiloState(in SiloComponent ejector)
        {
            // UISiloWindow._OnUpdate
            if (ejector.direction != 0f) //可弹射
            {
                itemId = ejector.bulletId;
                worksate = EWorkState.LackInc; //缺乏增產劑
            }
            else if (ejector.bulletCount == 0) 
            {
                itemId = ejector.bulletId;
                worksate = EWorkState.Lack; //缺少火箭
            }
            // idle: 節點已滿
        }
    }
}
