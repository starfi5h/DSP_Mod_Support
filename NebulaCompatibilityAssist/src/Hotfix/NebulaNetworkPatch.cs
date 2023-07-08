using HarmonyLib;
using NebulaAPI;
using NebulaModel.Networking;
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

                    Type classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Factory.Foundation.FoundationBuildUpdateProcessor");
                    MethodInfo methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(FoundationBuildUpdatePacket), typeof(NebulaConnection) });
                    Plugin.Instance.Harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(FoundationBuildUpdateProcessor))));

                    Log.Info("PacketProcessors patch success!!");
                }
                catch (Exception e)
                {
                    Log.Warn("PacketProcessors patch fail!");
                    Log.Warn(e);
                }
            }
        }

        private static Vector3[] reformPoints = new Vector3[100];
        public static bool FoundationBuildUpdateProcessor(FoundationBuildUpdatePacket packet, NebulaConnection conn)
        {
            // Increase reformPoints for mods that increase brush size over 10
            if (packet.ReformSize * packet.ReformSize > reformPoints.Length)
            {
                Log.Info("FoundationBuildUpdateProcessor: brush size " + packet.ReformSize);
                reformPoints = new Vector3[packet.ReformSize * packet.ReformSize];
            }
            bool IsHost = Multiplayer.Session.LocalPlayer.IsHost;

            PlanetData planet = GameMain.galaxy.PlanetById(packet.PlanetId);
            PlanetFactory factory = IsHost ? GameMain.data.GetOrCreateFactory(planet) : planet?.factory;
            if (factory != null)
            {
                Array.Clear(reformPoints, 0, reformPoints.Length);

                //Check if some mandatory variables are missing
                if (factory.platformSystem.reformData == null)
                {
                    factory.platformSystem.InitReformData();
                }

                Multiplayer.Session.Factories.TargetPlanet = packet.PlanetId;
                Multiplayer.Session.Factories.AddPlanetTimer(packet.PlanetId);
                Multiplayer.Session.Factories.TargetPlanet = NebulaModAPI.PLANET_NONE;

                //Perform terrain operation
                int reformPointsCount = factory.planet.aux.ReformSnap(packet.GroundTestPos.ToVector3(), packet.ReformSize, packet.ReformType, packet.ReformColor, reformPoints, packet.ReformIndices, factory.platformSystem, out Vector3 reformCenterPoint);
                factory.ComputeFlattenTerrainReform(reformPoints, reformCenterPoint, packet.Radius, reformPointsCount, 3f, 1f);
                using (Multiplayer.Session.Factories.IsIncomingRequest.On())
                {
                    factory.FlattenTerrainReform(reformCenterPoint, packet.Radius, packet.ReformSize, packet.VeinBuried, 3f);
                }
                int area = packet.ReformSize * packet.ReformSize;
                for (int i = 0; i < area; i++)
                {
                    int num71 = packet.ReformIndices[i];
                    PlatformSystem platformSystem = factory.platformSystem;
                    if (num71 >= 0)
                    {
                        int type = platformSystem.GetReformType(num71);
                        int color = platformSystem.GetReformColor(num71);
                        if (type != packet.ReformType || color != packet.ReformColor)
                        {
                            factory.platformSystem.SetReformType(num71, packet.ReformType);
                            factory.platformSystem.SetReformColor(num71, packet.ReformColor);
                        }
                    }
                }
            }

            if (IsHost)
            {
                Multiplayer.Session.Network.SendPacketToStar(packet, planet.star.id);
            }

            return false;
        }
    }
}
