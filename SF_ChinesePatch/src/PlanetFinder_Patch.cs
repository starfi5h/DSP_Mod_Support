using HarmonyLib;

namespace SF_ChinesePatch
{
    public class PlanetFinder_Patch
    {
        public const string NAME = "PlanetFinder";
        public const string GUID = "com.hetima.dsp.PlanetFinder";

        public static void OnAwake(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID)) return;
            if (!Plugin.Instance.Config.Bind("Enable", NAME, true).Value) return;
            RegisterStrings();

            try
            {
                harmony.Patch(AccessTools.Method(AccessTools.TypeByName("PlanetFinderMod.PLFN"), "CreateUI"),
                    null, null, new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Plugin.TranslateStrings))));

                harmony.Patch(AccessTools.Method(AccessTools.TypeByName("PlanetFinderMod.UIConfigWindow"), "CreateUI"),
                    null, null, new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Plugin.TranslateStrings))));

                var target = AccessTools.TypeByName("PlanetFinderMod.UIPlanetFinderWindow");
                harmony.Patch(AccessTools.Method(target, "_OnCreate"),
                    null, null, new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Plugin.TranslateStrings))));
                //harmony.Patch(AccessTools.Method(target, "SetUpItemList"),
                //    null, null, new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Plugin.TranslateStrings))));

                harmony.Patch(AccessTools.Method(AccessTools.TypeByName("PlanetFinderMod.UIPlanetFinderListItem"), "CreateListViewPrefab"),
                    null, null, new HarmonyMethod(typeof(Plugin).GetMethod(nameof(Plugin.TranslateStrings))));

            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} error!\n" + e);
            }
        }

        private static void RegisterStrings()
        {
            // PLFN.CreateUI
            StringManager.RegisterString("open/close Planet Finder Window", "打开/关闭行星搜索器Planet Finder");

            // UIPlanetFinderWindow._OnCreate
            StringManager.RegisterString("Sys", "星系");
            StringManager.RegisterString("All Planet", "行星");
            StringManager.RegisterString("Current Star", "当前星系");
            StringManager.RegisterString("Has Factory", "存在工厂");
            StringManager.RegisterString("★", "★"); // 收藏
            StringManager.RegisterString("Recent", "最近造访");

            // UIPlanetFinderListItem.CreateListViewPrefab
            StringManager.RegisterString("Locate Planet", "定位行星"); 
            StringManager.RegisterString("Show the planet on the starmap", "在星图上查看行星");

            #region UIConfigWindow
            StringManager.RegisterString("Main Hotkey", "快捷键");
            StringManager.RegisterString("Show Button In Main Panel", "在主面板中显示模组按钮");
            StringManager.RegisterString("Show Button In Starmap", "在星图中显示按钮");
            StringManager.RegisterString("Window Size", "窗口尺寸");
            StringManager.RegisterString("Show Power State In List", "列表中显示电力状态");
            StringManager.RegisterString("Show [GAS] [TL] Prefix", "显示巨行星[GAS]和潮汐锁定[TL]标志");
            StringManager.RegisterString("Show Fav Button In Starmap", "星图中显示收藏标志★");
            StringManager.RegisterString("Integration With DSPStarMapMemo", "兼容DSPStarMapMemo");
            StringManager.RegisterString("Display Icons / Search Memo", "显示图标/搜索备注");
            StringManager.RegisterString("Integration With LSTM", "兼容LSTM");
            StringManager.RegisterString("Open LSTM From Context Panel", "从关联按钮打开LSTM");
            StringManager.RegisterString("Integration With CruiseAssist", "兼容CruiseAssist");
            StringManager.RegisterString("Set CruiseAssist From Context Panel", "从关联按钮设置CruiseAssist");
            #endregion
        }
    }
}
