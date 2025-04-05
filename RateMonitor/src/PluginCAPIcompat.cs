#if !DEBUG
using BepInEx;
using CommonAPI;
using CommonAPI.Systems;
using UnityEngine;
using CommonAPI.Systems.ModLocalization;

namespace RateMonitor
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(LocalizationModule), nameof(CustomKeyBindSystem))]
    public class PluginCAPIcompat : BaseUnityPlugin
    {
        // This compatible plugin only load when CommonAPI is present
        public const string GUID = "starfi5h.plugin.RateMonitor.CAPIcompat";
        public const string NAME = "RateMonitorCAPIcompat";
        public const string VERSION = "1.0.0";
        public static bool IsRegisiter = false;

        public void Awake()
        {
            CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
            {
                key = new CombineKey((int)KeyCode.X, CombineKey.ALT_COMB, ECombineKeyAction.OnceClick, false),
                conflictGroup = 2048,
                name = "RateMonitorSelectionToolKey",
                canOverride = true
            });
            LocalizationModule.RegisterTranslation("RateMonitorSelection", "(Mod) RateMonitor Selection", "(Mod) RateMonitor框选计算", "(Mod) Rate Monitor Selection");
            IsRegisiter = true;
        }

        public static bool IsPressed()
        {
            return CustomKeyBindSystem.GetKeyBind("RateMonitorSelectionToolKey").keyValue;
        }
    }
}            
#endif