using System;
using UnityEngine;

namespace RateMonitor.UI
{
    public class UIWindow
    {
        public static UIWindow Instance { get; private set; }
        public StatTable Table { get; private set; }
        public float RatePanelWidth { get; private set; } = 190f;
        public float ProfilePanelWidth { get; private set; } = 350f;


        private const int windowId = 432145525;
        private Rect windowRect = new (100, 150, 580, 400);
        private bool isResizing;

        private RatePanel ratePanel;
        private ProfilePanel profilePanel;
        private OperactionPanel operactionPanel;
        private SettingPanel settingPanel;
        private string titleText = "";

        public static void OnGUI()
        {
            if (Plugin.MainTable == null) return;
            if (Instance == null) Instance = new UIWindow();
            if (Instance.Table != Plugin.MainTable) Instance.Init(Plugin.MainTable);

            // Make the window draggable and get the returned position
            Color originalColor = GUI.backgroundColor; // Save the original color
            GUI.backgroundColor = new Color(1f, 1f, 1f, 1f);
            Instance.windowRect = GUILayout.Window(windowId, Instance.windowRect, Instance.DrawWindow, "Rate Monitor");
            Instance.HandleResize(ref Instance.windowRect);
            GUI.backgroundColor = originalColor;
        }

        public static void RefreshTitle()
        {
            if (Instance == null) return;

            Instance.titleText = SP.rateUnitText + ModSettings.RateUnit.Value + "/min  ";
            if (CalDB.ForceInc) Instance.titleText += SP.forceText;
            Instance.titleText += SP.incLevelText + CalDB.IncLevel + "  ";
            Instance.titleText += SP.entityCountText + Instance.Table.GetEntityCount();
        }

        private void Init(StatTable statTable)
        {
            Table = statTable;
            ratePanel = new RatePanel(statTable);
            profilePanel = new ProfilePanel(statTable);
            settingPanel = new SettingPanel(statTable);
            operactionPanel = new OperactionPanel(statTable);
            SP.Init();
            RefreshTitle();
        }        

        private void DrawWindow(int windowID)
        {
            // Draw close button
            if (GUI.Button(new Rect(windowRect.width - 23, 3, 20, 20), "X"))
            {
                Plugin.SaveCurrentTable();
                Plugin.MainTable = null;
                return;
            }

            GUILayout.Space(2);
            DrawTitle();
            GUILayout.Space(2);

            if (settingPanel.IsActive)
            {
                settingPanel.DrawPanel();
            }
            else if (operactionPanel.IsActive)
            {
                operactionPanel.DrawPanel();
            }
            else
            {
                GUILayout.BeginHorizontal();
                ratePanel.DrawPanel(RatePanelWidth);
                profilePanel.DrawPanel();
                GUILayout.EndHorizontal();
            }

            // Make the window draggable            
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
        }

        private void DrawTitle()
        {
            // 標題顯示當前時間單位(/min)和選中的建築數量
            GUILayout.BeginHorizontal();

            GUILayout.Label(titleText);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(operactionPanel.IsActive ? SP.backButtonText : SP.operationButtonText, GUILayout.Width(Utils.ShortButtonWidth)))
            {
                operactionPanel.IsActive = !operactionPanel.IsActive;
                if (operactionPanel.IsActive) settingPanel.IsActive = false;
            }
            if (GUILayout.Button(settingPanel.IsActive ? SP.backButtonText : SP.settingButtonText, GUILayout.Width(Utils.ShortButtonWidth)))
            {
                settingPanel.IsActive = !settingPanel.IsActive;
                if (settingPanel.IsActive)
                {
                    settingPanel.RefreshInputs();
                    operactionPanel.IsActive = false;
                }
            }            
            GUILayout.EndHorizontal();
        }

        private void HandleResize(ref Rect windowRect)
        {
            var resizeHandleRect = new Rect(windowRect.xMax - 10, windowRect.yMax - 10, 25, 25);

            if (resizeHandleRect.Contains(Event.current.mousePosition) && !windowRect.Contains(Event.current.mousePosition))
            {
                GUI.Box(resizeHandleRect, "↘"); // Draw a resize handle in the bottom-right corner for 20x20 pixel
                if (Event.current.type == EventType.MouseDown)
                {
                    isResizing = true;
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                isResizing = false;
            }

            if (isResizing)
            {
                // Calculate new window size based on mouse position, keeping the minimum window size as 30x30
                windowRect.xMax = Math.Max(Event.current.mousePosition.x, windowRect.xMin + 30);
                windowRect.yMax = Math.Max(Event.current.mousePosition.y, windowRect.yMin + 30);
                ProfilePanelWidth = windowRect.width - RatePanelWidth - 40;
            }

            // EatInputInRect
            if (!(Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))) //Eat only when left-click
                return;
            if (windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
        }
    }
}
