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
        public const string VERSION = "1.0.1";

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
#if !DEBUG
            CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
            {
                key = new CombineKey((int)KeyCode.F, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
                conflictGroup = 2052,
                name = "ShowFactoryLocator",
                canOverride = true
            });
            ProtoRegistry.RegisterString("KEYShowFactoryLocator", "Show Factory Locator Window", "打开FactoryLocator窗口");
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
