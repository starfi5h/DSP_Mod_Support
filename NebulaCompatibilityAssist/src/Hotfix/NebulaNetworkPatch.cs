using HarmonyLib;
using NebulaAPI;
using NebulaModel.Networking;
using NebulaModel.Packets.Combat.GroundEnemy;
using NebulaModel.Packets.Factory.Foundation;
using NebulaWorld;
using System;
using System.Reflection;
using UnityEngine;

namespace NebulaCompatibilityAssist.Hotfix
{
    public static class NebulaNetworkPatch
    {
        private static bool isPatched = false;

        public static void BeforeMultiplayerGame()
        {
            if (!isPatched)
            {
                isPatched = true;
                try
                {
                    // We need patch PacketProcessor after NebulaNetwork assembly is loaded
                    foreach (Assembly a in AccessTools.AllAssemblies())
                    {
                        //Log.Info(a.GetName()); //why does iterate all assemblies stop the exception?
                    }

                    Type classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Combat.GroundEnemy.DFGKillEnemyProcessor");
                    MethodInfo methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(DFGKillEnemyPacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(DFGKillEnemyProcessor))));

                    Log.Info("PacketProcessors patch success!!");
                }
                catch (Exception e)
                {
                    Log.Warn("PacketProcessors patch fail!");
                    Log.Warn(e);
                }
            }
        }

        public static bool DFGKillEnemyProcessor(DFGKillEnemyPacket packet, NebulaConnection conn)
        {
            ProcessPacket(packet);
            return false;
        }

        static void ProcessPacket(DFGKillEnemyPacket packet)
        {
            var factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null || packet.EnemyId >= factory.enemyPool.Length) return;

            ref var ptr = ref factory.enemyPool[packet.EnemyId];
            var killStatistics = GameMain.data.spaceSector.skillSystem.killStatistics;            
            if (Multiplayer.Session.IsServer)
            {
                // Alive, broadcast the event to all clients in the system
                if (ptr.id > 0)
                {
                    killStatistics.RegisterFactoryKillStat(factory.index, ptr.modelIndex);
                    factory.KillEnemyFinally(GameMain.mainPlayer, packet.EnemyId, ref CombatStat.empty);
                }
                // If the enemy is already dead, that mean the client is behind and kill event has been sent by the server
            }
            else
            {
                using (Multiplayer.Session.Combat.IsIncomingRequest.On())
                {                    
                    if (ptr.id > 0)
                    {
                        killStatistics.RegisterFactoryKillStat(factory.index, ptr.modelIndex);
                        factory.KillEnemyFinally(GameMain.mainPlayer, packet.EnemyId, ref CombatStat.empty);
                    }
                    else if (ptr.isInvincible) // Mark
                    {
                        ptr.id = packet.EnemyId;
                        ptr.isInvincible = false;
                        killStatistics.RegisterFactoryKillStat(factory.index, ptr.modelIndex);
                        factory.KillEnemyFinally(GameMain.mainPlayer, packet.EnemyId, ref CombatStat.empty);
                    }
                }
            }
        }
    }
}