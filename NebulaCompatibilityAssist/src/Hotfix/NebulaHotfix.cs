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
                
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 6)
                {
                    harmony.PatchAll(typeof(Warper096));
                    Log.Info("Nebula hotfix 0.9.6 - OK");
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

    public static class Warper096
    {
        // Fix error when client load planet. 
        // IndexOutOfRangeException: Index was outside the bounds of the array.
        // EnemyUnitComponent.RunBehavior_Defense_Ground(System.Int32 formTick, SkillSystem skillSystem, EnemyData[] enemyPool, DFGBaseComponent[] bases, System.Single altitude, EnemyData& enemy);(IL_028B)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyManager), nameof(EnemyManager.OnFactoryLoadFinished))]
        public static void OnFactoryLoadFinished_Postfix(PlanetFactory factory)
        {
            var unitCursor = factory.enemySystem.units.cursor;
            var unitBuffer = factory.enemySystem.units.buffer;
            for (var i = 1; i < unitCursor; i++)
            {
                // clear the blocking skill to prevent error due to skills are not all present in client
                unitBuffer[i].ClearBlockSkill();
            }
        }

        // Fix error when bomb from other player from accessing the null planetfactory
        // NullReferenceException: Object reference not set to an instance of an object
        // Bomb_Explosive.TickSkillLogic (SkillSystem skillSystem, System.Int64 time);(IL_03BE)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Bomb_Explosive), nameof(Bomb_Explosive.TickSkillLogic))]
        [HarmonyPatch(typeof(Bomb_Liquid), nameof(Bomb_Liquid.TickSkillLogic))]
        [HarmonyPatch(typeof(Bomb_EMCapsule), nameof(Bomb_EMCapsule.TickSkillLogic))]
        public static void Bomb_TickSkillLogic(ref int ___nearPlanetAstroId, ref int ___life)
        {
            if (___nearPlanetAstroId > 0 && GameMain.spaceSector.skillSystem.astroFactories[___nearPlanetAstroId] == null)
            {
                // The nearest planetFactory hasn't loaded yet, skip and remove
                ___life = 0;
            }
        }
    }
}
