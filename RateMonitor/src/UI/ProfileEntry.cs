using System.Collections.Generic;
using UnityEngine;

namespace RateMonitor.UI
{
    public class ProfileEntry
    {
        public readonly ProductionProfile Profile;
        public bool IsExpand { get; set; }
        public bool IsExpandRecords { get; set; }

        public ProfileEntry(ProductionProfile profile)
        {
            Profile = profile;
        }

        public void DrawProfileItem(int index)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            float panelWidth = UIWindow.Instance.ProfilePanelWidth;
            float freespace = panelWidth - Utils.ShortButtonWidth;
            float totalMachineCount = Profile.TotalMachineCount;

            // Item icon: click to focus
            int itemId = Profile.itemIds[index];
            Utils.FocusItemIconButton(itemId);

            // Reference net rate (working percentage)
            string referenceRateStr = Utils.RateKMG(Profile.itemRefSpeeds[index] * totalMachineCount);
            float workingRate = Profile.WorkingMachineCount / totalMachineCount;
            if (ModSettings.ShowRealtimeRate.Value)
            {
                referenceRateStr += workingRate > CalDB.WORKING_THRESHOLD ? " (100%)" : " (" + (int)(workingRate * 100) + "%)";
            }
            GUILayout.Label(referenceRateStr, GUILayout.Width(freespace / 3));

            // (Building icon) machine count (working count)
            if (Utils.RecipeExpandButton(Profile.protoId))
            {
                IsExpand = !IsExpand;
            }
            string machineCountStr = "  " + totalMachineCount;
            if (ModSettings.ShowRealtimeRate.Value)
            {
                machineCountStr += " (" + Utils.KMG(Profile.WorkingMachineCount) + ")";
            }
            GUILayout.Label(machineCountStr, GUILayout.Width(freespace / 3 - 10));

            // Per machine rate
            string PerMachineRateStr = Utils.RateKMG(Profile.itemRefSpeeds[index]);
            if (Profile.incUsed) PerMachineRateStr += "*";
            GUILayout.Label(" x " + PerMachineRateStr);

            GUILayout.EndHorizontal();

            if (IsExpand)
            {
                DrawProfileItemDetail(Profile, index);
            }

            GUILayout.EndVertical();
        }


        void DrawProfileItemDetail(ProductionProfile profile, int index)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Recipe name,  
            GUILayout.BeginHorizontal();
            int itemId = profile.itemIds[index];
            var recipeProto = LDB.recipes.Select(profile.recipeId);
            if (recipeProto != null)
            {
                GUILayout.Label(recipeProto.name);
            }

            // Profilfer setting and incCost
            if (profile.incLevel > 0)
            {
                string incText = "(" + (profile.accMode ? "加速生产".Translate() : "额外产出".Translate()) + ")";
                //incText += " " + Utils.RateKMG(profile.incCost * profile.TotalMachineCount);
                GUILayout.Label(incText);
            }
            GUILayout.FlexibleSpace();

            // Net machine count
            if (ProfilePanel.FocusItmeId != 0 && ProfilePanel.FocusItmeId == itemId)
            {
                var statTable = UIWindow.Instance.Table;
                float netRefMachineCount = statTable.ItemRefRates[itemId] / profile.itemRefSpeeds[index];
                float netEstMachineCount = statTable.ItemEstRates[itemId] / profile.itemRefSpeeds[index];
                string machineCountText = SP.netMachineText + netRefMachineCount.ToString("0.##");
                if (ModSettings.ShowRealtimeRate.Value)
                {
                    machineCountText += " (" + netEstMachineCount.ToString("0.00") + ")";
                }
                GUILayout.Label(machineCountText);
            }
            GUILayout.EndHorizontal();

            // Show record summary and expand toggle
            if (profile.entityRecords.Count > 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label(profile.GetRecordSummary());
                IsExpandRecords = GUILayout.Toggle(IsExpandRecords, SP.expandRecordText, GUILayout.Width(Utils.ShortButtonWidth));
                GUILayout.EndHorizontal();

                if (IsExpandRecords)
                {
                    foreach (var entityRecord in profile.entityRecords)
                    {
                        Utils.EntityRecordButton(entityRecord);
                    }
                }

                GUILayout.EndVertical();
            }

            // Consume items rate(ref + est)
            if (profile.materialCount > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(SP.itemIdConsumeText, GUILayout.Width(Utils.RateWidth));
                for (int i = 0; i < profile.itemRefSpeeds.Count; i++)
                {
                    float refSpeed = profile.itemRefSpeeds[i];
                    if (refSpeed <= 0f)
                    {
                        Utils.FocusItemIconButton(profile.itemIds[i]);
                        string referenceRateStr = " " + Utils.RateKMG(refSpeed * profile.TotalMachineCount);
                        if (ModSettings.ShowRealtimeRate.Value)
                        {
                            referenceRateStr += "\n(" + Utils.RateKMG(refSpeed * profile.WorkingMachineCount) + ")";
                        }
                        GUILayout.Label(referenceRateStr);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            // Produce itmes rate(ref + est)
            if (profile.productCount > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(SP.itemIdProduceText, GUILayout.Width(Utils.RateWidth));
                for (int i = 0; i < profile.itemRefSpeeds.Count; i++)
                {
                    float refSpeed = profile.itemRefSpeeds[i];
                    if (refSpeed >= 0f)
                    {
                        Utils.FocusItemIconButton(profile.itemIds[i]);
                        string referenceRateStr = " " + Utils.RateKMG(refSpeed * profile.TotalMachineCount);
                        if (ModSettings.ShowRealtimeRate.Value)
                        {
                            referenceRateStr += "\n(" + Utils.RateKMG(refSpeed * profile.WorkingMachineCount) + ")";
                        }
                        GUILayout.Label(referenceRateStr);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}
