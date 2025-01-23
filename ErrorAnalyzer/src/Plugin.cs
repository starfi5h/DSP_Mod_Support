using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.Reflection;
using System;
using UnityEngine;

[assembly: AssemblyTitle(ErrorAnalyzer.Plugin.NAME)]
[assembly: AssemblyVersion(ErrorAnalyzer.Plugin.VERSION)]

namespace ErrorAnalyzer
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "aaa.dsp.plugin.ErrorAnalyzer"; // Change guid to make it load first
        public const string NAME = "ErrorAnalyzer";
        public const string VERSION = "1.2.4";

        public static ManualLogSource Log;
        public static bool isRegisitered;
        public static string errorString;
        public static string errorStackTrace;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);
            var enableDebug = Config.Bind("DEBUG Mode", "Enable", false, "Enable DEBUG mode to track the entity when starting up the game").Value;
            var showFullstack = Config.Bind("Message", "Show All Patches", false, "Show all mod patches on the stacktrace (By default it will not list GameData.Gametick() and below methods)").Value;
            var dumpPatchMap = Config.Bind("Message", "Dump All Patches", false, "Dump Harmony patches of all mods when the game load in BepInEx\\LogOutput.log").Value;

            if (Chainloader.PluginInfos.ContainsKey("dsp.nebula-multiplayer"))
            {
                Log.LogInfo("Skip patching UIFatalErrorTip_Patch for Nebula is enabled");
            }
            else
            {
                try
                {
                    harmony.PatchAll(typeof(UIFatalErrorTip_Patch));
                    Application.logMessageReceived += HandleLog;
                    isRegisitered = true;
                }
                catch (Exception e)
                {
                    Log.LogError("Error when patching UIFatalErrorTip_Patch");
                    Log.LogError(e);
                }
            }

            {
                try
                {
                    harmony.PatchAll(typeof(StacktraceParser));
                }
                catch (Exception e)
                {
                    Log.LogError("Error when patching StacktraceParser");
                    Log.LogError(e);
                }
            }

            if (enableDebug)
            {
                TrackEntity_Patch.Enable(true);
            }
            if (dumpPatchMap)
            {
                harmony.PatchAll(typeof(Plugin));
            }
        }

        static bool init = false;
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        static void OnBegin()
        {
            if (init) return;
            init = true;
            StacktraceParser.GeneratePatchMap();
            StacktraceParser.DumpPatchMap();
        }

#if DEBUG
        public void OnDestroy()
        {
            UIFatalErrorTip_Patch.OnCloseClick(0);
            TrackEntity_Patch.Enable(false);
            harmony.UnpatchSelf();
            harmony = null;
        }
#endif

        public static void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(errorString))
            {
                if (logString.IndexOf("Exception") > 0)
                {
                    errorString = logString;
                    errorStackTrace = stackTrace;
                    Log.LogDebug("Exception Record");
                }
            }
        }
    }
}
