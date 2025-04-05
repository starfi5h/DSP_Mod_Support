using BepInEx.Configuration;
using UnityEngine;

namespace RateMonitor
{
    public static class ModSettings
    {
        public static ConfigEntry<KeyboardShortcut> SelectionToolKey;
        public static ConfigEntry<int> RateUnit;
        public static ConfigEntry<bool> ShowRealtimeRate;
        public static ConfigEntry<int> IncLevel;
        public static ConfigEntry<bool> ForceInc;

        public static void LoadConfigs(ConfigFile config)
        {
            SelectionToolKey = config.Bind("KeyBinds", "SelectToolKey", new KeyboardShortcut(KeyCode.X, KeyCode.LeftAlt),
                "Hotkey to toggle area selection tool\n启用框选工具的热键");

            RateUnit = config.Bind("UI", "Rate Unit", 1,
                new ConfigDescription("Timescale unit (x item per minute)\n速率单位(每分钟x个物品)", new AcceptableValueRange<int>(1, 14400)));

            ShowRealtimeRate = config.Bind("UI", "Show Realtime Rate", true,
                "Show Real-time Monitoring Rate\n显示即时监控速率");

            IncLevel = config.Bind("General", "Proliferator Level", -1,
                new ConfigDescription("Level of proliferator [1,2,4]. Auto dectect: -1\n增产效果等级[1,2,4] 自动侦测:-1", new AcceptableValueRange<int>(-1, 10)));

            ForceInc = config.Bind("General", "Force Proliferator", false,
                "The theoretical max rate always apply proliferator, regardless the material.\n计算理论上限时是否强制套用增产设定(否=依照当下原料决定)");
        }
    }
}
