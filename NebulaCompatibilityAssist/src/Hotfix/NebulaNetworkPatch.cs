/*
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
            var factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null || packet.EnemyId >= factory.enemyPool.Length) return true;

            ref var enemy = ref factory.enemyPool[packet.EnemyId];
            if (enemy.dfGBaseId != 0)
                Log.Debug($"DFGKill: base[{enemy.dfGBaseId}]: id={enemy.id} isInvincible={enemy.isInvincible}");
            return true;
        }
    }
}
*/