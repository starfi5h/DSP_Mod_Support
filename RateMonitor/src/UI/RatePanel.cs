using RateMonitor.Model;
using System.Collections.Generic;
using UnityEngine;

namespace RateMonitor.UI
{
    public class RatePanel // 速率面板
    {
        public bool IsActive { get; set; }

        Vector2 scrollPosition;
        readonly StatTable statTable;

        static bool itemIdProduceWorkingOnly;
        static bool itemIdConsumeWorkingOnly;
        static bool itemIdIntermediateWorkingOnly;

        public RatePanel(StatTable statTable)
        {
            this.statTable = statTable;
        }

        public void DrawPanel(float ratePanelWidth)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(ratePanelWidth));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (statTable.ItemIdProduce.Count > 0)
            {
                DrawSectionWithToggle(SP.itemIdProduceText, statTable.ItemIdProduce, ref itemIdProduceWorkingOnly);
            }
            if (statTable.ItemIdConsume.Count > 0)
            {
                DrawSectionWithToggle(SP.itemIdConsumeText, statTable.ItemIdConsume, ref itemIdConsumeWorkingOnly);
            }
            if (statTable.ItemIdIntermediate.Count > 0)
            {
                //DrawSection(itemIdIntermediateText, statTable.ItemIdIntermediate);
                DrawSectionWithToggle(SP.itemIdIntermediateText, statTable.ItemIdIntermediate, ref itemIdIntermediateWorkingOnly);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        void DrawSectionWithToggle(string sectionName, List<int> itemIds, ref bool workingOnly)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(sectionName);
            GUILayout.FlexibleSpace();
            workingOnly = GUILayout.Toggle(workingOnly, SP.workingOnlyText);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            DrawList(itemIds, workingOnly);
            GUILayout.EndVertical();
        }

        void DrawList(List<int> itemIds, bool workingOnly)
        {
            foreach (int itemId in itemIds)
            {
                if (workingOnly && statTable.ItemEstRates[itemId] == 0f && !statTable.WorkingItemIds.Contains(itemId)) continue;

                GUILayout.BeginHorizontal();
                Utils.FocusItemIconButton(itemId);
                GUILayout.Label(Utils.RateKMG(statTable.ItemRefRates[itemId] * CalDB.CountMultiplier), GUILayout.MinWidth(Utils.RateWidth));
                if (ModSettings.ShowRealtimeRate.Value)
                {
                    GUILayout.Label(Utils.RateKMG(statTable.ItemEstRates[itemId] * CalDB.CountMultiplier));
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}