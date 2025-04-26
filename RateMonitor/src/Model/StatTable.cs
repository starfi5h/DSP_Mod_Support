﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RateMonitor.Model
{
    public class StatTable
    {
        struct EntityInfo
        {
            public int entityId;
            public float utilization; // 工作效率
            public EWorkingState workState; // 上一個工作異常狀態
            public ProductionProfile profile;
        }

        public List<ProductionProfile> Profiles { get; private set; } = new(); // 配方特性

        public Dictionary<int, float> ItemRefRates { get; private set; } = new(); // 理論淨生產/消耗
        public List<int> ItemIdProduce { get; private set; } = new(); //生產物品
        public List<int> ItemIdConsume { get; private set; } = new(); //消耗物品
        public List<int> ItemIdIntermediate { get; private set; } = new(); //中間產物

        // Real Time
        public Dictionary<int, float> ItemEstRates { get; private set; } = new(); // 預測淨生產/消耗 = 理論值 * 工作效率
        public HashSet<int> WorkingItemIds { get; private set; } = new(); // 有在工作中的itemId
        public int TotalTick { get; set; } // 總紀錄時間

        PlanetFactory factory;        
        EntityInfo[] entityInfos = new EntityInfo[0];        

        public List<int> GetEntityIds(out PlanetFactory factory)
        {
            factory = this.factory;
            var list = new List<int>();
            for (int i = 0; i < entityInfos.Length; i++)
            {
                list.Add(entityInfos[i].entityId);
            }
            return list;
        }

        public int GetEntityCount()
        {
            return entityInfos.Length;
        }

        public PlanetFactory GetFactory()
        {
            return factory;
        }

        public void Recalculate()
        {
            var entityIds = new List<int>();
            for (int i = 0; i < entityInfos.Length; i++)
            {
                entityIds.Add(entityInfos[i].entityId);
            }
            Initialize(factory, entityIds);
        }

        public void Initialize(PlanetFactory facotry, List<int> entityIds)
        {
            CalDB.Refresh();
            factory = facotry;
            Profiles.Clear();
            TotalTick = 0;
            if (facotry == null)
            {
                CalculateRefRate();
                return;
            }

            // Generate profiles
            var profileDict = new Dictionary<long, ProductionProfile>();
            var entityInfoList = new List<EntityInfo>();
            foreach (int entityId in entityIds)
            {
                if (entityId <= 0 || entityId >= facotry.entityCursor) continue;
                                
                ref EntityData entityData = ref facotry.entityPool[entityId];
                var entityInfo = new EntityInfo
                {
                    entityId = entityId
                };
                long profileHash = ProductionProfile.GetHash(facotry, in entityData);
                if (profileDict.TryGetValue(profileHash, out var profile))
                {
                    entityInfo.profile = profile;
                    profile.entityIds.Add(entityId);
                }
                else
                {
                    entityInfo.profile = new ProductionProfile(facotry, entityData, CalDB.IncLevel, CalDB.ForceInc);
                    profileDict.Add(profileHash, entityInfo.profile);
                    Profiles.Add(entityInfo.profile);
                    entityInfo.profile.entityIds.Add(entityId);
                }
                entityInfoList.Add(entityInfo);
            }
            Profiles.Sort();
            Profiles.Reverse();
            entityInfos = entityInfoList.ToArray();

            CalculateRefRate();
        }

        public void CalculateRefRate()
        {
            // Get max rate from profiles
            ItemRefRates.Clear();
            ItemEstRates.Clear();
            var produceSet = new HashSet<int>();
            var consumeSet = new HashSet<int>();
            var intermediateSet = new HashSet<int>();

            foreach (var profile in Profiles)
            {
                int machineCount = profile.TotalMachineCount;
                for (int i = 0; i < profile.itemIds.Count; i++)
                {
                    int itemId = profile.itemIds[i];
                    float refSpeed = machineCount * profile.itemRefSpeeds[i];                    
                    ItemRefRates.TryGetValue(itemId, out float refValue);
                    ItemRefRates[itemId] = refValue + refSpeed;
                    ItemEstRates.TryGetValue(itemId, out float estValue);
                    ItemEstRates[itemId] = estValue + refSpeed;

                    if (intermediateSet.Contains(itemId))
                    {
                        continue;
                    }
                    if (refSpeed > 0f) produceSet.Add(itemId);
                    else if (refSpeed < 0f) consumeSet.Add(itemId);
                    else intermediateSet.Add(itemId); // 會有這種配方嗎?
                }
            }

            var intersect = produceSet.Intersect(consumeSet).ToList();
            foreach (var itemId in intersect)
            {
                produceSet.Remove(itemId);
                consumeSet.Remove(itemId);
                intermediateSet.Add(itemId);
            }
            ItemIdProduce = produceSet.ToList();
            ItemIdConsume = consumeSet.ToList();
            ItemIdIntermediate = intermediateSet.ToList();
            ItemIdProduce.Sort(); ItemIdProduce.Reverse();
            ItemIdConsume.Sort(); ItemIdConsume.Reverse();
            ItemIdIntermediate.Sort(); ItemIdIntermediate.Reverse();
        }

        public void CalculateEstRate()
        {
            foreach (var profile in Profiles) profile.WorkingMachineCount = 0;
            for (int i = 0; i < entityInfos.Length; i++)
            {
                entityInfos[i].profile.WorkingMachineCount += entityInfos[i].utilization;
            }

            foreach (int itemId in ItemIdProduce) ItemEstRates[itemId] = 0f;
            foreach (int itemId in ItemIdConsume) ItemEstRates[itemId] = 0f;
            foreach (int itemId in ItemIdIntermediate) ItemEstRates[itemId] = 0f;
            foreach (var profile in Profiles)
            {
                if (profile.WorkingMachineCount < float.Epsilon) continue;
                for (int i = 0; i < profile.itemIds.Count; i++)
                {
                    int itemId = profile.itemIds[i];
                    WorkingItemIds.Add(itemId);
                    float estSpeed = profile.WorkingMachineCount * profile.itemRefSpeeds[i];                    
                    ItemEstRates[itemId] += estSpeed;
                }
            }
        }

        public void OnGameTick()
        {
            if (factory == null) return;

            if (++TotalTick >= 108000) // Maximum record period: 30 minutes
            {
                TotalTick -= 15;
            }
            for (int i = 0; i < entityInfos.Length; i++)
            {
                ref var entityInfo = ref entityInfos[i];
                entityInfo.utilization *= (float)(TotalTick - 1) / TotalTick;
                float workingRatio = GetEntityWorkingRatio(entityInfo.entityId, entityInfo.profile);
                entityInfo.utilization += workingRatio / TotalTick;

                if (workingRatio < 0.999f && entityInfo.workState == EWorkingState.Running)
                {
                    var entityRecord = new EntityRecord(factory, entityInfo.entityId, i, workingRatio, entityInfo.profile);
                    entityInfo.workState = entityRecord.worksate;
                    if (entityInfo.workState == EWorkingState.Running) entityInfo.workState = EWorkingState.Idle;
                    entityInfo.profile.AddEnittyRecord(entityRecord);
                }
            }
            if (TotalTick % 15 == 0)
            {
                CalculateEstRate();
            }
        }

        public void OnError()
        {
            factory = null;
        }

        public void ResetTimer()
        {
            TotalTick = 0;
            foreach (var profile in Profiles)
            {
                profile.ResetTimer();
            }
            for (int i = 0; i < entityInfos.Length; i++)
            {
                entityInfos[i].workState = EWorkingState.Running;
            }
        }

        public float GetEntityWorkingRatio(int entityId, ProductionProfile profile)
        {
            if (entityId <= 0 || entityId >= factory.entityCursor) return 0f;
            ref var entityData = ref factory.entityPool[entityId];

            // Reference SetPCState
            int incLevel = profile.incLevel;
            return profile.entityProcessor.CalculateWorkingRatio(factory, entityId, incLevel);
        }

        public void PrintRefRates()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Settings: IncAbility={CalDB.IncLevel}, ForceInc={CalDB.ForceInc}");

            sb.AppendLine($"== Product [{ItemIdProduce.Count}] ==");
            foreach (int itemId in ItemIdProduce)
            {
                sb.AppendLine($"{LDB.ItemName(itemId)}: {ItemRefRates[itemId]:F2}/min");
            }
            sb.AppendLine($"== Ingredients [{ItemIdConsume.Count}] ==");
            foreach (int itemId in ItemIdConsume)
            {
                sb.AppendLine($"{LDB.ItemName(itemId)}: {ItemRefRates[itemId]:F2}/min");
            }
            sb.AppendLine($"== Intermediates [{ItemIdIntermediate.Count}] ==");
            foreach (int itemId in ItemIdIntermediate)
            {
                sb.AppendLine($"{LDB.ItemName(itemId)}: {ItemRefRates[itemId]:F2}/min");
            }
            Plugin.Log.LogDebug(sb.ToString());
        }

        public void PrintProifles(bool showWorkingCount = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Profile count = {Profiles.Count}. Total machine count = {entityInfos.Length}");
#if DEBUG
            foreach (var profile in Profiles)
            {
                sb.Append($"{LDB.ItemName(profile.protoId)} x({profile.TotalMachineCount}) recipe:{LDB.RecipeName(profile.recipeId)}");
                if (showWorkingCount) sb.Append(" workingCount=" + profile.WorkingMachineCount.ToString("F2"));

                sb.Append("\n[");
                for (int i = 0; i < profile.itemIds.Count; i++)
                    sb.Append($" {LDB.ItemName(profile.itemIds[i])}:{profile.itemRefSpeeds[i]:F2}");
                sb.AppendLine("]");
            }
#endif
            Plugin.Log.LogDebug(sb.ToString());
        }
    }
}
