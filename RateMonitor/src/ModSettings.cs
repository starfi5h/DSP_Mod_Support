using BepInEx.Configuration;
using UnityEngine;

namespace RateMonitor
{
    public static class ModSettings
    {
        public static ConfigEntry<KeyboardShortcut> SelectionToolKey;
        // Display
        public static ConfigEntry<int> RateUnit;
        public static ConfigEntry<int> FontSize;
        public static ConfigEntry<bool> ShowRealtimeRate;
        public static ConfigEntry<bool> ShowWorkingRateInPercentage;
        // Genral
        public static ConfigEntry<int> IncLevel;
        public static ConfigEntry<bool> ForceInc;
        public static ConfigEntry<bool> ForceLens;
        // UI
        public static ConfigEntry<bool> EnableQuickBarButton;
        public static ConfigEntry<bool> EnableSingleBuildingClick;        
        public static ConfigEntry<float> WindowWidth;
        public static ConfigEntry<float> WindowHeight;
        public static ConfigEntry<float> WindowRatePanelWidth;

        public static void LoadConfigs(ConfigFile config)
        {
            SelectionToolKey = config.Bind("KeyBinds", "SelectToolKey", new KeyboardShortcut(KeyCode.X, KeyCode.LeftAlt),
                "Hotkey to toggle area selection tool\n启用框选工具的热键");

            RateUnit = config.Bind("Display", "Rate Unit", 1,
                new ConfigDescription("Timescale unit (x item per minute)\n速率单位(每分钟x个物品)", new AcceptableValueRange<int>(1, 14400)));

            FontSize = config.Bind("Display", "Font Size", 14);

            ShowRealtimeRate = config.Bind("Display", "Show Realtime Rate", true,
                "Show Real-time Monitoring Rate\n显示即时监控速率");

            ShowWorkingRateInPercentage = config.Bind("Display", "Show Working Rate in percentage", true,
                "Show Real-time Working Rate in percentage\n以百分比显示工作效率");

            IncLevel = config.Bind("General", "Proliferator Level", -1,
                new ConfigDescription("Level of proliferator [1,2,4]. Auto dectect: -1\n增产效果等级[1,2,4] 自动侦测:-1", new AcceptableValueRange<int>(-1, 10)));

            ForceInc = config.Bind("General", "Force Proliferator", false,
                "The theoretical max rate always apply proliferator, regardless the material.\n计算理论上限时是否强制套用增产设定(false=依照当下原料决定)");

            ForceLens = config.Bind("General", "Force Gravity Lens in Ray Receiver", false,
                "The theoretical max rate always apply gravity lens.\n计算射线接收站时总是套用透镜(false=依照当下决定)");

            EnableQuickBarButton = config.Bind("UI", "Enable Quick Bar Button", true,
                "Create a button in mecha energy bar\n在机甲能量条创建一个mod开关按钮");
            EnableQuickBarButton.SettingChanged += (_,_) => QuickBarButton.Enable(EnableQuickBarButton.Value);

            EnableSingleBuildingClick = config.Bind("UI", "Enable Single Building Click", true,
                "When mod window is open, enable ctrl/shift + RMB to add/remove building\n在窗口打开时，Ctrl/Shift+右键可以添加/移除建筑");

            WindowWidth = config.Bind("UI", "Window Width", 580f);
            WindowHeight = config.Bind("UI", "Window Height", 400f);
            WindowRatePanelWidth = config.Bind("UI", "Window Rate Panel Width", 190f);
        }
    }
}
