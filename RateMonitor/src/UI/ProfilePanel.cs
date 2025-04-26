using RateMonitor.Model;
using System.Collections.Generic;
using UnityEngine;

namespace RateMonitor.UI
{
    public class ProfilePanel // 配方面板
    {
        public static int FocusItmeId { get; set; } // 聚焦的物品id
        public static bool ExpandOnly { get; set; } // 過濾已展開的配方
        public static bool WorkingOnly { get; set; } // 過濾工作中的配方

        readonly List<ProfileEntry> profileEntries = new();
        Vector2 scrollPosition;

        float totalMaxIncCost;
        float workingIncCost;
        float totalMaxPowerCost;
        float workingPowerCost;
        float totalProduction;
        float workingProduction;
        float totalConsumption; 
        float workingConsumption;

        public ProfilePanel(StatTable statTable)
        {
            foreach (var profile in statTable.Profiles)
            {
                profileEntries.Add(new ProfileEntry(profile));
            }
        }

        public void DrawPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawHeader();

            if (FocusItmeId != 0) DrawFocusProfileList();
            else DrawNormalProfileList();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            if (FocusItmeId != 0) // 聚焦物品 (顯示總產出和消耗)
            {
                Utils.FocusItemIconButton(FocusItmeId);

                string productionText = "";
                if (totalConsumption > float.Epsilon)
                {
                    productionText = SP.totalConsumptionText + Utils.RateKMG(totalConsumption * CalDB.CountMultiplier);
                    if (ModSettings.ShowRealtimeRate.Value)
                    {
                        productionText += " (" + Utils.RateKMG(workingConsumption * CalDB.CountMultiplier) + ")";
                    }
                }
                if (totalProduction > float.Epsilon)
                {
                    if (productionText != "") productionText += "\n";
                    productionText += SP.totalProductionText + Utils.RateKMG(totalProduction * CalDB.CountMultiplier);
                    if (ModSettings.ShowRealtimeRate.Value)
                    {
                        productionText += " (" + Utils.RateKMG(workingProduction * CalDB.CountMultiplier) + ")";
                    }
                }
                GUILayout.Label(productionText);
                totalProduction = workingProduction = totalConsumption = workingConsumption = 0f;

                GUILayout.FlexibleSpace();

                ExpandOnly = GUILayout.Toggle(ExpandOnly, SP.expandedOnlyText);
            }
            else // 全局: 計算增產劑消耗
            {
                string proliferatorText = SP.proliferatorCostText + Utils.KMG(-totalMaxIncCost * CalDB.IncToProliferatorRatio * CalDB.CountMultiplier);
                if (ModSettings.ShowRealtimeRate.Value)
                {
                    proliferatorText += " (" + Utils.KMG(-workingIncCost * CalDB.IncToProliferatorRatio * CalDB.CountMultiplier) + ")";
                }
                GUILayout.Label(proliferatorText);
                totalMaxIncCost = workingIncCost = 0f;

                GUILayout.FlexibleSpace();
                ExpandOnly = GUILayout.Toggle(ExpandOnly, SP.expandedOnlyText, GUILayout.Height(Utils.IconHeight));
            }
            GUILayout.EndHorizontal();

            // 耗電計算
            GUILayout.BeginHorizontal();
            string powerText = SP.powerCostText + Utils.KMG(totalMaxPowerCost * CalDB.CountMultiplier);
            powerText += "W";
            if (ModSettings.ShowRealtimeRate.Value)
            {
                powerText += " (" + Utils.KMG(workingPowerCost) + "W)";
            }
            GUILayout.Label(powerText);
            totalMaxPowerCost = workingPowerCost = 0f;
            GUILayout.FlexibleSpace();
            
            WorkingOnly = GUILayout.Toggle(WorkingOnly, SP.workingOnlyText);
            GUILayout.EndHorizontal();            
        }

        void DrawNormalProfileList()
        {
            foreach (var entry in profileEntries)
            {
                var profile = entry.Profile;
                if (ExpandOnly && !entry.IsExpand) continue;
                if (WorkingOnly && profile.WorkingMachineCount < float.Epsilon) continue;

                float totalMachineCount = profile.TotalMachineCount;
                float idleMachineCount = totalMachineCount - profile.WorkingMachineCount;
                totalMaxIncCost += profile.incCost * totalMachineCount;
                workingIncCost += profile.incCost * profile.WorkingMachineCount;
                totalMaxPowerCost += profile.workEnergyW * totalMachineCount;
                workingPowerCost += profile.workEnergyW * profile.WorkingMachineCount + profile.idleEnergyW * idleMachineCount;

                bool hasProduct = false;
                for (int i = 0; i < profile.itemIds.Count; i++)
                {
                    if (profile.itemRefSpeeds[i] > 0f)
                    {
                        entry.DrawProfileItem(i);
                        hasProduct = true;
                    }
                }
                if (!hasProduct)
                {
                    for (int i = 0; i < profile.itemIds.Count; i++)
                    {
                        if (profile.itemRefSpeeds[i] < 0f)
                        {
                            entry.DrawProfileItem(i); // pure consumer e.g. fuel generator
                        }
                    }
                }
            }
        }

        void DrawFocusProfileList()
        {
            foreach (var entry in profileEntries)
            {
                var profile = entry.Profile;
                if (ExpandOnly && !entry.IsExpand) continue;
                if (WorkingOnly && profile.WorkingMachineCount < float.Epsilon) continue;

                float totalMachineCount = profile.TotalMachineCount;
                float idleMachineCount = totalMachineCount - profile.WorkingMachineCount;
                bool containsItem = false;
                for (int i = 0; i < profile.itemIds.Count; i++)
                {
                    if (profile.itemIds[i] != FocusItmeId) continue;

                    if (profile.itemRefSpeeds[i] > float.Epsilon)
                    {
                        entry.DrawProfileItem(i);
                        totalProduction += profile.itemRefSpeeds[i] * totalMachineCount;
                        workingProduction += profile.itemRefSpeeds[i] * profile.WorkingMachineCount;
                    }
                    else if (profile.itemRefSpeeds[i] < -float.Epsilon)
                    {
                        entry.DrawProfileItem(i);
                        totalConsumption += -profile.itemRefSpeeds[i] * totalMachineCount;
                        workingConsumption += -profile.itemRefSpeeds[i] * profile.WorkingMachineCount;
                    }
                    containsItem = true;
                }
                if (containsItem)
                {
                    totalMaxIncCost += profile.incCost * totalMachineCount;
                    workingIncCost += profile.incCost * profile.WorkingMachineCount;
                    totalMaxPowerCost += profile.workEnergyW * totalMachineCount;
                    workingPowerCost += profile.workEnergyW * profile.WorkingMachineCount + profile.idleEnergyW * idleMachineCount;
                }
            }
        }
    }
}
