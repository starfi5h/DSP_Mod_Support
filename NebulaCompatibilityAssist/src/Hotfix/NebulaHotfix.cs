using HarmonyLib;
using NebulaModel.Networking;
using NebulaModel.Packets.GameStates;
using NebulaModel.Packets.Logistics;
using NebulaModel.Utils;
using NebulaNetwork;
using NebulaWorld;
using NebulaWorld.Combat;
using NebulaWorld.GameStates;
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
                
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 5)
                {
                    harmony.PatchAll(typeof(Warper095));
                    Log.Info("Nebula hotfix 0.9.5 - OK");
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

    public static class Warper095
    {
        // Change GameStatesManager.LastSaveTime to DateTimeOffset.UtcNow.ToUnixTimeSeconds (real time) instead of UPS

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame))]
        static void LoadCurrentGame_Postfix()
        {
            if (!Multiplayer.IsActive || !Multiplayer.Session.LocalPlayer.IsHost) return;
            GameStatesManager.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame))]
        static void SaveCurrentGame_Postfix()
        {
            if (!Multiplayer.IsActive || !Multiplayer.Session.LocalPlayer.IsHost) return;
            // Update last save time in clients
            GameStatesManager.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Multiplayer.Session.Server.SendPacket(new GameStateSaveInfoPacket(GameStatesManager.LastSaveTime));
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(UIEscMenu), nameof(UIEscMenu._OnOpen))]
        public static void UIEscMenu_OnOpen_Postfix(UIEscMenu __instance)
        {
            if (!Multiplayer.IsActive || Multiplayer.Session.LocalPlayer.IsHost) return;

            var timeSinceSave = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - GameStatesManager.LastSaveTime;
            var second = (int)(timeSinceSave);
            var minute = second / 60;
            var hour = minute / 60;
            var saveBtnText = "存档时间".Translate() + $" {hour}h{minute % 60}m{second % 60}s ago";
            __instance.button2Text.text = saveBtnText;
        }
    }
}
