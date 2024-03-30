using HarmonyLib;
using NebulaModel.Utils;
using NebulaNetwork;
using NebulaWorld;
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
                
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 2)
                {
                    harmony.PatchAll(typeof(Waraper092));
                    Log.Info("Nebula hotfix 0.9.2 - OK");
                }

                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(Analysis.StacktraceParser));
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

    public static class Waraper092
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.KeyTickLogic))]
        public static void EnemyDFGroundSystem_KeyTickLogic_Prefix(EnemyDFGroundSystem __instance)
        {
            // Fix NRE in EnemyDFGroundSystem.KeyTickLogic (System.Int64 time);(IL_0929)
            if (!Multiplayer.IsActive || Multiplayer.Session.IsServer) return;

            var cursor = __instance.builders.cursor;
            var buffer = __instance.builders.buffer;
            var baseBuffer = __instance.bases.buffer;
            var enemyPool = __instance.factory.enemyPool;
            for (int builderId = 1; builderId < cursor; builderId++)
            {
                ref var builder = ref buffer[builderId];
                
                if (builder.id == builderId)
                {
                    if (baseBuffer[enemyPool[builder.enemyId].owner] == null)
                    {
                        var msg = $"Remove EnemyDFGroundSystem enemy[{builder.enemyId}]: owner = {enemyPool[builder.enemyId].owner}";
                        Log.Warn(msg);
                        ChatManager.ShowWarningInChat(msg);

                        using (Multiplayer.Session.Combat.IsIncomingRequest.On())
                        {
                            __instance.factory.KillEnemyFinally(GameMain.mainPlayer, builder.enemyId, ref CombatStat.empty);
                            __instance.factory.enemyPool[builder.enemyId].SetEmpty();
                            __instance.builders.Remove(builderId);
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpaceSector), nameof(SpaceSector.GameTick))]
        public static void SpaceSector_GameTick_Prefix(SpaceSector __instance)
        {
            // Fix NRE in DFSTurretComponent.InternalUpdate (PrefabDesc pdesc);(IL_0017)
            if (!Multiplayer.IsActive || Multiplayer.Session.IsServer) return;

            for (var enemyId = 1; enemyId < __instance.enemyCursor; enemyId++)
            {
                ref var enemy = ref __instance.enemyPool[enemyId];
                if (enemy.id != enemyId) continue;

                if (SpaceSector.PrefabDescByModelIndex[enemy.modelIndex] == null)
                {
                    var msg = $"Remove SpeaceSector enemy[{enemyId}]: modelIndex{enemy.modelIndex}";
                    Log.Warn(msg);
                    ChatManager.ShowWarningInChat(msg);

                    using (Multiplayer.Session.Enemies.IsIncomingRequest.On())
                    {
                        __instance.KillEnemyFinal(enemyId, ref CombatStat.empty);
                        __instance.enemyPool[enemyId].SetEmpty();
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyFormation), nameof(EnemyFormation.RemoveUnit))]
        public static bool RemoveUnit_Prefix(EnemyFormation __instance, int port)
        {
            if (__instance.units[port] != 0)
            {
                if (__instance.vacancyCursor < __instance.vacancies.Length) // guard
                {
                    __instance.vacancies[__instance.vacancyCursor++] = port;
                }
                __instance.units[port] = 0;
            }
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SimulatedWorld), nameof(SimulatedWorld.SetupInitialPlayerState))]
        public static void SetupInitialPlayerState_Postfix()
        {
            // Fix IdxErr in UIZS_FighterEntry._OnUpdate () [0x001bb] ;IL_01BB 
            if (Multiplayer.Session.IsServer) return;

            FixInventory();

            //Log.Debug("CheckCombatModuleDataIsValidPatch");
            GameMain.mainPlayer.mecha.CheckCombatModuleDataIsValidPatch();

            // CombatModuleComponent.RemoveFleetDirectly
            CleanFighterCraftId(GameMain.mainPlayer.mecha.groundCombatModule);
            CleanFighterCraftId(GameMain.mainPlayer.mecha.spaceCombatModule);
        }

        static void FixInventory()
        {
            // Inventory Capacity level 7 will increase package columncount from 10 -> 12
            int packageRowCount = (GameMain.mainPlayer.package.size - 1) / GameMain.mainPlayer.GetPackageColumnCount() + 1;
            GameMain.mainPlayer.package.SetSize(GameMain.mainPlayer.packageColCount * packageRowCount); // Make sure all slots are available on UI
            GameMain.mainPlayer.deliveryPackage.rowCount = packageRowCount;
            GameMain.mainPlayer.deliveryPackage.NotifySizeChange();
        }

        static void CleanFighterCraftId(CombatModuleComponent combatModuleComponent)
        {
            for (int fleetIndex = 0; fleetIndex < combatModuleComponent.moduleFleets.Length; fleetIndex++)
            {
                ref var moduleFleet = ref combatModuleComponent.moduleFleets[fleetIndex];
                for (int fighterId = 0; fighterId < moduleFleet.fighters.Length; fighterId++)
                {
                    moduleFleet.fighters[fighterId].craftId = 0;
                }
            }
        }
    }
}
