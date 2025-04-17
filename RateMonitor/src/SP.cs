namespace RateMonitor
{
    public static class SP // Strings Pool
    {
        public static bool IsInit { get; private set; }

        // Setting Panel
        public static string uiSettingsText;
        public static string showRealTimeRateText;
        public static string showInPercentageText;
        public static string rateUnitText;
        public static string perMinuteText;
        public static string perSecondText;
        public static string[] perBeltTexts = new string[3];
        public static string fontSizeText;

        public static string calculateSettingsText;
        public static string incLevelText;
        public static string forceIncText;
        public static string forceText;
        public static string forceLens;

        // UI Window
        public static string settingButtonText;
        public static string operationButtonText;
        public static string backButtonText;
        public static string entityCountText;
        public static string countMultiplierText;

        // Operaction Panel
        public static string addActionText;
        public static string subActionText;
        public static string planetSelectDescriptionText;
        public static string wholeLocalPlanetText;
        public static string wholeRemotePlanetText;
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
            showInPercentageText = isZHCN ? "以百分比显示" : "Show In Percentage".Translate();
            rateUnitText = isZHCN ? "速率单位: " : "RateUnit: ".Translate();
            perMinuteText = isZHCN ? "每分钟" : "Per Minute".Translate();
            perSecondText = isZHCN ? "每秒钟" : "Per Second".Translate();
            perBeltTexts[0] = isZHCN ? "黄带" : "Mk1 Belt".Translate();
            perBeltTexts[1] = isZHCN ? "绿带" : "Mk2 Belt".Translate();
            perBeltTexts[2] = isZHCN ? "蓝带" : "Mk3 Belt".Translate();
            fontSizeText = isZHCN ? "字体大小" : "Font Size".Translate();

            calculateSettingsText = isZHCN ? "計算设定" : "Calculate Settings".Translate();
            incLevelText = isZHCN ? "增产等级: " : "ProliferatorLevel: ".Translate();
            forceIncText = isZHCN ? "强制增产" : "ForceProliferator".Translate();
            forceLens = isZHCN ? "强制透镜" : "ForceGravitonLens".Translate();
            forceText = isZHCN ? "强制" : "Force".Translate();

            // UI Window
            settingButtonText = isZHCN ? "设置" : "Config".Translate();
            operationButtonText = isZHCN ? "操作" : "Operate".Translate();
            backButtonText = isZHCN ? "返回" : "Back".Translate();
            entityCountText = isZHCN ? "建筑数量: " : "EntityCount: ".Translate();
            countMultiplierText = isZHCN ? "建筑数量倍数" : "Count Multiplier".Translate();

            // Operaction Panel
            addActionText = isZHCN ? "添增选取" : "Add Selection".Translate();
            subActionText = isZHCN ? "移除选取" : "Sub Selection".Translate();
            planetSelectDescriptionText = isZHCN ? "可以在统计面板或总控面板选择星球来框选全建筑" :
                "Select a planet in the statistics panel or control panel to select all buildings on it".Translate();

            wholeLocalPlanetText = isZHCN ? "选取本地全球机器" : "Select Whole Local Planet".Translate();
            wholeRemotePlanetText = isZHCN ? "选取远端全球机器" : "Select Whole Remote Planet".Translate();
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
            expandRecordText = isZHCN ? "检视" : "Detail".Translate();
            recordTooltipText = isZHCN ? "导航至机器" : "Navigate to machine".Translate();

            EntityRecord.InitStrings(isZHCN);

            IsInit = true;
        }
    }
}
