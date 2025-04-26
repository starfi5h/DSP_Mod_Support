using RateMonitor.Model;
using UnityEngine;

namespace RateMonitor.UI
{
    public class ProfileEntry
    {
        public readonly ProductionProfile Profile;
        public bool IsExpand { get; set; }
        public bool IsExpandRecords { get; set; }

        static ushort entityCursor = 0;

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
            string referenceRateStr = Utils.RateKMG(Profile.itemRefSpeeds[index] * totalMachineCount * CalDB.CountMultiplier);
            float workingRate = Profile.WorkingMachineCount / totalMachineCount;
            if (ModSettings.ShowRealtimeRate.Value)
            {
                if (ModSettings.ShowWorkingRateInPercentage.Value)
                    referenceRateStr += workingRate > CalDB.WORKING_THRESHOLD ? " (100%)" : " (" + (int)(workingRate * 100) + "%)";
                else
                    referenceRateStr += " (" + Utils.RateKMG(Profile.itemRefSpeeds[index] * Profile.WorkingMachineCount) + ")";
            }
            GUILayout.Label(referenceRateStr, GUILayout.Width(freespace / 3));

            // (Building icon) machine count (working count)
            if (Utils.RecipeExpandButton(Profile.protoId))
            {
                IsExpand = !IsExpand;
            }
            string machineCountStr = "  " + totalMachineCount * CalDB.CountMultiplier;
            if (ModSettings.ShowRealtimeRate.Value)
            {
                machineCountStr += " (" + Utils.KMG(Profile.WorkingMachineCount * CalDB.CountMultiplier) + ")";
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
                float netRefMachineCount = statTable.ItemRefRates[itemId] / profile.itemRefSpeeds[index] * CalDB.CountMultiplier;
                float netEstMachineCount = statTable.ItemEstRates[itemId] / profile.itemRefSpeeds[index] * CalDB.CountMultiplier;
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
                        if (entityRecord.worksate == EWorkingState.Inefficient) continue;
                        Utils.EntityRecordButton(entityRecord);
                    }
                }

                GUILayout.EndVertical();
            }
            else if (profile.entityIds.Count > 0)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(SP.naviToMachineText);
                bool isPressed = false;
                if (GUILayout.Button("<", GUILayout.Width(Utils.BaseScale * 2)))
                {
                    entityCursor = (ushort)((entityCursor + profile.entityIds.Count - 1) % profile.entityIds.Count);
                    isPressed = true;
                }
                if (GUILayout.Button(">", GUILayout.Width(Utils.BaseScale * 2)))
                {
                    entityCursor = (ushort)((entityCursor + 1) % profile.entityIds.Count);
                    isPressed = true;
                }
                if (isPressed)
                {
                    int entityId = profile.entityIds[entityCursor];
                    Utils.NavigateToEntity(Plugin.MainTable.GetFactory(), entityId);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            // Consume items rate(ref + est)
            if (profile.materialCount > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(SP.itemIdConsumeText, GUILayout.Width(Utils.RateWidth + 2));
                for (int i = 0; i < profile.itemRefSpeeds.Count; i++)
                {
                    float refSpeed = profile.itemRefSpeeds[i];
                    if (refSpeed <= 0f)
                    {
                        Utils.FocusItemIconButton(profile.itemIds[i]);
                        string referenceRateStr = " " + Utils.RateKMG(refSpeed * profile.TotalMachineCount * CalDB.CountMultiplier);
                        if (ModSettings.ShowRealtimeRate.Value)
                        {
                            referenceRateStr += "\n(" + Utils.RateKMG(refSpeed * profile.WorkingMachineCount * CalDB.CountMultiplier) + ")";
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
                GUILayout.Label(SP.itemIdProduceText, GUILayout.Width(Utils.RateWidth + 2));
                for (int i = 0; i < profile.itemRefSpeeds.Count; i++)
                {
                    float refSpeed = profile.itemRefSpeeds[i];
                    if (refSpeed >= 0f)
                    {
                        Utils.FocusItemIconButton(profile.itemIds[i]);
                        string referenceRateStr = " " + Utils.RateKMG(refSpeed * profile.TotalMachineCount * CalDB.CountMultiplier);
                        if (ModSettings.ShowRealtimeRate.Value)
                        {
                            referenceRateStr += "\n(" + Utils.RateKMG(refSpeed * profile.WorkingMachineCount * CalDB.CountMultiplier) + ")";
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
