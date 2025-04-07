using System;
using UnityEngine;

namespace RateMonitor.UI
{
    public class UIWindow
    {
        public static UIWindow Instance { get; private set; }
        public static bool InResizingArea { get; private set; }
        public StatTable Table { get; private set; }
        public float RatePanelWidth { get; private set; } = 190f;
        public float ProfilePanelWidth { get; private set; } = 350f;


        private const int windowId = 432145525;
        private Rect windowRect = new (100, 150, 580, 400);
        private string windowName = "Rate Monitor";
        private bool isResizingWindow;
        private bool isResizingPanel;

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
            Instance.windowRect = GUILayout.Window(windowId, Instance.windowRect, Instance.DrawWindow, Instance.windowName);

            InResizingArea = false;
            if(Instance.ratePanel.IsActive) Instance.HandlePanelResize(ref Instance.windowRect);
            Instance.HandleWindowResize(ref Instance.windowRect);
            GUI.backgroundColor = originalColor;
        }

        public static void RefreshTitle()
        {
            if (Instance == null) return;
            if (!SP.IsInit) SP.Init();
            
            Instance.titleText = SP.rateUnitText + ModSettings.RateUnit.Value + "/min  ";
            if (CalDB.ForceInc) Instance.titleText += SP.forceText;
            Instance.titleText += SP.incLevelText + CalDB.IncLevel + "  ";
            if (Instance.Table != null) Instance.windowName = "Rate Monitor (" + Instance.Table.GetEntityCount() + ")";
        }

        public static void LoadUIWindowConfig()
        {
            if (Instance == null) Instance = new UIWindow();
            Instance.windowRect.width = ModSettings.WindowWidth.Value;
            Instance.windowRect.height = ModSettings.WindowHeight.Value;
            Instance.RatePanelWidth = ModSettings.WindowRatePanelWidth.Value;
        }

        public static void SaveUIWindowConfig()
        {
            if (Instance == null) return;
            ModSettings.WindowWidth.Value = Instance.windowRect.width;
            ModSettings.WindowHeight.Value = Instance.windowRect.height;
            ModSettings.WindowRatePanelWidth.Value = Instance.RatePanelWidth;
        }

        private void Init(StatTable statTable)
        {
            Table = statTable;
            ratePanel = new RatePanel(statTable);
            profilePanel = new ProfilePanel(statTable);
            settingPanel = new SettingPanel(statTable);
            operactionPanel = new OperactionPanel(statTable);
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
            ratePanel.IsActive = !operactionPanel.IsActive && !settingPanel.IsActive;
            GUILayout.EndHorizontal();
        }

        private void HandleWindowResize(ref Rect windowRect)
        {
            var resizeHandleRect = new Rect(windowRect.xMax - 10, windowRect.yMax - 10, 25, 25);

            if (resizeHandleRect.Contains(Event.current.mousePosition) && !windowRect.Contains(Event.current.mousePosition))
            {
                InResizingArea = true;
                GUI.Box(resizeHandleRect, "↘"); // Draw a resize handle in the bottom-right corner for 20x20 pixel
                if (Event.current.type == EventType.MouseDown)
                {
                    isResizingWindow = true;
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                isResizingWindow = false;
            }

            if (isResizingWindow)
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

        private void HandlePanelResize(ref Rect windowRect)
        {
            var resizeHandleRect = new Rect(windowRect.xMin + RatePanelWidth + 3 + 2, windowRect.yMin, 20 -2, windowRect.height);

            if (resizeHandleRect.Contains(Event.current.mousePosition))
            {
                InResizingArea = true;
                GUI.Box(resizeHandleRect, ""); // Draw a resize handler
                if (Event.current.type == EventType.MouseDown)
                {
                    isResizingPanel = true;
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                isResizingPanel = false;
            }

            if (isResizingPanel)
            {
                // Calculate new RatePanelWidth based on mouse position x, keeping the minimum panel size
                RatePanelWidth = Event.current.mousePosition.x - windowRect.xMin - 12f;
                RatePanelWidth = (float)Maths.Clamp(RatePanelWidth, 10f, windowRect.width - 60f);
            }
        }
    }
}
