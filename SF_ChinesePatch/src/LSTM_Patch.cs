namespace SF_ChinesePatch
{
    public class LSTM_Patch
    {
        public const string NAME = "LSTM";
        public const string GUID = "com.hetima.dsp.LSTM";

        public static void OnAwake()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;
            if (!Plugin.Instance.Config.Bind("Enable", NAME, true).Value) return;
            RegisterStrings();
        }

        private static void RegisterStrings()
        {
            StringManager.RegisterString("Select Item", "选择物品");
            StringManager.RegisterString("Select item to display", "选择要显示的物品");
            StringManager.RegisterString("Click here to clear navi", "清除导航");
            StringManager.RegisterString("Upgrades Required", "需要升级");
            StringManager.RegisterString("Click: select item\nRight-click: select and update this list", "点击：选择物品\n右键点击：选择并更新列表");
            StringManager.RegisterString("Material Selector", "材料选择器");
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
        }
    }
}
