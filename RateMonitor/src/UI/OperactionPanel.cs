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

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(SP.planetSelectDescriptionText);

            // 框選本地全球建築            
            if (GameMain.localPlanet != null)
            {
                var factory = GameMain.localPlanet.factory;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(SP.wholeLocalPlanetText, GUILayout.MaxWidth(Utils.LargeButtonWidth)))
                {
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
                GUILayout.Label("entity count: " + (factory.entityCursor - factory.enemyRecycleCursor));
                GUILayout.EndHorizontal();
            }

            // 框選遠端全球建築 (從統計面板)
            int astroId = 0;
            if (UIRoot.instance.uiGame.statWindow.active) astroId = UIRoot.instance.uiGame.statWindow.astroFilter;
            if (UIRoot.instance.uiGame.controlPanelWindow.active) astroId = UIRoot.instance.uiGame.controlPanelWindow.filter.astroFilter;
            var remotePlanet = GameMain.galaxy.PlanetById(astroId);
            if (remotePlanet != null && remotePlanet.factory != null)
            {
                var factory = remotePlanet.factory;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(SP.wholeRemotePlanetText, GUILayout.MaxWidth(Utils.LargeButtonWidth)))
                {
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
                GUILayout.Label(remotePlanet.displayName + ": " + (factory.entityCursor - factory.enemyRecycleCursor));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

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
                statTable.ResetTimer();
            }
            GUILayout.Label((statTable.TotalTick / 60f).ToString("F2") + "s");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
