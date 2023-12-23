using HarmonyLib;

namespace SF_ChinesePatch
{
    public class LSTM_Patch
    {
        public const string NAME = "LSTM";
        public const string GUID = "com.hetima.dsp.LSTM";

        public static void OnAwake(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;
            if (!Plugin.Instance.Config.Bind("Enable", NAME, true).Value) return;
            RegisterStrings();

            try
            {
                harmony.Patch(AccessTools.Method(AccessTools.TypeByName("LSTMMod.UIConfigWindow"), "CreateUI"),
                    null, null, new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Plugin.TranslateStrings))));
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} error!\n" + e);
            }
        }

        private static void RegisterStrings()
        {
            StringManager.RegisterString("Select Item", "选择物品");
            StringManager.RegisterString("Select item to display", "选择要显示的物品");
            StringManager.RegisterString("Click here to clear navi", "清除导航");
            StringManager.RegisterString("Upgrades Required", "需要升级");
            StringManager.RegisterString("Click: select item\nRight-click: select and update this list", "点击：选择物品\n右键点击：选择并更新列表");
            StringManager.RegisterString("Material Selector", "原料选择器");
            StringManager.RegisterString("ILS Stock Ratio", "星際物流塔库存比例");
            StringManager.RegisterString("Click to show / hide ILS stock ratio\nby LSTM", "点击显示/隐藏LSTM的星際物流塔库存比例");
            StringManager.RegisterString("Locate Station", "定位站点");
            StringManager.RegisterString("Show the star to which the station belongs or navigation to this station", "显示该站点所属的星球或导航至该站点");
            StringManager.RegisterString("Item Filter", "物品筛选");
            StringManager.RegisterString("Filter list with this item", "使用该物品筛选列表");
            StringManager.RegisterString("(All Planets)", "(所有星球)");
            StringManager.RegisterString("Supply", "供应");
            StringManager.RegisterString("Demand", "需求");
            StringManager.RegisterString("Log", "日志");
            StringManager.RegisterString("Open Log with current state", "使用当前状态打开日志");

            #region UIConfigWindow
            StringManager.RegisterString("General", "常规");
            StringManager.RegisterString("Main Hotkey", "快捷键");
            StringManager.RegisterString("Show Material Picker", "显示原料选择器");
            StringManager.RegisterString("Indicates Warper Sign", "以*标记有翘曲器的站点");
            StringManager.RegisterString("Close Panel With E Key", "使用E键关闭面板");
            StringManager.RegisterString("Act As Standard Panel", "单击Esc关闭面板");
            StringManager.RegisterString("Suppress Open Inventory Window", "阻止打开背包窗口");
            StringManager.RegisterString("Show Station Info Icon", "显示站点信息图标");
            StringManager.RegisterString("Only In Planet View", "仅在行星视图中");
            StringManager.RegisterString("Show Stat On Statistics Window", "在统计窗口中显示统计信息");
            StringManager.RegisterString("Show Open LSTM Button On", "在以下介面增加LSTM按钮");
            StringManager.RegisterString("Station Window", "物流站窗口");
            StringManager.RegisterString(" Statistics Window", "统计窗口");
            StringManager.RegisterString("Starmap Detail Panel", "星球详细信息面板");
            StringManager.RegisterString("Set Construction Point To Ground", "将建设点设置到地面");  
            StringManager.RegisterString("Double-Click To Navi Everywhere", "双击地面以产生导航线");
            StringManager.RegisterString("On Planet View", "在行星视图中");
            StringManager.RegisterString("Enable Traffic Log (needs restart game)", "启用运输日志（需重启游戏）");
            StringManager.RegisterString("Hide Storaged Slot", "隐藏仓储槽位");

            StringManager.RegisterString("Traffic Logic", "运输逻辑");
            StringManager.RegisterString("One-time demand", "一次性需求");
            StringManager.RegisterString("Ignore Supply Range", "忽略最大距离限制");
            StringManager.RegisterString("Smart Transport", "智能运输");
            StringManager.RegisterString("Consider Opposite Range", "考虑对向站点的距离限制");
            StringManager.RegisterString("Remote Demand Delay (96%)", "远程需求延迟 (96%)");
            StringManager.RegisterString("Local Demand Delay (98%)", "本地需求延迟 (98%)");
            StringManager.RegisterString("Remote Cluster [C:]", "远程群集 [C:]");
            StringManager.RegisterString("Local Cluster [c:]", "本地群集 [c:]");
            StringManager.RegisterString("Remote Distance/Capacity Balance *", "远程距离/容量平衡 *");
            StringManager.RegisterString("Supply 70%-100% Multiplier", "当供应 70%-100% 时, 距离增长倍率");
            StringManager.RegisterString("Demand 0%-30% Multiplier", "当需求 0%-30% 时, 距离增长倍率");
            StringManager.RegisterString("Supply 0%-30% Denominator", "当供应 0%-30% 时, 距离缩减倍率");
            StringManager.RegisterString("* Distance/Capacity Balance will be forced off when Smart Transport is on", "* 当启用智能运输时将强制关闭距离/容量平衡");
            #endregion
        }
    }
}
