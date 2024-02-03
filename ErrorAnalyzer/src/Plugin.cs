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
        public const string VERSION = "1.1.0";

        public static ManualLogSource Log;
        public static bool isRegisitered;
        public static string errorString;
        public static string errorStackTrace;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);
            if (!Chainloader.PluginInfos.TryGetValue("dsp.nebula-multiplayer", out var _))
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
            if (!Chainloader.PluginInfos.TryGetValue("NebulaCompatibilityAssist", out var _))
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
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }

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
