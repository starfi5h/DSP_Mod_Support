namespace RateMonitor
{
    public static class SP // Strings Pool
    {
        // Setting Panel
        public static string uiSettingsText;
        public static string showRealTimeRateText;
        public static string rateUnitText;
        public static string perMinuteText;
        public static string perSecondText;
        public static string[] perBeltTexts = new string[3];

        public static string calculateSettingsText;
        public static string incLevelText;
        public static string forceIncText;
        public static string forceText;

        // UI Window
        public static string settingButtonText;
        public static string operationButtonText;
        public static string backButtonText;
        public static string entityCountText;

        // Operaction Panel
        public static string addActionText;
        public static string subActionText;
        public static string wholePlanetText;
        public static string loadLastText;
        public static string resetTimerText;

        // Rate Panel
        public static string itemIdProduceText;
        public static string itemIdConsumeText;
        public static string itemIdIntermediateText;
        public static string workingOnlyText;

        // Profile Panel
        public static string expandedOnlyText;
        public static string proliferatorCostText;
        public static string powerCostText;
        public static string totalProductionText;
        public static string totalConsumptionText;

        // Profile Panel - Detail
        public static string netMachineText;
        public static string expandRecordText;
        public static string recordTooltipText;

        public static void Init()
        {
            bool isZHCN = Localization.isZHCN;

            // Setting Panel
            uiSettingsText = isZHCN ? "UI设定" : "UI Settings".Translate();
            showRealTimeRateText = isZHCN ? "显示即时监控速率" : "Show Real-time Monitoring Rate".Translate();
            rateUnitText = isZHCN ? "速率单位: " : "RateUnit: ".Translate();
            perMinuteText = isZHCN ? "每分钟" : "Per Minute".Translate();
            perSecondText = isZHCN ? "每秒钟" : "Per Second".Translate();
            perBeltTexts[0] = isZHCN ? "黄带" : "Mk1 Belt".Translate();
            perBeltTexts[1] = isZHCN ? "绿带" : "Mk2 Belt".Translate();
            perBeltTexts[2] = isZHCN ? "蓝带" : "Mk3 Belt".Translate();

            calculateSettingsText = isZHCN ? "計算设定" : "Calculate Settings".Translate();
            incLevelText = isZHCN ? "增产等级: " : "ProliferatorLevel: ".Translate();
            forceIncText = isZHCN ? "强制增产" : "ForceProliferator".Translate();
            forceText = isZHCN ? "强制" : "Force".Translate();

            // UI Window
            settingButtonText = isZHCN ? "设置" : "Config".Translate();
            operationButtonText = isZHCN ? "操作" : "Operate".Translate();
            backButtonText = isZHCN ? "返回" : "Back".Translate();
            entityCountText = isZHCN ? "建筑数量: " : "EntityCount: ".Translate();

            // Operaction Panel
            addActionText = isZHCN ? "添增选取" : "Add Selection".Translate();
            subActionText = isZHCN ? "移除选取" : "Sub Selection".Translate();
            wholePlanetText = isZHCN ? "选取本地全球机器" : "Select Whole Local Planet".Translate();
            loadLastText = isZHCN ? "载入上一个选取" : "Load Last Selection".Translate();
            resetTimerText = isZHCN ? "重置计时器" : "Reset Timer".Translate();

            // Rate Panel
            itemIdProduceText = isZHCN ? "产物" : "Product ".Translate();
            itemIdConsumeText = isZHCN ? "原料" : "Material".Translate();
            itemIdIntermediateText = isZHCN ? "中间产物" : "Intermediate".Translate();
            workingOnlyText = isZHCN ? "工作中" : "Working".Translate();

            // Profile Panel
            expandedOnlyText = isZHCN ? "已展开" : "Expanded".Translate();
            proliferatorCostText = isZHCN ? "增产剂需求: " : "Proliferator Cost: ".Translate();
            powerCostText = isZHCN ? "电力需求: " : "Power Cost: ".Translate();
            totalProductionText = isZHCN ? "总生产: " : "Total Produce:   ".Translate();
            totalConsumptionText = isZHCN ? "总消耗: " : "Total Consume: ".Translate();

            // Profile Panel - Detail
            netMachineText = isZHCN ? "净机器: " : "Net Machine: ".Translate();
            expandRecordText = isZHCN ? "检视详情" : "Detail".Translate();
            recordTooltipText = isZHCN ? "导航至机器" : "Navigate to the machine".Translate();

            EntityRecord.InitStrings(isZHCN);
        }
    }
}
