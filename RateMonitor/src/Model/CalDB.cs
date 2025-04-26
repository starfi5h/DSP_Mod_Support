using System;

namespace RateMonitor.Model
{
    /// <summary>
    /// 計算過程用到的相關環境變數
    /// </summary>
    public static class CalDB
    {
        public const float WORKING_THRESHOLD = 0.99999999f; // e-08

        public static int IncLevel { get; private set; } = 4; // 全域增產等級. [1,2,4]
        public static bool ForceInc { get; private set; } // 是否強制套用增產劑設定
        public static float IncToProliferatorRatio { get; private set; } // 增產點數與自噴塗增產劑物品數量的兌換比例
        public static int MaxBeltStack { get; private set; } = 4; // 帶子的最大堆疊，分餾計算專用

        public static readonly int[] BeltSpeeds = new int[3] { 360, 720, 1800 }; // 黃帶，綠帶，藍帶速率

        // 電力設施選項
        public static bool IncludeFuelGenerator { get; private set; } = true; // 包含燃料發電機
        public static bool ForceGammaCatalyst { get; private set; } // 是否要強制套用透鏡

        // UI選項
        public static int CountMultiplier { get; set; } // 機器個數倍率


        public static Action OnRefresh; // 委派，用於MOD兼容
        public static bool CompatGB { get; set; } = false;


        public static void Refresh()
        {
            GameHistoryData history = GameMain.history;
            if (history == null) return;
            ForceInc = ModSettings.ForceInc.Value;
            IncLevel = ModSettings.IncLevel.Value;
            ForceGammaCatalyst = ModSettings.ForceLens.Value;

            if (IncLevel < 0 || IncLevel > 10)
            {
                IncLevel = 0;
                ItemProto itemProto = LDB.items.Select(2313); //噴塗機id
                int[] incItemIds = itemProto?.prefabDesc.incItemId;
                if (incItemIds != null)
                {
                    for (int i = 0; i < incItemIds.Length; i++)
                    {
                        if (history.ItemUnlocked(incItemIds[i])) //檢查增產劑是否已解鎖
                        {
                            ItemProto incItem = LDB.items.Select(incItemIds[i]);
                            if (incItem != null && incItem.Ability > IncLevel)
                            {
                                IncLevel = incItem.Ability;
                            }
                        }
                    }
                }
                IncLevel = IncLevel <= 10 ? IncLevel : 10;
            }
            if (IncLevel == 0) IncToProliferatorRatio = 0f;
            else if (IncLevel == 1) IncToProliferatorRatio = 1 / 13f;
            else if (IncLevel == 2) IncToProliferatorRatio = 1 / 28f;
            else IncToProliferatorRatio = 1 / 75f;

            if (CompatGB) OnRefresh_GB();

            OnRefresh?.Invoke();
        }

        static void OnRefresh_GB()
        {
            BeltSpeeds[0] = 720;
            BeltSpeeds[1] = 1800;
            BeltSpeeds[2] = 3600;
        }
    }
}
