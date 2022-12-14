using HarmonyLib;
using System;
using System.Reflection;

using NebulaModel.Networking;
using NebulaModel.Packets.GameHistory;
using NebulaModel.Packets.Planet;
using NebulaWorld;
using NebulaModel.Packets.Trash;
using NebulaModel.Packets.Factory.Splitter;

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

                    // Patch PacketProcessors here in the future
                    Type classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Planet.VegeMinedProcessor");
                    MethodInfo methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(VegeMinedPacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(VegeMinedProcessor))));

                    classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.GameHistory.GameHistoryUnlockTechProcessor");
                    methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(GameHistoryUnlockTechPacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(GameHistoryUnlockTechProcessor))));

                    classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Trash.TrashSystemTrashRemovedProcessor");
                    methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(TrashSystemTrashRemovedPacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(TrashSystemTrashRemovedProcessor))));

                    classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Trash.TrashSystemResponseDataProcessor");
                    methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(TrashSystemResponseDataPacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(TrashSystemResponseDataProcessor))));

                    classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Factory.Splitter.SplitterPriorityChangeProcessor");
                    methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(SplitterPriorityChangePacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(SplitterPriorityChangeProcessor))));

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

        public static bool GameHistoryUnlockTechProcessor(GameHistoryUnlockTechPacket packet)
        {
            Log.Info($"Unlocking tech (ID: {packet.TechId})");
            using (Multiplayer.Session.History.IsIncomingRequest.On())
            {
                // Let the default method give back the items
                GameMain.mainPlayer.mecha.lab.ManageTakeback();

                GameMain.history.UnlockTechUnlimited(packet.TechId, false);
            }

            return false;
        }

        public static bool TrashSystemTrashRemovedProcessor(TrashSystemTrashRemovedPacket packet, NebulaConnection conn)
        {
            if (Multiplayer.Session.LocalPlayer.IsHost)
            {
                Multiplayer.Session.Network.PlayerManager.SendPacketToOtherPlayers(packet, conn);
            }
            using (Multiplayer.Session.Trashes.RemoveTrashFromOtherPlayers.On())
            {
                GameMain.data.trashSystem.RemoveTrash(packet.TrashId);
            }

            return false;
        }

        public static bool TrashSystemResponseDataProcessor(TrashSystemResponseDataPacket packet)
        {
            if (Multiplayer.Session.LocalPlayer.IsHost)
            {
                return false;
            }

            using (BinaryUtils.Reader reader = new BinaryUtils.Reader(packet.TrashSystemData))
            {
                GameMain.data.trashSystem.Import(reader.BinaryReader);
            }
            // Wait until WarningDataPacket to assign warningId
            TrashContainer container = GameMain.data.trashSystem.container;
            for (int i = 0; i < container.trashCursor; i++)
            {
                container.trashDataPool[i].warningId = -1;
            }

            return false;
        }

        public static bool SplitterPriorityChangeProcessor(SplitterPriorityChangePacket packet, NebulaConnection conn)
        {
            SplitterComponent[] pool = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory?.cargoTraffic.splitterPool;
            if (pool != null && packet.SplitterIndex != -1 && packet.SplitterIndex < pool.Length && pool[packet.SplitterIndex].id != -1)
            {
                pool[packet.SplitterIndex].SetPriority(packet.Slot, packet.IsPriority, packet.Filter);
            }

            return false;
        }
    }
}
