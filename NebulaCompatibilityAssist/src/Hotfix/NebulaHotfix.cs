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
                
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 1)
                {
                    harmony.PatchAll(typeof(Waraper091));
                    Log.Info("Nebula hotfix 0.9.1 - OK");
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

    public static class Waraper091
    {

        #region Drone

        [HarmonyPrefix, HarmonyPatch(typeof(ConstructionModuleComponent), nameof(ConstructionModuleComponent.InsertTmpBuildTarget))]
        public static bool InsertTmpBuildTarget_Prefix(ConstructionModuleComponent __instance, int objectId, float value)
        {
            if (__instance.entityId != 0 || value < 225.0f) return true; // BAB, or distance is very close
            if (!Multiplayer.IsActive) return true;

            // Only send out mecha drones if it is the closest player to the target prebuild
            if (GameMain.localPlanet == null) return true;
            var sqrDistToOtherPlayer = GetClosestRemotePlayerSqrDistance(GameMain.localPlanet.factory.prebuildPool[objectId].pos);
            return value <= sqrDistToOtherPlayer;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ConstructionSystem), nameof(ConstructionSystem.AddBuildTargetToModules))]
        public static IEnumerable<CodeInstruction> AddBuildTargetToModules_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                /*  Sync Prebuild.itemRequired changes by player, insert local method call after player.package.TakeTailItems
                    Replace: if (num8 <= num) { this.player.mecha.constructionModule.InsertBuildTarget ... }
                    With:    if (num8 <= num && IsClosestPlayer(ref pos)) { this.player.mecha.constructionModule.InsertBuildTarget ... }
                */

                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(true,
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldloc_0),
                        new CodeMatch(OpCodes.Bgt_Un)
                    );
                Log.Info(codeMatcher.IsValid);
                var sqrDist = codeMatcher.InstructionAt(-2).operand;
                var skipLabel = codeMatcher.Operand;
                codeMatcher.Advance(1)
                    .Insert(
                        new CodeInstruction(OpCodes.Ldloc_S, sqrDist),
                        new CodeInstruction(OpCodes.Ldarg_2), //ref Vector3 pos
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Waraper091), nameof(IsClosestPlayer))),
                        new CodeInstruction(OpCodes.Brfalse_S, skipLabel)
                    );

                return codeMatcher.InstructionEnumeration();
            }
            catch (System.Exception e)
            {
                Log.Error("Transpiler ConstructionSystem.AddBuildTargetToModules failed.");
                Log.Error(e);
                return instructions;
            }
        }

        static bool IsClosestPlayer(float sqrDist, ref Vector3 pos)
        {
            if (!Multiplayer.IsActive) return true;
            if (sqrDist < 225.0f) return true;
            return sqrDist <= GetClosestRemotePlayerSqrDistance(pos);
        }

        static Vector3[] localPlayerPos = new Vector3[2];
        static int localPlayerCount = 0;

        [HarmonyPostfix, HarmonyPatch(typeof(DroneManager), nameof(DroneManager.RefreshCachedPositions))]
        static void RefreshCachedPositions_Postfix(DroneManager __instance)
        {
            localPlayerCount = 0;
            if (GameMain.localPlanet == null) return;
            var localPlanetId = GameMain.localPlanet.id;
            foreach (var data in __instance.cachedPositions.Values)
            {
                if (data.PlanetId != localPlanetId) continue;
                if (localPlayerCount >= localPlayerPos.Length)
                {
                    var newArray = new Vector3[localPlayerPos.Length * 2];
                    Array.Copy(localPlayerPos, newArray, localPlayerPos.Length);
                    localPlayerPos = newArray;
                }
                localPlayerPos[localPlayerCount++] = data.Position;
            }
        }

        static float GetClosestRemotePlayerSqrDistance(Vector3 pos)
        {
            var sqrDistance = float.MaxValue;
            for (var i = 0; i < localPlayerCount; i++)
            {
                var tmp = (pos - localPlayerPos[i]).sqrMagnitude;
                if (tmp < sqrDistance) sqrDistance = tmp;
            }
            return sqrDistance;
        }

        #endregion

        static bool suppressed = false;

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnGameBegin()
        {
            suppressed = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadServerData))]
        public static void LoadServerData_Prefix()
        {
            // Reset saved player data
            SaveManager.playerSaves.Clear();
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
                Log.Error(msg);
            }
            return null;
        }

    }
}
