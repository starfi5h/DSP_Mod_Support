using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(ErrorAnalyzer.Plugin.NAME)]
[assembly: AssemblyVersion(ErrorAnalyzer.Plugin.VERSION)]

namespace ErrorAnalyzer
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.ErrorAnalyzer";
        public const string NAME = "ErrorAnalyzer";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);
            if (!Chainloader.PluginInfos.TryGetValue("dsp.nebula-multiplayer", out var _))
            {
                harmony.PatchAll(typeof(UIFatalErrorTip_Patch));
            }
            if (!Chainloader.PluginInfos.TryGetValue("NebulaCompatibilityAssist", out var _))
            {
                harmony.PatchAll(typeof(StacktraceParser));
            }
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }
    }
}
