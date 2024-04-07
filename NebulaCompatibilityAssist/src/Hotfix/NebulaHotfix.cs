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
                            //__instance.factory.KillEnemyFinally(GameMain.mainPlayer, builder.enemyId, ref CombatStat.empty);
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
                        //__instance.KillEnemyFinal(enemyId, ref CombatStat.empty);
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

            FixPlayerAfterImport();
        }

        static void FixPlayerAfterImport()
        {
            var player = GameMain.mainPlayer;

            // Inventory Capacity level 7 will increase package columncount from 10 -> 12
            var packageRowCount = (player.package.size - 1) / player.GetPackageColumnCount() + 1;
            // Make sure all slots are available on UI
            player.package.SetSize(player.packageColCount * packageRowCount);
            player.deliveryPackage.rowCount = packageRowCount;
            player.deliveryPackage.NotifySizeChange();

            // Set fleetId = 0, fleetAstroId = 0 and fighter.craftId = 0
            var moduleFleets = player.mecha.groundCombatModule.moduleFleets;
            for (var index = 0; index < moduleFleets.Length; index++)
            {
                moduleFleets[index].ClearFleetForeignKey();
            }
            moduleFleets = player.mecha.spaceCombatModule.moduleFleets;
            for (var index = 0; index < moduleFleets.Length; index++)
            {
                moduleFleets[index].ClearFleetForeignKey();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.OnFactoryLoadFinished))]
        public static bool OnFactoryLoadFinished(PlanetFactory factory)
        {
            var turretsCursor = factory.defenseSystem.turrets.cursor;
            var turretsBuffer = factory.defenseSystem.turrets.buffer;
            for (var id = 1; id < turretsCursor; id++)
            {
                if (turretsBuffer[id].id == id)
                {
                    //Remove turretLaserContinuous
                    turretsBuffer[id].projectileId = 0;
                }
            }

            // Return if the game is not fully loaded yet
            /*
            if (!Multiplayer.Session.IsGameLoaded)
            {
                return false;
            }
            */

            // Clear all combatStat to avoid collision or index out of range error (mimic CombatStat.HandleFullHp)
            for (var i = 1; i < factory.entityCursor; i++)
            {
                factory.entityPool[i].combatStatId = 0;
            }
            for (var i = 1; i < factory.craftCursor; i++)
            {
                factory.craftPool[i].combatStatId = 0;
            }
            for (var i = 1; i < factory.vegeCursor; i++)
            {
                factory.vegePool[i].combatStatId = 0;
            }
            for (var i = 1; i < factory.enemyCursor; i++)
            {
                factory.enemyPool[i].combatStatId = 0;
            }
            for (var i = 1; i < factory.veinCursor; i++)
            {
                factory.veinPool[i].combatStatId = 0;
            }

            // Clear the combatStat pool
            var astroId = factory.planet.id;
            var count = 0;
            var combatStats = GameMain.data.spaceSector.skillSystem.combatStats;
            int CombatStatCursor = combatStats.cursor;
            CombatStat[] buffer = combatStats.buffer;
            for (int i = 1; i < CombatStatCursor; i++)
            {
                if (buffer[i].id == i && buffer[i].astroId == astroId)
                {
                    combatStats.Remove(i);
                    count++;
                }
            }
            Log.Info($"CombatManager: Clear {count} combatStat on {astroId}");
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ILSShipManager), nameof(ILSShipManager.CreateFakeStationComponent))]
        static void CreateFakeStationComponent_Prefix(int gId, int maxShipCount)
        {
            var stationComponent = GameMain.data.galacticTransport.stationPool[gId];
            stationComponent.idleShipCount = maxShipCount; // add dummy idle ship count to use in ILSShipManager
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ILSShipManager), nameof(ILSShipManager.IdleShipGetToWork))]
        static bool IdleShipGetToWork_Prefix(ILSIdleShipBackToWork packet)
        {
            IdleShipGetToWork(packet);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ILSShipManager), nameof(ILSShipManager.WorkShipBackToIdle))]
        static bool WorkShipBackToIdle_Prefix(ILSWorkShipBackToIdle packet)
        {
            WorkShipBackToIdle(packet);
            return false;
        }

        static void IdleShipGetToWork(ILSIdleShipBackToWork packet)
        {
            var planetA = GameMain.galaxy.PlanetById(packet.PlanetA);
            var planetB = GameMain.galaxy.PlanetById(packet.PlanetB);

            if (planetA == null || planetB == null)
            {
                return;
            }
            var stationPool = GameMain.data.galacticTransport.stationPool;
            if (stationPool.Length <= packet.ThisGId)
            {
                ILSShipManager.CreateFakeStationComponent(packet.ThisGId, packet.PlanetA, packet.StationMaxShipCount);
            }
            else if (stationPool[packet.ThisGId] == null)
            {
                ILSShipManager.CreateFakeStationComponent(packet.ThisGId, packet.PlanetA, packet.StationMaxShipCount);
            }
            if (stationPool.Length <= packet.OtherGId)
            {
                ILSShipManager.CreateFakeStationComponent(packet.OtherGId, packet.PlanetB, packet.StationMaxShipCount);
            }
            else if (stationPool[packet.OtherGId] == null)
            {
                ILSShipManager.CreateFakeStationComponent(packet.OtherGId, packet.PlanetB, packet.StationMaxShipCount);
            }

            var stationComponent = stationPool[packet.ThisGId];
            if (stationComponent == null)
            {
                return; // This shouldn't happen, but guard just in case
            }
            if (stationComponent.idleShipCount <= 0 || stationComponent.workShipCount >= stationComponent.workShipDatas.Length)
            {
                return; // Ship count is outside the range
            }

            stationComponent.workShipDatas[stationComponent.workShipCount].stage = -2;
            stationComponent.workShipDatas[stationComponent.workShipCount].planetA = packet.PlanetA;
            stationComponent.workShipDatas[stationComponent.workShipCount].planetB = packet.PlanetB;
            stationComponent.workShipDatas[stationComponent.workShipCount].otherGId = packet.OtherGId;
            stationComponent.workShipDatas[stationComponent.workShipCount].direction = 1;
            stationComponent.workShipDatas[stationComponent.workShipCount].t = 0f;
            stationComponent.workShipDatas[stationComponent.workShipCount].itemId = packet.ItemId;
            stationComponent.workShipDatas[stationComponent.workShipCount].itemCount = packet.ItemCount;
            stationComponent.workShipDatas[stationComponent.workShipCount].inc = packet.Inc;
            stationComponent.workShipDatas[stationComponent.workShipCount].gene = packet.Gene;
            stationComponent.workShipDatas[stationComponent.workShipCount].shipIndex = packet.ShipIndex;
            stationComponent.workShipDatas[stationComponent.workShipCount].warperCnt = packet.ShipWarperCount;
            stationComponent.warperCount = packet.StationWarperCount;

            stationComponent.workShipCount++;
            stationComponent.idleShipCount--;
            stationComponent.IdleShipGetToWork(packet.ShipIndex);

            var shipSailSpeed = GameMain.history.logisticShipSailSpeedModified;
            var shipWarpSpeed = GameMain.history.logisticShipWarpDrive
                ? GameMain.history.logisticShipWarpSpeedModified
                : shipSailSpeed;
            var astroPoses = GameMain.galaxy.astrosData;

            var canWarp = shipWarpSpeed > shipSailSpeed + 1f;
            var trip = (astroPoses[packet.PlanetB].uPos - astroPoses[packet.PlanetA].uPos).magnitude +
                       astroPoses[packet.PlanetB].uRadius + astroPoses[packet.PlanetA].uRadius;
            stationComponent.energy -= stationComponent.CalcTripEnergyCost(trip, shipSailSpeed, canWarp);
        }

        public static void WorkShipBackToIdle(ILSWorkShipBackToIdle packet)
        {
            if (!Multiplayer.IsActive || Multiplayer.Session.LocalPlayer.IsHost)
            {
                return;
            }

            var stationPool = GameMain.data.galacticTransport.stationPool;
            if (stationPool.Length <= packet.GId)
            {
                ILSShipManager.CreateFakeStationComponent(packet.GId, packet.PlanetA, packet.StationMaxShipCount);
            }
            else if (stationPool[packet.GId] == null)
            {
                ILSShipManager.CreateFakeStationComponent(packet.GId, packet.PlanetA, packet.StationMaxShipCount);
            }

            var stationComponent = stationPool[packet.GId];
            if (stationComponent == null)
            {
                return; // This shouldn't happen, but guard just in case
            }
            if (stationComponent.workShipCount <= 0 || stationComponent.workShipDatas.Length <= packet.WorkShipIndex)
            {
                return; // Ship count is outside the range
            }

            Array.Copy(stationComponent.workShipDatas, packet.WorkShipIndex + 1, stationComponent.workShipDatas,
                packet.WorkShipIndex, stationComponent.workShipDatas.Length - packet.WorkShipIndex - 1);
            stationComponent.workShipCount--;
            stationComponent.idleShipCount++;
            stationComponent.WorkShipBackToIdle(packet.ShipIndex);
            Array.Clear(stationComponent.workShipDatas, stationComponent.workShipCount,
                stationComponent.workShipDatas.Length - stationComponent.workShipCount);
        }
    }
}
