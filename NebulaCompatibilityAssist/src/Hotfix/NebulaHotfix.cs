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
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Warn(e);
            }

            try
            {
                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(Analysis.StacktraceParser));
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

    /*
    public static class Warper0910
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DFGBaseComponent_Transpiler), "LaunchCondition")]
        static bool LaunchCondition(DFGBaseComponent @this, ref bool __result)
        {
            if (!Multiplayer.IsActive) return true;

            // In MP, replace local_player_grounded_alive flag with the following condition
            var planetId = @this.groundSystem.planet.id;
            var players = Multiplayer.Session.Combat.Players;
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].isAlive && players[i].planetId == planetId)
                {
                    @this.groundSystem.local_player_pos = players[i].position;
                    Log.Debug($"Base attack LaunchCondition: player[{i}] planeId{planetId}");
                    __result =  true;
                    return false;
                }
            }
            __result = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyDFGroundSystem_Patch), "CanEraseBase_Prefix")]
        static bool StopPatch()
        {
            // Disable as the half-growth base can't be destoryed
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.KeyTickLogic))]
        static void FixClientBaseInvincible(EnemyDFGroundSystem __instance)
        {
            if (!Multiplayer.IsActive || Multiplayer.Session.IsServer) return;

            var cursor = __instance.bases.cursor;
            var baseBuffer = __instance.bases.buffer;
            var enemyPool = __instance.factory.enemyPool;
            for (int baseId = 1; baseId < cursor; baseId++)
            {
                var dfgbaseComponent = baseBuffer[baseId];
                if (dfgbaseComponent == null || dfgbaseComponent.id != baseId) continue;
                if (dfgbaseComponent.enemyId != 0 && enemyPool[dfgbaseComponent.enemyId].id == 0)
                {
                    // Note: isInvincible in enemy is used by Nebula client to note if the enemy is pending to get killed
                    // isInvincible will get set back to true in EnemyDFGroundSystem.KeyTickLogic when base sp > 0
                    // So we'll need to set isInvincible = true to let host's incoming KillEnemyFinally packet get executed
                    //if (!enemyPool[dfgbaseComponent.enemyId].isInvincible) Log.Debug($"Base[{baseId}] isInvincible = true");
                    enemyPool[dfgbaseComponent.enemyId].isInvincible = true;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.DamageGroundObjectByLocalCaster))]
        public static void DamageGroundObjectByLocalCaster_Prefix(PlanetFactory factory, int damage, int slice, ref SkillTarget target, ref SkillTarget caster)
        {
            if (caster.type != ETargetType.Craft
                || target.type != ETargetType.Enemy
                || !Multiplayer.IsActive || Multiplayer.Session.Combat.IsIncomingRequest.Value) return;

            if (factory == GameMain.localPlanet?.factory) // Sync for local planet combat drones
            {
                target.astroId = caster.astroId = GameMain.localPlanet.astroId;
                var packet = new CombatStatDamagePacket(damage, slice, in target, in caster)
                {
                    // Change the caster to player as craft (space fleet) is not sync yet
                    CasterType = (short)ETargetType.Player,
                    CasterId = Multiplayer.Session.LocalPlayer.Id
                };
                Multiplayer.Session.Network.SendPacketToLocalPlanet(packet);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpaceSector), nameof(SpaceSector.RemoveEnemyWithComponents))]
        public static void RemoveEnemyWithComponents_Prefix(SpaceSector __instance, int id)
        {
            // Fix IndexOutOfRangeException in SpaceSector.RemoveEnemyWithComponents IL_026A 
            // This is due to combatStats is not sync in client

            if (id != 0 && __instance.enemyPool[id].id != 0)
            {
                if (__instance.enemyPool[id].combatStatId != 0)
                {
                    if (__instance.enemyPool[id].combatStatId >= __instance.skillSystem.combatStats.cursor)
                        __instance.enemyPool[id].combatStatId = 0;
                }
            }
        }
    }
    */
}
