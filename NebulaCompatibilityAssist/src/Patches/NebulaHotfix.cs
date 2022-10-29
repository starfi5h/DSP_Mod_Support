using HarmonyLib;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using NebulaAPI;
using NebulaModel.Networking;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class NebulaHotfix
    {
        //private const string NAME = "NebulaMultiplayerMod";
        private const string GUID = "dsp.nebula-multiplayer";
        private static bool isPatched = false;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                System.Version nebulaVersion = pluginInfo.Metadata.Version;
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 8 && nebulaVersion.Build == 12)
                {
                    Patch0812(harmony);
                    Log.Info("Nebula hotfix 0.8.12 - OK");
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        private static void Patch0812(Harmony harmony)
        {
            Type classType;
            //classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            //harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaHotfix).GetMethod("BeforeHostGame")));

            classType = AccessTools.TypeByName("NebulaWorld.SimulatedWorld");
            harmony.Patch(AccessTools.Method(classType, "SetupInitialPlayerState"),
                null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod("SetupInitialPlayerState")));

            harmony.Patch(typeof(PlanetTransport).GetMethod("RefreshDispenserOnStoragePrebuildBuild"), 
                null, null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod("RefreshDispenserOnStoragePrebuildBuild_Transpiler")));
        }

        public static void SetupInitialPlayerState()
        {
            var player = NebulaModAPI.MultiplayerSession.LocalPlayer;
            if (player.IsClient && player.IsNewPlayer)
            {
                // Make new client spawn higher to avoid collision
                float altitude = GameMain.mainPlayer.transform.localPosition.magnitude;
                if (altitude > 0)
                    GameMain.mainPlayer.transform.localPosition *= (altitude + 20f) / altitude;
                Log.Debug($"Starting: {GameMain.mainPlayer.transform.localPosition} {altitude}");
            }
            else
            {
                // Prevent old client from dropping into gas gaint
                var planet = GameMain.galaxy.PlanetById(player.Data.LocalPlanetId);
                if (planet != null && planet.type == EPlanetType.Gas)
                {
                    GameMain.mainPlayer.movementState = EMovementState.Fly;
                }
            }
        }

        public static IEnumerable<CodeInstruction> RefreshDispenserOnStoragePrebuildBuild_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // factoryModel.gpuiManager is null for remote planets, so we need to use GameMain.gpuiManager which is initialized by nebula
                // replace : this.factory.planet.factoryModel.gpuiManager
                // with    : GameMain.gpuiManager
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(OpCodes.Callvirt),
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(i => i.opcode ==OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "gpuiManager")
                    )
                    .Repeat(matcher => matcher
                            .RemoveInstructions(4)
                            .SetAndAdvance(OpCodes.Call, typeof(GameMain).GetProperty("gpuiManager").GetGetMethod()
                    ));

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Warn("RefreshDispenserOnStoragePrebuildBuild_Transpiler fail!");
                Log.Dev(e);
                return instructions;
            }
        }

        public static void BeforeHostGame()
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

                    // Patch PacketProcessors here in the future
                    Log.Info("PacketProcessors patch success!");
                }
                catch (Exception e)
                {
                    Log.Warn("PacketProcessors patch fail!");
                    Log.Warn(e);
                }
            }
        }


    }
}
