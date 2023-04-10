using BepInEx;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;
using FactoryLocator.UI;
using FactoryLocator.Compat;

namespace FactoryLocator
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem))]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.FactoryLocator";
        public const string NAME = "FactoryLocator";
        public const string VERSION = "1.1.0";

        public static UILocatorWindow mainWindow = null;
        public static MainLogic mainLogic = null;
        public static Harmony harmony;

        public void Awake()
        {
            Log.LogSource = Logger;
            harmony = new(GUID);
            harmony.PatchAll(typeof(WarningSystemPatch));
            harmony.PatchAll(typeof(UIentryCount));

#if DEBUG
            Init();
#else
            harmony.PatchAll(typeof(Plugin));
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnCreate))]
        internal static void Init()
        {
            Log.Debug("Initing...");
            mainLogic = new MainLogic();
            mainWindow = UILocatorWindow.CreateWindow();
            NebulaCompat.Init();
            DSPMoreRecipesCompat.Init();
            GenesisBookCompat.Init();
#if !DEBUG
            CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
            {
                key = new CombineKey((int)KeyCode.F, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
                conflictGroup = 2052,
                name = "ShowFactoryLocator",
                canOverride = true
            });
            ProtoRegistry.RegisterString("KEYShowFactoryLocator", "Show Factory Locator Window", "打开FactoryLocator窗口");
            ProtoRegistry.RegisterString("Building", "Building", "建筑");
            ProtoRegistry.RegisterString("Vein", "Vein", "矿脉");
            ProtoRegistry.RegisterString("Recipe", "Recipe", "配方");
            ProtoRegistry.RegisterString("Warning", "Warning", "警报");
            ProtoRegistry.RegisterString("Storage", "Storage", "储物仓");
            ProtoRegistry.RegisterString("Station", "Station", "物流塔");
            ProtoRegistry.RegisterString("Signal Icon", "Signal Icon", "信号图标");
            ProtoRegistry.RegisterString("Clear All", "Clear All", "清空");
            ProtoRegistry.RegisterString("Display All Warning", "Display All Warning", "显示所有警报提示");
            ProtoRegistry.RegisterString("Auto Clear Query", "Auto Clear Query", "自动清除搜寻结果");
            ProtoRegistry.RegisterString("Power Network Status", "Power Network Status", "电网状态");
            ProtoRegistry.RegisterString("Satisfaction - Consumer Count", "Satisfaction - Consumer Count", "供电率 - 消耗者数量");
#endif
        }

        public void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (!mainWindow.active)
                    mainWindow.OpenWindow();
                else
                    mainWindow._Close();
                return;
            }
#else
            if (CustomKeyBindSystem.GetKeyBind("ShowFactoryLocator").keyValue) 
            {
                if (!mainWindow.active)
                    mainWindow.OpenWindow();
                else
                    mainWindow._Close();
                return;
            }
#endif
            if (mainWindow != null && mainWindow.active)
            {
                mainWindow._OnUpdate();
                if (VFInput.escape)
                {
                    VFInput.UseEscape();
                    mainWindow._Close();
                }
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            if (mainWindow != null)
            {
                Destroy(mainWindow.gameObject);
                mainWindow = null;
            }
            UIentryCount.OnDestory();
        }
    }

    public static class Log
    {
        public static ManualLogSource LogSource;
        public static void Error(object obj) => LogSource.LogError(obj);
        public static void Warn(object obj) => LogSource.LogWarning(obj);
        public static void Info(object obj) => LogSource.LogInfo(obj);
        public static void Debug(object obj) => LogSource.LogDebug(obj);
    }
}
