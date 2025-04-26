using System;

namespace RateMonitor.Model
{
    public class EntityRecord
    {
        public const int MAX_WORKSTATE = 20;
        public readonly static string[] workStateTexts = new string[MAX_WORKSTATE];

        public static void InitStrings(bool isZHCN)
        {
            workStateTexts[(int)EWorkingState.Running] = "正常运转".Translate();
            workStateTexts[(int)EWorkingState.Inefficient] = isZHCN ? "低效" : "Inefficient".Translate();
            workStateTexts[(int)EWorkingState.Idle] = "待机".Translate();
            workStateTexts[(int)EWorkingState.Removed] = "无法监控".Translate();
            workStateTexts[(int)EWorkingState.Full] = "产物堆积".Translate();
            workStateTexts[(int)EWorkingState.Lack] = "缺少原材料".Translate();
            workStateTexts[(int)EWorkingState.LackInc] = isZHCN ? "缺少增产剂" : "Lack of proliferator".Translate();
            workStateTexts[(int)EWorkingState.LackMatrix] = "矩阵不足".Translate();
            workStateTexts[(int)EWorkingState.MinerSlowMode] = isZHCN ? "减速模式" : "Slow Mode".Translate();
            workStateTexts[(int)EWorkingState.EjectorNoOrbit] = "轨道未设置".Translate();
            workStateTexts[(int)EWorkingState.EjectorBlocked] = "路径被遮挡".Translate();
            workStateTexts[(int)EWorkingState.EjectorAngleLimit] = "俯仰限制".Translate();
            workStateTexts[(int)EWorkingState.SiloNoNode] = "待机无需求".Translate();
            workStateTexts[(int)EWorkingState.GammaNoLens] = isZHCN ? "缺少透镜" : "Lack of lens".Translate();
            workStateTexts[(int)EWorkingState.GammaWarmUp] = isZHCN ? "热机中" : "Warm Up".Translate();
            workStateTexts[(int)EWorkingState.NeedFuel] = "需要燃料".Translate();

            workStateTexts[(int)EWorkingState.Error] = "Error";
        }

        public int entityInfoIndex;
        public int entityId;
        public EWorkingState worksate;
        public int itemId;

        // 判定可以參考SingleProducerStatPlan

        public override string ToString()
        {
            return workStateTexts[(int)worksate];
        }

        public EntityRecord(PlanetFactory factory, int entityId, int entityInfoIndex, float workingRatio, ProductionProfile profile)
        {
            this.entityInfoIndex = entityInfoIndex;
            this.entityId = entityId;
            worksate = workingRatio < float.Epsilon ? EWorkingState.Idle : EWorkingState.Inefficient;
            itemId = 0;

            if (entityId < 0 || entityId >= factory.entityPool.Length)
            {
                this.entityId = 0;
                worksate = EWorkingState.Removed;
                return;
            }
            ref var entity = ref factory.entityPool[entityId];
            if (entity.id != entityId)
            {
                this.entityId = 0;
                worksate = EWorkingState.Removed;
                return;
            }

            try
            {
                profile.entityProcessor.DetermineWorkState(factory, entityId, profile.incLevel, this);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug("EntityRecord error!");
                Plugin.Log.LogDebug(ex);
                worksate = EWorkingState.Error;
            }
        }
    }
}
