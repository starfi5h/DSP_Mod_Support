using HarmonyLib;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Reflection;

using NebulaAPI;
using NebulaModel.Networking;
using NebulaModel.Packets.Planet;
using NebulaWorld;

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
                    harmony.PatchAll(typeof(Analysis.StacktraceParser));
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
            classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaHotfix).GetMethod("BeforeHostGame")));

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
                    Type classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Planet.VegeMinedProcessor");
                    MethodInfo methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(VegeMinedPacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(VegeMinedProcessor))));

                    Log.Info("PacketProcessors patch success!");
                }
                catch (Exception e)
                {
                    Log.Warn("PacketProcessors patch fail!");
                    Log.Warn(e);
                }
            }
        }

        public static bool VegeMinedProcessor(VegeMinedPacket packet)
        {
            if (GameMain.galaxy.PlanetById(packet.PlanetId)?.factory != null && GameMain.galaxy.PlanetById(packet.PlanetId)?.factory?.vegePool != null)
            {
                using (Multiplayer.Session.Planets.IsIncomingRequest.On())
                {
                    PlanetData planetData = GameMain.galaxy.PlanetById(packet.PlanetId);
                    PlanetFactory factory = planetData?.factory;
                    if (packet.Amount == 0 && factory != null)
                    {
                        if (packet.IsVein)
                        {
                            VeinData veinData = factory.GetVeinData(packet.VegeId);
                            VeinProto veinProto = LDB.veins.Select((int)veinData.type);

                            factory.RemoveVeinWithComponents(packet.VegeId);

                            // Patch: Only show effect if it is on the same local planet
                            if (veinProto != null && GameMain.localPlanet == planetData)
                            {
                                VFEffectEmitter.Emit(veinProto.MiningEffect, veinData.pos, Maths.SphericalRotation(veinData.pos, 0f));
                                VFAudio.Create(veinProto.MiningAudio, null, veinData.pos, true, 0, -1, -1L);
                            }
                        }
                        else
                        {
                            VegeData vegeData = factory.GetVegeData(packet.VegeId);
                            VegeProto vegeProto = LDB.veges.Select(vegeData.protoId);

                            factory.RemoveVegeWithComponents(packet.VegeId);
                            Log.Warn(vegeProto != null && GameMain.localPlanet == planetData);

                            // Patch: Only show effect if it is on the same local planet
                            if (vegeProto != null && GameMain.localPlanet == planetData)
                            {
                                VFEffectEmitter.Emit(vegeProto.MiningEffect, vegeData.pos, Maths.SphericalRotation(vegeData.pos, 0f));
                                VFAudio.Create(vegeProto.MiningAudio, null, vegeData.pos, true, 0, -1, -1L);
                            }
                        }
                    }
                    else if (factory != null)
                    {
                        // Taken from if (!isInfiniteResource) part of PlayerAction_Mine.GameTick()
                        VeinData veinData = factory.GetVeinData(packet.VegeId);
                        VeinGroup[] veinGroups = factory.veinGroups;
                        short groupIndex = veinData.groupIndex;

                        // must be a vein/oil patch (i think the game treats them same now as oil patches can run out too)
                        factory.veinPool[packet.VegeId].amount = packet.Amount;
                        veinGroups[groupIndex].amount = veinGroups[groupIndex].amount - 1L;
                    }
                    else
                    {
                        Log.Warn("Received VegeMinedPacket but could not do as i was told :C");
                    }
                }
            }

            return false;
        }


    }
}
