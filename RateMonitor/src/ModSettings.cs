using BepInEx.Configuration;
using UnityEngine;

namespace RateMonitor
{
    public static class ModSettings
    {
        public static ConfigEntry<KeyboardShortcut> SelectionToolKey;
        public static ConfigEntry<int> RateUnit;
        public static ConfigEntry<bool> ShowRealtimeRate;
        public static ConfigEntry<bool> ShowWorkingRateInPercentage;
        public static ConfigEntry<float> WindowWidth;
        public static ConfigEntry<float> WindowHeight;
        public static ConfigEntry<float> WindowRatePanelWidth;
        public static ConfigEntry<int> IncLevel;
        public static ConfigEntry<bool> ForceInc;
        public static ConfigEntry<bool> ForceLens;

        public static void LoadConfigs(ConfigFile config)
        {
            SelectionToolKey = config.Bind("KeyBinds", "SelectToolKey", new KeyboardShortcut(KeyCode.X, KeyCode.LeftAlt),
                "Hotkey to toggle area selection tool\n启用框选工具的热键");

            RateUnit = config.Bind("UI", "Rate Unit", 1,
                new ConfigDescription("Timescale unit (x item per minute)\n速率单位(每分钟x个物品)", new AcceptableValueRange<int>(1, 14400)));

            WindowWidth = config.Bind("UI", "Window Width", 580f);
            WindowHeight = config.Bind("UI", "Window Height", 400f);
            WindowRatePanelWidth = config.Bind("UI", "Window Rate Panel Width", 190f);

            ShowRealtimeRate = config.Bind("UI", "Show Realtime Rate", true,
                "Show Real-time Monitoring Rate\n显示即时监控速率");

            ShowWorkingRateInPercentage = config.Bind("UI", "Show Working Rate in percentage", true,
                "Show Real-time Working Rate in percentage\n以百分比显示工作效率");

            IncLevel = config.Bind("General", "Proliferator Level", -1,
                new ConfigDescription("Level of proliferator [1,2,4]. Auto dectect: -1\n增产效果等级[1,2,4] 自动侦测:-1", new AcceptableValueRange<int>(-1, 10)));

            ForceInc = config.Bind("General", "Force Proliferator", false,
                "The theoretical max rate always apply proliferator, regardless the material.\n计算理论上限时是否强制套用增产设定(否=依照当下原料决定)");

            ForceLens = config.Bind("General", "Force Gravity Lens in Ray Receiver", false,
                "The theoretical max rate always apply gravity lens.\n计算射线接收站时总是套用透镜(否=依照当下决定)");
        }
    }
}
