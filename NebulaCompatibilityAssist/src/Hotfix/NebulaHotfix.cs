using HarmonyLib;
using NebulaModel.Networking;
using NebulaModel.Packets.Logistics;
using NebulaModel.Utils;
using NebulaNetwork;
using NebulaWorld;
using NebulaWorld.Combat;
using NebulaWorld.Logistics;
using NebulaWorld.Player;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace NebulaCompatibilityAssist.Hotfix
{
    public static class NebulaHotfix
    {
        //private const string NAME = "NebulaMultiplayerMod";
        private const string GUID = "dsp.nebula-multiplayer";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                System.Version nebulaVersion = pluginInfo.Metadata.Version;
                
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 4)
                {
                    harmony.PatchAll(typeof(Warper094));
                    Log.Info("Nebula hotfix 0.9.4 - OK");
                }

                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(Analysis.StacktraceParser));
                harmony.PatchAll(typeof(SuppressErrors));
                Log.Info("Nebula extra features - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        /*
        private static void PatchPacketProcessor(Harmony harmony)
        {
            Type classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
            harmony.Patch(AccessTools.Method(classType, "JoinGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
        }
        */
    }

    public static class SuppressErrors
    {
        static bool suppressed = false;

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnGameBegin()
        {
            suppressed = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.GameTickLogic))]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.KeyTickLogic))]
        [HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.GameTickLogic))]
        [HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.KeyTickLogic))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.GameTick))]
        public static Exception EnemyGameTick_Finalizer(Exception __exception)
        {
            if (__exception != null && !suppressed)
            {
                suppressed = true;
                var msg = "NebulaCompatibilityAssist suppressed the following exception: \n" + __exception.ToString();
                ChatManager.ShowWarningInChat(msg);
                Log.Error(msg);
            }
            return null;
        }
    }

    public static class Warper094
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaModel.Logger.Log), nameof(NebulaModel.Logger.Log.Error), new Type[] { typeof(string) })]
        static bool LogError_Prefix(string message)
        {
            NebulaModel.Logger.Log.logger.LogError(message);
            NebulaModel.Logger.Log.LastErrorMsg = message;
            if (UIFatalErrorTip.instance != null)
            {
                // Test if current code is executing on the main unity thread
                if (BepInEx.ThreadingHelper.Instance.InvokeRequired)
                {
                    // ShowError has Unity API and needs to call on the main thread
                    BepInEx.ThreadingHelper.Instance.StartSyncInvoke(() =>
                        UIFatalErrorTip.instance.ShowError("[Nebula Error] " + message, "")
                    );
                    return false;
                }
                UIFatalErrorTip.instance.ShowError("[Nebula Error] " + message, "");
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NebulaNetwork.Ngrok.NgrokManager), nameof(NebulaNetwork.Ngrok.NgrokManager.IsNgrokActive))]
        static bool IsNgrokActive(NebulaNetwork.Ngrok.NgrokManager __instance, ref bool __result)
        {
            if (__instance._ngrokProcess == null)
            {
                __result = false;
                return false;
            }
            try
            {
                __instance._ngrokProcess.Refresh();
                __result = !__instance._ngrokProcess.HasExited;
            }
            catch
            {
                __result = false;
            }
            return false;
        }
    }
}
