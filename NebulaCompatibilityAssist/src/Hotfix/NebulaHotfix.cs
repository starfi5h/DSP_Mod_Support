using BepInEx.Bootstrap;
using HarmonyLib;
using NebulaModel;
using NebulaModel.DataStructures.Chat;
using NebulaModel.Networking;
using NebulaModel.Packets.Combat;
using NebulaModel.Packets.GameStates;
using NebulaModel.Packets.Logistics;
using NebulaModel.Utils;
using NebulaNetwork;
using NebulaPatcher.Patches.Dynamic;
using NebulaPatcher.Patches.Transpilers;
using NebulaWorld;
using NebulaWorld.Chat;
using NebulaWorld.Combat;
using NebulaWorld.GameStates;
using NebulaWorld.Logistics;
using NebulaWorld.MonoBehaviours.Local.Chat;
using NebulaWorld.Player;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using TMPro;
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
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 10)
                {
                    //harmony.PatchAll(typeof(Warper0910));
                    //PatchPacketProcessor(harmony);
                    //Log.Info("Nebula hotfix 0.9.10 - OK");
                }
                if (nebulaVersion < new System.Version(0, 9, 13))
                {
                    harmony.PatchAll(typeof(Warper0912));
                    //PatchPacketProcessor(harmony);
                    Log.Info("Nebula new feature 0.9.12 - OK");
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Warn(e);
            }

            try
            {
                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(SuppressErrors));
                Log.Info("Nebula extra features - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula extra features patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Warn(e);
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
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.GameTick))]
        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.GameTick))]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.CalcFormsSupply))]
        [HarmonyPatch(typeof(NearColliderLogic), nameof(NearColliderLogic.UpdateCursorNear))]
        [HarmonyPatch(typeof(PlayerAction_Combat), nameof(PlayerAction_Combat.AfterMechaGameTick))]
        [HarmonyPatch(typeof(CargoTraffic_Patch), nameof(CargoTraffic_Patch.SetBeltSignalIcon_Postfix))]
        [HarmonyPatch(typeof(CargoTraffic_Patch), nameof(CargoTraffic_Patch.SetBeltSignalNumber_Postfix))]
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

        static List<string> IgnorePluginList = new()
        {
            "IlLine",
            "CloseError",
            "Galactic Scale 2 Nebula Compatibility Plug-In",
            "Common API Nebula Compatibility",
            "Blueprint Tweaks Installation Checker",
            "Giga Stations Nebula Compatibility",
            "GenesisBook.InstallationCheck",
            "FractionateEverything.CheckPlugins",
            "MMSGCPatch",
            "MMSBottleneckCompat",
            "BuildBarTool.CheckPlugins",
            "BuildBarTool_RebindBuildBarCompat"
        };

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFatalErrorTip_Patch), nameof(UIFatalErrorTip_Patch.Title))]
        static void Title(ref string __result)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("An error has occurred! Game version ");
            stringBuilder.Append(GameConfig.gameVersion.ToString());
            stringBuilder.Append('.');
            stringBuilder.Append(GameConfig.gameVersion.Build);
            if (Multiplayer.IsActive)
            {
                stringBuilder.Append(Multiplayer.Session.LocalPlayer.IsHost ? " (Host)" : " (Client)");
            }
            stringBuilder.AppendLine();

            var modSB = new StringBuilder();
            var modCount = 0;
            foreach (var pluginInfo in Chainloader.PluginInfos.Values)
            {
                if (IgnorePluginList.Contains(pluginInfo.Metadata.Name)) continue;
                modSB.Append('[');
                modSB.Append(pluginInfo.Metadata.Name);
                modSB.Append(pluginInfo.Metadata.Version);
                modSB.Append("] ");
                modCount++;
            }
            stringBuilder.Append(modCount + " Mods used: ");
            stringBuilder.Append(modSB);

            __result = stringBuilder.ToString();
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITutorialTip), nameof(UITutorialTip.PopupTutorialTip))]
        private static bool UITutorialTip_PopupTutorialTip_Prefix(int tutorialId)
        {
            GameMain.history.UnlockTutorial(tutorialId);
            return false;
        }
    }

    public static class Warper0912
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SimulatedWorld), nameof(SimulatedWorld.FixPlayerAfterImport))]
        public static void FixPlayerAfterImport_Postfix()
        {
            // TODO: Sync energyShieldBurstUnlocked in importing
            GameMain.mainPlayer.mecha.energyShieldBurstUnlocked = true;
        }
    }
}
