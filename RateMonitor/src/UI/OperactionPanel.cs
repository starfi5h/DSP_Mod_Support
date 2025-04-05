using System.Collections.Generic;
using UnityEngine;

namespace RateMonitor.UI
{
    public class OperactionPanel // 操作面板
    {
        public bool IsActive { get; set; }

        readonly StatTable statTable;
        Vector2 scrollPosition;

        public OperactionPanel(StatTable statTable)
        {
            this.statTable = statTable;
        }

        public void DrawPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // 框選全球建築
            GUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button(SP.wholePlanetText, GUILayout.MaxWidth(Utils.LargeButtonWidth)))
            {
                if (GameMain.localPlanet != null)
                {
                    var factory = GameMain.localPlanet.factory;
                    var entityIds = new List<int>();
                    for (int entityId = 1; entityId < factory.entityCursor; entityId++)
                    {
                        if (SelectionTool.ShouldAddObject(factory, entityId)) entityIds.Add(entityId);
                    }
                    if (entityIds.Count > 0)
                    {
                        Plugin.SaveCurrentTable();
                        Plugin.CreateMainTable(factory, entityIds);
                    }
                }
            }
            if (GameMain.localPlanet != null)
            {
                var factory = GameMain.localPlanet.factory;
                GUILayout.Label("total entity count: " + (factory.entityCursor - factory.enemyRecycleCursor));
            }
            GUILayout.EndHorizontal();

            // 載入上一個框選
            GUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button(SP.loadLastText, GUILayout.MaxWidth(Utils.LargeButtonWidth)))
            {
                Plugin.LoadLastTable();
            }
            GUILayout.Label(Plugin.LastStatInfo);
            GUILayout.EndHorizontal();

            // 重設時間
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(SP.resetTimerText, GUILayout.MaxWidth(Utils.LargeButtonWidth)))
            {
                statTable.TotalTick = 0;
                foreach (var profile in statTable.Profiles)
                {
                    profile.Reset();
                }
            }
            GUILayout.Label((statTable.TotalTick / 60f).ToString("F2") + "s");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
